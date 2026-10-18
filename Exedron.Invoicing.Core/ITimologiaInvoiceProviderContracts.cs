using System;
using Exedron.Invoicing.Core.Models;
namespace Exedron.Invoicing.Core
{
    public sealed class ITimologiaProviderOptions
    {
        public string IssuerVat { get; set; }
        public string MasterToken { get; set; }
        public string SubscriptionId { get; set; }
        public bool TestMode { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
        public string PosTerminalId { get; set; }
        public int PosSignatureFormat { get; set; }
        public IITimologiaSubmissionStore SubmissionStore { get; set; }
        public ICommunicationTokenCache TokenCache { get; set; }
        public IRequestResponseLogger Logger { get; set; }
    }

}
