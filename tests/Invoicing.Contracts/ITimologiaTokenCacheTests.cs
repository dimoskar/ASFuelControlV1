using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Invoicing;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

internal static partial class Program
{
    private static void ITimologiaTokenCache()
    {
        Test("ASFuelControl keeps the product-neutral iTimologia user-variable name", () => {
            string suffix = Guid.NewGuid().ToString("N").ToUpperInvariant();
            string scope = "EXEDRON_INVOICING_ITIMOLOGIA_TOKEN_" + suffix;
            string variable = scope;
            string original = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            string json = JsonConvert.SerializeObject(new {
                connectionToken = "synthetic-opaque-value",
                expiresAt = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
            try
            {
                var cache = new UserEnvironmentCommunicationTokenCache();
                cache.Save(scope, json);
                Equal(variable, UserEnvironmentCommunicationTokenCache.VariableName(scope));
                Equal(json, Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User));
                Equal(json, cache.Load(scope));
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, original, EnvironmentVariableTarget.User);
            }
        });

        Test("A new iTimologia provider instance reuses the stored communication token", () => {
            var cache = new MemoryCommunicationTokenCache();
            var state = new TokenHttpState();
            using (var first = Provider(cache, state))
                first.LookupInvoice(Guid.NewGuid());
            Equal(1, state.Creates);

            using (var second = Provider(cache, state))
                second.LookupInvoice(Guid.NewGuid());
            Equal(1, state.Creates);
            Equal(0, state.Renews);
        });

        Test("Concurrent iTimologia provider instances perform at most one token renewal", () => {
            var cache = new MemoryCommunicationTokenCache();
            var state = new TokenHttpState { DelayRenewal = true };
            string scope;
            using (var probe = Provider(cache, state)) scope = probe.TokenCacheVariableName;
            cache.Save(scope, JsonConvert.SerializeObject(new {
                connectionToken = "synthetic-expired-value",
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            }));

            using (var first = Provider(cache, state))
            using (var second = Provider(cache, state))
            using (var start = new ManualResetEventSlim())
            {
                var one = Task.Run(() => { start.Wait(); first.LookupInvoice(Guid.NewGuid()); });
                var two = Task.Run(() => { start.Wait(); second.LookupInvoice(Guid.NewGuid()); });
                start.Set();
                Task.WaitAll(one, two);
            }

            Equal(0, state.Creates);
            Equal(1, state.Renews);
        });

        Test("iTimologia keeps its process-local token cache when no host cache is supplied", () => {
            var state = new TokenHttpState();
            using (var provider = Provider(null, state))
            {
                provider.LookupInvoice(Guid.NewGuid());
                provider.LookupInvoice(Guid.NewGuid());
            }
            Equal(1, state.Creates);
            Equal(0, state.Renews);
        });
    }

    private static ITimologiaInvoiceProvider Provider(ICommunicationTokenCache cache, TokenHttpState state)
    {
        return new ITimologiaInvoiceProvider(new ITimologiaProviderOptions {
            IssuerVat = "099999999",
            MasterToken = "synthetic-master-value",
            SubscriptionId = "synthetic-subscription-value",
            TestMode = true,
            SubmissionStore = new EmptyITimologiaStore(),
            TokenCache = cache,
            Logger = new DiscardingLogger()
        }, new TokenHttpHandler(state));
    }

    private sealed class DiscardingLogger : IRequestResponseLogger
    {
        public void RequestStarted(InvoiceRequestLog request) { }
        public void ResponseReceived(InvoiceResponseLog response) { }
    }

    private sealed class EmptyITimologiaStore : IITimologiaSubmissionStore
    {
        public IDisposable Acquire(Guid invoiceId) { return new EmptyLease(); }
        public ITimologiaSubmissionAttempt LoadLatest(Guid invoiceId, bool testMode, string issuerVat) { return null; }
        public void Begin(ITimologiaSubmissionAttempt attempt) { }
        public void Complete(ITimologiaSubmissionAttempt attempt) { }

        private sealed class EmptyLease : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class MemoryCommunicationTokenCache : ICommunicationTokenCache
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();
        private readonly object sync = new object();

        public string Load(string scope)
        {
            lock (sync)
            {
                string value;
                return values.TryGetValue(scope, out value) ? value : null;
            }
        }

        public void Save(string scope, string serializedToken)
        {
            lock (sync) values[scope] = serializedToken;
        }
    }

    private sealed class TokenHttpState
    {
        public int Creates;
        public int Renews;
        public bool DelayRenewal;
    }

    private sealed class TokenHttpHandler : HttpMessageHandler
    {
        private readonly TokenHttpState state;

        public TokenHttpHandler(TokenHttpState state)
        {
            this.state = state;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = request.RequestUri.AbsolutePath;
            if (path.EndsWith("/tokens/create", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref state.Creates);
                return Task.FromResult(TokenResponse());
            }
            if (path.EndsWith("/tokens/renew", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref state.Renews);
                if (state.DelayRenewal) Thread.Sleep(100);
                return Task.FromResult(TokenResponse());
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) {
                Content = new StringContent("{}")
            });
        }

        private static HttpResponseMessage TokenResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"token\":\"synthetic-refreshed-value\",\"expires_at\":\"2099-01-01T00:00:00Z\"}")
            };
        }
    }
}
