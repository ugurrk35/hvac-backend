namespace ECommerce.Domain.Entity;

/// <summary>Controls whether customers choose a shipping method during checkout.</summary>
public class ShippingCheckoutSettings : AuditableEntity
{
    public bool IsMethodSelectionEnabled { get; set; }
}
