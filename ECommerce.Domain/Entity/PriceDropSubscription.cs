namespace ECommerce.Domain.Entity
{
    public class PriceDropSubscription : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal ReferencePrice { get; set; }
        public bool IsNotified { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
