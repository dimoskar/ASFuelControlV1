using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Exedron.Invoicing.Core
{
    /// <summary>
    /// Configuration for the iTimologia communication-token create/renew flow.
    /// </summary>
    public sealed class ITimologiaCommunicationTokenManagerOptions
    {
        public string BaseEndpoint { get; set; }
        public string MasterToken { get; set; }
        public string SubscriptionId { get; set; }
        public ICommunicationTokenCache TokenCache { get; set; }
        public string CacheKey { get; set; }
        public string LegacyCacheKey { get; set; }
        public string MutexName { get; set; }
        public TimeSpan ExpirationSafetyMargin { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan MutexTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Sanitized failure from an iTimologia token create or renew request.
    /// Response bodies and credentials are deliberately not exposed.
    /// </summary>
    public sealed class ITimologiaTokenAuthenticationException : InvalidOperationException
    {
        public ITimologiaTokenAuthenticationException(
            string operation,
            string endpoint,
            int? statusCode,
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
            Operation = operation;
            Endpoint = endpoint;
            StatusCode = statusCode;
        }

        public string Operation { get; private set; }
        public string Endpoint { get; private set; }
        public int? StatusCode { get; private set; }
    }

    /// <summary>
    /// Shared iTimologia-only token manager. It creates an initial communication token,
    /// renews an existing token, and coordinates a shared cache across processes.
    /// </summary>
    public sealed class ITimologiaCommunicationTokenManager
    {
        public const string SharedCachePrefix = "EXEDRON_INVOICING_ITIMOLOGIA_TOKEN_";
        public const string LegacyCachePrefix = "ASSALESMANAGER_ITIMOLOGIA_TOKEN_";
        public const string SharedMutexPrefix = @"Local\EXEDRON_ITIMOLOGIA_TOKEN_LOCK_";

        private static readonly object TokenSync = new object();
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        private readonly ITimologiaCommunicationTokenManagerOptions options;
        private readonly HttpClient httpClient;
        private string communicationToken;
        private string memoryTokenCache;

        public ITimologiaCommunicationTokenManager(
            ITimologiaCommunicationTokenManagerOptions options,
            HttpClient httpClient)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(options.BaseEndpoint))
                throw new ArgumentException("The iTimologia base endpoint is required.", nameof(options));
            options.BaseEndpoint = NormalizeBaseEndpoint(options.BaseEndpoint);
            if (string.IsNullOrWhiteSpace(options.CacheKey))
                options.CacheKey = CreateCacheKey(options.BaseEndpoint, options.SubscriptionId, options.MasterToken);
        }

        public string CacheKey { get { return options.CacheKey; } }
        public string CurrentToken { get { return communicationToken; } }

        public static ITimologiaCommunicationTokenManager CreateWithEnvironmentCache(
            string baseEndpoint,
            string masterToken,
            string subscriptionId,
            EnvironmentVariableTarget target,
            HttpClient httpClient)
        {
            baseEndpoint = NormalizeBaseEndpoint(baseEndpoint);
            string cacheKey = CreateCacheKey(baseEndpoint, subscriptionId, masterToken);
            string suffix = cacheKey.Substring(SharedCachePrefix.Length);
            return new ITimologiaCommunicationTokenManager(
                new ITimologiaCommunicationTokenManagerOptions
                {
                    BaseEndpoint = baseEndpoint,
                    MasterToken = masterToken,
                    SubscriptionId = subscriptionId,
                    TokenCache = new ITimologiaEnvironmentTokenCache(target),
                    CacheKey = cacheKey,
                    LegacyCacheKey = LegacyCachePrefix + suffix,
                    MutexName = CreateMutexName(cacheKey)
                }, httpClient);
        }

        public static string CreateCacheKey(string baseEndpoint, string subscriptionId, string masterToken)
        {
            var scope = JsonConvert.SerializeObject(new[] {
                NormalizeBaseEndpoint(baseEndpoint),
                (subscriptionId ?? string.Empty).Trim(),
                (masterToken ?? string.Empty).Trim()
            }, JsonSettings);
            using (var hash = SHA256.Create())
                return SharedCachePrefix +
                    BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(scope))).Replace("-", string.Empty);
        }

        public static string CreateMutexName(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey)) throw new ArgumentNullException(nameof(cacheKey));
            return SharedMutexPrefix + (cacheKey.StartsWith(SharedCachePrefix, StringComparison.Ordinal)
                ? cacheKey.Substring(SharedCachePrefix.Length)
                : CacheKeyHash(cacheKey));
        }

        public bool PrepareCache()
        {
            return WithSharedLock(() =>
            {
                var cached = ReadCachedTokenOrMigrateLegacy();
                if (!IsReusable(cached)) return false;
                communicationToken = cached.ConnectionToken;
                return true;
            });
        }

        public string GetToken(bool forceRefresh)
        {
            return WithSharedLock(() => GetTokenUnderLock(forceRefresh));
        }

        private string GetTokenUnderLock(bool forceRefresh)
        {
            var cached = ReadCachedTokenOrMigrateLegacy();
            if (IsReusable(cached) &&
                (!forceRefresh || !string.Equals(cached.ConnectionToken, communicationToken, StringComparison.Ordinal)))
            {
                communicationToken = cached.ConnectionToken;
                return communicationToken;
            }

            var previousToken = cached == null ? communicationToken : cached.ConnectionToken;
            bool renew = !string.IsNullOrWhiteSpace(previousToken);
            string operation = renew ? "Token renewal" : "Token creation";
            string endpoint = Endpoint(renew ? "tokens/renew" : "tokens/create");
            object body = renew
                ? (object)new TokenRenewRequest { Token = previousToken }
                : new TokenCreateRequest { SubscriptionId = (options.SubscriptionId ?? string.Empty).Trim() };

            TokenHttpResult result;
            try
            {
                result = Send(endpoint, body);
            }
            catch (Exception ex)
            {
                throw new ITimologiaTokenAuthenticationException(
                    operation, endpoint, null, "iTimologia " + operation.ToLowerInvariant() + " request failed.", ex);
            }

            // A rejected renewal has two distinct meanings in the iTimologia
            // protocol. HTTP 401 keeps the existing token lifecycle and must not
            // create another token. HTTP 403 says that token can no longer be
            // renewed, so and only so may a replacement token be created.
            if (renew && result.StatusCode == HttpStatusCode.Forbidden)
            {
                renew = false;
                operation = "Token creation";
                endpoint = Endpoint("tokens/create");
                try
                {
                    result = Send(endpoint, new TokenCreateRequest
                    {
                        SubscriptionId = (options.SubscriptionId ?? string.Empty).Trim()
                    });
                }
                catch (Exception ex)
                {
                    throw new ITimologiaTokenAuthenticationException(
                        operation, endpoint, null, "iTimologia token creation request failed.", ex);
                }
            }

            if (!result.IsSuccess)
            {
                // If another application published a replacement during a failed
                // renewal, adopt it. A renewal HTTP 401 never falls through to create.
                if (renew && result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var replacement = ReadCachedTokenOrMigrateLegacy();
                    if (IsReusable(replacement) &&
                        !string.Equals(replacement.ConnectionToken, previousToken, StringComparison.Ordinal))
                    {
                        communicationToken = replacement.ConnectionToken;
                        return communicationToken;
                    }
                }
                throw new ITimologiaTokenAuthenticationException(operation, endpoint, (int)result.StatusCode,
                    "iTimologia " + operation.ToLowerInvariant() + " failed (HTTP " + (int)result.StatusCode + ").");
            }

            TokenReply reply;
            try
            {
                reply = JsonConvert.DeserializeObject<TokenReply>(result.Body, JsonSettings);
            }
            catch (JsonException ex)
            {
                throw new ITimologiaTokenAuthenticationException(operation, endpoint, (int)result.StatusCode,
                    "iTimologia returned an invalid token response.", ex);
            }

            if (reply == null || string.IsNullOrWhiteSpace(reply.Token))
                throw new ITimologiaTokenAuthenticationException(operation, endpoint, (int)result.StatusCode,
                    "iTimologia did not return a communication token.");
            if (!string.IsNullOrWhiteSpace(reply.Error) || !reply.ExpiresAt.HasValue ||
                reply.ExpiresAt.Value <= DateTimeOffset.UtcNow)
                throw new ITimologiaTokenAuthenticationException(operation, endpoint, (int)result.StatusCode,
                    "iTimologia did not return a valid communication-token expiration time.");

            SaveCache(JsonConvert.SerializeObject(new TokenCache
            {
                ConnectionToken = reply.Token,
                ExpiresAt = reply.ExpiresAt.Value.ToUniversalTime()
            }, JsonSettings));
            communicationToken = reply.Token;
            return communicationToken;
        }

        private TokenHttpResult Send(string endpoint, object body)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", (options.MasterToken ?? string.Empty).Trim());
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(body, JsonSettings), Encoding.UTF8, "application/json");
                using (var response = httpClient.SendAsync(request).GetAwaiter().GetResult())
                    return new TokenHttpResult(response.StatusCode,
                        response.Content == null ? string.Empty : response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }

        private T WithSharedLock<T>(Func<T> action)
        {
            lock (TokenSync)
            {
                string mutexName = string.IsNullOrWhiteSpace(options.MutexName)
                    ? CreateMutexName(options.CacheKey)
                    : options.MutexName;
                using (var mutex = new Mutex(false, mutexName))
                {
                    bool acquired = false;
                    try
                    {
                        try { acquired = mutex.WaitOne(options.MutexTimeout); }
                        catch (AbandonedMutexException) { acquired = true; }
                        if (!acquired)
                            throw new TimeoutException("Timed out waiting for the shared iTimologia token cache.");
                        return action();
                    }
                    finally
                    {
                        if (acquired) mutex.ReleaseMutex();
                    }
                }
            }
        }

        private TokenCache ReadCachedTokenOrMigrateLegacy()
        {
            var cached = ParseTokenCache(LoadCache(options.CacheKey));
            if (cached == null && !string.IsNullOrWhiteSpace(options.LegacyCacheKey))
            {
                var legacyJson = LoadCache(options.LegacyCacheKey);
                cached = ParseTokenCache(legacyJson);
                if (cached != null) SaveCache(legacyJson);
            }
            return cached;
        }

        private string LoadCache(string key)
        {
            return options.TokenCache == null ? memoryTokenCache : options.TokenCache.Load(key);
        }

        private void SaveCache(string value)
        {
            if (options.TokenCache == null) memoryTokenCache = value;
            else options.TokenCache.Save(options.CacheKey, value);
        }

        private bool IsReusable(TokenCache cached)
        {
            return cached != null && !string.IsNullOrWhiteSpace(cached.ConnectionToken) &&
                cached.ExpiresAt > DateTimeOffset.UtcNow.Add(options.ExpirationSafetyMargin);
        }

        private static TokenCache ParseTokenCache(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var cached = JsonConvert.DeserializeObject<TokenCache>(json, JsonSettings);
                return cached != null && !string.IsNullOrWhiteSpace(cached.ConnectionToken) ? cached : null;
            }
            catch (JsonException) { return null; }
        }

        private string Endpoint(string relativePath)
        {
            return options.BaseEndpoint.TrimEnd('/') + "/" + relativePath.TrimStart('/');
        }

        private static string NormalizeBaseEndpoint(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/') + "/";
        }

        private static string CacheKeyHash(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private sealed class TokenCreateRequest
        {
            [JsonProperty("subscription_id")]
            public string SubscriptionId { get; set; }
        }

        private sealed class TokenRenewRequest
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

        private sealed class TokenCache
        {
            [JsonProperty("connectionToken")]
            public string ConnectionToken { get; set; }
            [JsonProperty("expiresAt")]
            public DateTimeOffset ExpiresAt { get; set; }
        }

        private sealed class TokenReply
        {
            [JsonProperty("expires_at")]
            public DateTimeOffset? ExpiresAt { get; set; }
            [JsonProperty("token")]
            public string Token { get; set; }
            [JsonProperty("error")]
            public string Error { get; set; }
        }

        private sealed class TokenHttpResult
        {
            public TokenHttpResult(HttpStatusCode statusCode, string body)
            {
                StatusCode = statusCode;
                Body = body;
            }
            public HttpStatusCode StatusCode { get; private set; }
            public string Body { get; private set; }
            public bool IsSuccess { get { return (int)StatusCode >= 200 && (int)StatusCode <= 299; } }
        }
    }

    /// <summary>
    /// Windows environment-variable cache used by iTimologia token managers that
    /// share a token between applications running as the same user.
    /// </summary>
    public sealed class ITimologiaEnvironmentTokenCache : ICommunicationTokenCache
    {
        private readonly EnvironmentVariableTarget target;

        public ITimologiaEnvironmentTokenCache(EnvironmentVariableTarget target)
        {
            this.target = target;
        }

        public string Load(string scope)
        {
            return Environment.GetEnvironmentVariable(scope, target);
        }

        public void Save(string scope, string serializedToken)
        {
            Environment.SetEnvironmentVariable(scope, serializedToken, target);
        }
    }
}
