namespace ECommerce.Domain.Entity
{
    /// <summary>Internal staff note. It is never exposed to the storefront customer.</summary>
    public class OrderNote : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
