using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Dtos.ProductTagDtos;

namespace ECommerce.API.Dtos.Products
{
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }

        public decimal BasePrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public bool InStock { get; set; }
        public int Quantity { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<ProductImageDetailDto> ProductImages { get; set; }
        public List<ProductTagDetailDto> ProductTags { get; set; }
        public List<AttributeCombinationDetailDto> AttributeCombinations { get; set; }
        public List<ProductReviewDto> Reviews { get; set; }
        public int ReviewCount { get; set; }
        public int QuestionCount { get; set; }

        // SEO bilgileri (sayfa icin gerekli olabilir)
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string SeoFriendlyUrl { get; set; }
    }
}
