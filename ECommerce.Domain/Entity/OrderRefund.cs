namespace ECommerce.Domain.Entity
{
    public enum RefundStatus { Pending = 0, Succeeded = 1, Failed = 2 }
    public class OrderRefund : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        // A client-generated key makes retries safe when the browser loses the response.
        public string? IdempotencyKey { get; set; }
        public RefundStatus Status { get; set; }
        public string? ProviderResponse { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
