namespace ECommerce.Domain.Entity
{
    public enum MarketingAutomationType { AbandonedCart = 0, BackInStock = 1, PriceDrop = 2, PostPurchaseCrossSell = 3 }
    public enum MarketingChannel { Email = 0, Sms = 1, WhatsApp = 2 }
    public enum MarketingJobStatus { Pending = 0, Processing = 1, Sent = 2, Failed = 3, Skipped = 4 }
    public class MarketingAutomationJob : AuditableEntity
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string DeduplicationKey { get; set; } = string.Empty;
        public MarketingAutomationType Type { get; set; }
        public MarketingChannel Channel { get; set; }
        public MarketingJobStatus Status { get; set; } = MarketingJobStatus.Pending;
        public DateTime ScheduledAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public string? ErrorMessage { get; set; }
        public int AttemptCount { get; set; }
    }
}
