namespace ECommerce.Domain.Entity;

public class ProductDetailSettings : BaseEntity
{
    public int? CategoryId { get; set; }
    public string? ConfigJson { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
