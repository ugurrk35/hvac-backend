namespace ECommerce.Domain.Entity
{
    public enum ReturnRequestStatus { Pending = 0, Approved = 1, Rejected = 2, Received = 3, Closed = 4 }

    public class OrderReturnRequest : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? CustomerNote { get; set; }
        public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.Pending;
        public string? AdminNote { get; set; }
        public string? ReturnLabelUrl { get; set; }
        public string? ReturnTrackingNumber { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public ICollection<OrderReturnRequestItem> Items { get; set; } = new List<OrderReturnRequestItem>();
    }
}
