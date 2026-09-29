using System.ComponentModel.DataAnnotations;

namespace ECommerce.Service.Dtos.ProductDtos
{
    /// <summary>
    /// Applies an exact base price and/or stock quantity to every product matching a filter.
    /// </summary>
    public class BulkUpdateProductsByFilterDto
    {
        [Required]
        public ProductFilterDto Filter { get; set; } = new();

        [Range(0.01, double.MaxValue)]
        public decimal? BasePrice { get; set; }

        [Range(0, int.MaxValue)]
        public int? Quantity { get; set; }
    }
}
