using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string SKU { get; set; }
        public string ShortDescription { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal EffectivePrice => DiscountPrice ?? BasePrice;
        public bool IsPublished { get; set; }
        public int Quantity { get; set; }
        public bool InStock => Quantity > 0;
        public string Brand { get; set; }
        public string BrandLogoUrl { get; set; }
        public List<string> CardHighlights { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string MainImageUrl { get; set; }
        public List<string> TagNames { get; set; } = new();
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int SizeOptionCount { get; set; }
        public List<string> SizeOptions { get; set; } = new();
        public List<string> ColorOptions { get; set; } = new();
        public bool IsNew { get; set; }
    }
}
