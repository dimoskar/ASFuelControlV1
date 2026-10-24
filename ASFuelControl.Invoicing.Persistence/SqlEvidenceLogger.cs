using System;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class SqlEvidenceLogger : IRequestResponseLogger, IPaymentGatewayEventSink
    {
        private readonly SqlFiscalDatabase database;
        public SqlEvidenceLogger(SqlFiscalDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public void RequestStarted(InvoiceRequestLog request)
        {
            if (request == null || request.ExchangeId == Guid.Empty) throw new ArgumentException("Exchange identity required.");
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                SqlStatements.InsertExchange,
                "@id", request.ExchangeId, "@invoice", request.InvoiceId, "@kind", request.DocumentKind.HasValue ? (int?)request.DocumentKind.Value : null,
                "@document", request.DocumentId, "@started", request.StartedUtc.UtcDateTime, "@request", SqlFiscalDatabase.Json(request)))
                command.ExecuteNonQuery();
        }
        public void ResponseReceived(InvoiceResponseLog response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                SqlStatements.CompleteExchange,
                "@id", response.ExchangeId, "@completed", response.CompletedUtc.UtcDateTime, "@response", SqlFiscalDatabase.Json(response)))
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Missing request or already completed exchange; evidence was not overwritten.");
        }
        public void Record(PaymentGatewayEvent paymentEvent)
        {
            if (paymentEvent == null || string.IsNullOrWhiteSpace(paymentEvent.TransactionUid)) throw new ArgumentException("Payment event identity required.");
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                SqlStatements.InsertPaymentEvent,
                "@id", Guid.NewGuid(), "@uid", paymentEvent.TransactionUid, "@now", DateTime.UtcNow, "@event", SqlFiscalDatabase.Json(paymentEvent)))
                command.ExecuteNonQuery();
        }
    }
}
