namespace ECommerce.Domain.Entity
{
    public class BackInStockSubscription : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsNotified { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
