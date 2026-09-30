using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class CreateProductDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(300)]
        public string Slug { get; set; }

        [Required]
        [StringLength(100)]
        public string SKU { get; set; }

        [StringLength(500)]
        public string ShortDescription { get; set; }

        public string Description { get; set; }
        public string TechnicalDetails { get; set; }
        public string DeliveryInstallationDetails { get; set; }
        public string DocumentsDetails { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }

        public bool IsPublished { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        // SEO Fields
        [StringLength(60)]
        public string MetaTitle { get; set; }

        [StringLength(160)]
        public string MetaDescription { get; set; }

        [StringLength(200)]
        public string MetaKeywords { get; set; }

        public string CanonicalUrl { get; set; }

        [StringLength(60)]
        public string OgTitle { get; set; }

        [StringLength(160)]
        public string OgDescription { get; set; }

        public string OgImage { get; set; }

        public string TwitterCardType { get; set; } = "summary_large_image";

        // Product Details
        [StringLength(100)]
        public string Brand { get; set; }
        public string BrandLogoUrl { get; set; }
        public List<string> CardHighlights { get; set; } = new();

        [StringLength(50)]
        public string GTIN { get; set; }

        [StringLength(50)]
        public string MPN { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public List<int> AdditionalCategoryIds { get; set; } = new();
        public List<int> CampaignPackageIds { get; set; } = new();

        // Ana ürün fiyatı
        //public ProductPriceDto ProductPrice { get; set; }

        // Related Data
        public List<int> ProductTagIds { get; set; } = new();
        public List<CreateProductImageDto> ProductImages { get; set; } = new();
        public List<CreateProductAttributeCombinationDto> AttributeCombinations { get; set; } = new();

        // İlgili ürünler (opsiyonel, en fazla 4 adet)
        public List<int> RelatedProductIds { get; set; } = new();
    }
}
