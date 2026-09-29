namespace ECommerce.Domain.Entity;

public class AdvertisingSpend : AuditableEntity
{
    public string Platform { get; set; } = string.Empty;
    public string? Campaign { get; set; }
    public DateOnly SpendDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
}
