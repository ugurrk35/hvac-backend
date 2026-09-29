namespace ECommerce.Domain.Entity
{
    /// <summary>Optional city/district-specific price and free-shipping rule for a shipping method.</summary>
    public class ShippingMethodRateOverride : AuditableEntity
    {
        public int ShippingMethodId { get; set; }
        public ShippingMethod? ShippingMethod { get; set; }
        public string City { get; set; } = string.Empty;
        public string? District { get; set; }
        public decimal? Price { get; set; }
        public decimal? FreeShippingThreshold { get; set; }
    }
}
