namespace ECommerce.Domain.Entity
{
    public class CustomerMarketingConsent : AuditableEntity
    {
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public bool EmailMarketing { get; set; }
        public bool SmsMarketing { get; set; }
        public bool WhatsAppMarketing { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Source { get; set; }
    }
}
