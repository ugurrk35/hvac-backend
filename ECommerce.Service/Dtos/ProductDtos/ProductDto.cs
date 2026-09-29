using ECommerce.Service.Dtos.ProductTagDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string SKU { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal EffectivePrice => DiscountPrice ?? BasePrice;
        public bool IsPublished { get; set; }
        public int Quantity { get; set; }
        public bool InStock => Quantity > 0;
        public int ReviewCount { get; set; }
        public int QuestionCount { get; set; }

        // SEO Fields
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImage { get; set; }
        public string TwitterCardType { get; set; }

        // Product Details
        public string Brand { get; set; }
        public string GTIN { get; set; }
        public string MPN { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        // Related Data
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<ProductImageDto> ProductImages { get; set; } = new();
        public List<ProductTagDto> ProductTags { get; set; } = new();
        public List<ProductAttributeCombinationDto> AttributeCombinations { get; set; } = new();

        public string SeoFriendlyUrl => $"/products/{Slug}";
    }
}
