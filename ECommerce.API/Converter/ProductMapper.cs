using ECommerce.API.Dtos.Products;
using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Dtos.ProductTagDtos;
using System.Linq;
using System.Text.Json;

namespace ECommerce.API.Converter
{
    public static class ProductMapper
    {
        public static ProductDetailDto ToCustomerDto(Product product)
        {
            if (product == null) return null;

            return new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                TechnicalDetails = product.TechnicalDetails,
                DeliveryInstallationDetails = product.DeliveryInstallationDetails,
                DocumentsDetails = product.DocumentsDetails,

                BasePrice = product.BasePrice,
                DiscountPrice = product.DiscountPrice ?? 0,
                Quantity = product.Quantity,
                InStock = product.Quantity > 0 || (product.ProductAttributeCombination?.Any(item => !item.IsDeleted && item.Quantity > 0) ?? false),
                EffectivePrice = product.DiscountPrice ?? product.BasePrice,

                CategoryId = product.CategoryId,
                AdditionalCategoryIds = ReadCategoryIds(product.AdditionalCategoryIdsJson),
                CategoryName = product.Category?.Name,
                BrandLogoUrl = product.BrandLogoUrl,
                CardHighlights = ReadHighlights(product.CardHighlightsJson),

                ProductImages = product.ProductImages?.Select(pi => new ProductImageDetailDto
                {
                    Url = pi.Image?.Url,
                    AltText = pi.Image?.AltText,
                    SortOrder = pi.SortOrder
                }).ToList(),

                ProductTags = product.ProductProductTags?
                    .Select(pt => new ProductTagDetailDto
                    {
                        Name = pt.ProductTag?.Name,
                        Slug = pt.ProductTag?.Slug
                    }).ToList(),

                AttributeCombinations = product.ProductAttributeCombination?
                    .Select(pc => new AttributeCombinationDetailDto
                    {
                        Id = pc.Id,
                        Sku = pc.Sku,
                        Price = pc.Price,
                        Quantity = pc.Quantity,
                        AttributeValues = pc.ProductAttributeCombinationValues?.Select(av => new AttributeValueDetailDto
                        {
                            AttributeId = av.ProductAttributeId,
                            AttributeValueId = av.ProductAttributeValueId,
                            AttributeName = av.ProductAttribute?.Name,
                            AttributeValue = av.ProductAttributeValue?.Value,
                            PersonalizationText = av.ProductAttribute.TextPrompt,
                            IsPersonalization = av.ProductAttribute.IsPersonalizationText,
                        }).ToList()
                    }).ToList(),

                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                SeoFriendlyUrl = product.GetSeoFriendlyUrl(),

                Reviews = product.Reviews?
                    .Where(r => r.IsApproved)
                    .Select(r => new ProductReviewDto
                    {
                        ReviewerName = r.ReviewerName,
                        ReviewerEmail = r.ReviewerEmail,
                        Title = r.Title,
                        Content = r.Content,
                        Rating = r.Rating,
                        IsApproved = r.IsApproved,
                        ReviewDate = r.ReviewDate,
                        Images = r.Photos?
                            .Where(p => p.Image != null)
                            .Select(p => new ProductReviewImageDto
                            {
                                Url = p.Image.Url,
                                Alt = p.Image.AltText
                            }).ToList() ?? new List<ProductReviewImageDto>()
                    }).ToList(),
                ReviewCount = product.Reviews?.Count(r => r.IsApproved) ?? 0,
                QuestionCount = 0
            };
        }

        private static List<int> ReadCategoryIds(string? value)
        {
            try { return JsonSerializer.Deserialize<List<int>>(value ?? "[]")?.Where(id => id > 0).Distinct().ToList() ?? new(); }
            catch { return new(); }
        }

        private static List<string> ReadHighlights(string? value)
        {
            try { return JsonSerializer.Deserialize<List<string>>(value ?? "[]")?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Take(3).ToList() ?? new(); }
            catch { return new(); }
        }
    }
}
