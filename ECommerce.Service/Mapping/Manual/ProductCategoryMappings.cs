using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.CategoryDtos;
using ECommerce.Service.Dtos.ImageDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Dtos.ProductTagDtos;
using System.Text.Json;

namespace ECommerce.Service.Mapping.Manual;

public static class ProductCategoryMappings
{
    public static CategoryDto ToDto(this Category source) => new()
    {
        Id = source.Id, Name = source.Name, Slug = source.Slug, Description = source.Description,
        MetaTitle = source.MetaTitle, MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords,
        CanonicalUrl = source.CanonicalUrl, OgTitle = source.OgTitle, OgDescription = source.OgDescription,
        OgImage = source.OgImage, TwitterCardType = source.TwitterCardType,
        Products = source.products?.Select(product => new ProductSummaryDto { Id = product.Id, Name = product.Name }).ToList() ?? new()
    };

    public static Category ToEntity(this CategoryCreateDto source) => new()
    {
        Name = source.Name, Slug = source.Slug, Description = source.Description,
        MetaTitle = source.MetaTitle, MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords,
        CanonicalUrl = source.CanonicalUrl, OgTitle = source.OgTitle, OgDescription = source.OgDescription,
        OgImage = source.OgImage, TwitterCardType = source.TwitterCardType
    };

    public static Category ToEntity(this CategoryUpdateDto source) => new()
    {
        Id = source.Id, Name = source.Name, Slug = source.Slug, Description = source.Description,
        MetaTitle = source.MetaTitle, MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords,
        CanonicalUrl = source.CanonicalUrl, OgTitle = source.OgTitle, OgDescription = source.OgDescription,
        OgImage = source.OgImage, TwitterCardType = source.TwitterCardType
    };

    public static ProductListDto ToListDto(this Product source)
    {
        var approvedReviews = source.Reviews?.Where(review => review.IsApproved && !review.IsDeleted).ToList() ?? new();
        var attributeValues = source.ProductAttributeCombination?
            .Where(combination => !combination.IsDeleted)
            .SelectMany(combination => combination.ProductAttributeCombinationValues ?? new List<ProductAttributeCombinationValue>())
            .Where(value => !value.IsDeleted && value.ProductAttributeValue != null && value.ProductAttribute != null)
            .ToList() ?? new();
        var sizeOptions = attributeValues
            .Where(value => value.ProductAttribute.Name.Contains("Beden", StringComparison.OrdinalIgnoreCase) || value.ProductAttribute.Name.Contains("Ölçü", StringComparison.OrdinalIgnoreCase))
            .Select(value => value.ProductAttributeValue!.Value).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
        var colorOptions = attributeValues
            .Where(value => value.ProductAttribute.Name.Contains("Renk", StringComparison.OrdinalIgnoreCase))
            .Select(value => value.ProductAttributeValue!.Value).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();

        return new ProductListDto
        {
            Id = source.Id, Name = source.Name, Slug = source.Slug, SKU = source.SKU,
            ShortDescription = source.ShortDescription, BasePrice = source.BasePrice, DiscountPrice = source.DiscountPrice,
            IsPublished = source.IsPublished, Quantity = source.Quantity, Brand = source.Brand, BrandLogoUrl = source.BrandLogoUrl, CardHighlights = DeserializeHighlights(source.CardHighlightsJson), CreatedAt = source.CreatedAt,
            CategoryId = source.CategoryId, CategoryName = source.Category?.Name,
            MainImageUrl = source.ProductImages?.OrderBy(image => image.SortOrder).Select(image => image.Image?.Url)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)),
            TagNames = source.ProductProductTags?.Select(tag => tag.ProductTag?.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList() ?? new(),
            ReviewCount = approvedReviews.Count,
            AverageRating = approvedReviews.Count > 0 ? approvedReviews.Average(review => review.Rating) : 0,
            SizeOptionCount = sizeOptions.Count,
            SizeOptions = sizeOptions,
            ColorOptions = colorOptions,
            IsNew = source.CreatedAt >= DateTime.UtcNow.AddDays(-30)
        };
    }

    public static ProductDto ToDto(this Product source) => new()
    {
        Id = source.Id, Name = source.Name, Slug = source.Slug, SKU = source.SKU, ShortDescription = source.ShortDescription,
        Description = source.Description, TechnicalDetails = source.TechnicalDetails, DeliveryInstallationDetails = source.DeliveryInstallationDetails, DocumentsDetails = source.DocumentsDetails, BasePrice = source.BasePrice, DiscountPrice = source.DiscountPrice,
        IsPublished = source.IsPublished, Quantity = source.Quantity, MetaTitle = source.MetaTitle,
        MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords, CanonicalUrl = source.CanonicalUrl,
        OgTitle = source.OgTitle, OgDescription = source.OgDescription, OgImage = source.OgImage,
        TwitterCardType = source.TwitterCardType, Brand = source.Brand, BrandLogoUrl = source.BrandLogoUrl, CardHighlights = DeserializeHighlights(source.CardHighlightsJson), GTIN = source.GTIN, MPN = source.MPN,
        CreatedAt = source.CreatedAt, CreatedBy = source.CreatedBy, LastModifiedAt = source.LastModifiedAt,
        LastModifiedBy = source.LastModifiedBy, IsDeleted = source.IsDeleted, CategoryId = source.CategoryId,
        AdditionalCategoryIds = DeserializeCategoryIds(source.AdditionalCategoryIdsJson),
        CategoryName = source.Category?.Name,
        ProductImages = source.ProductImages?.Select(image => image.ToDto()).ToList() ?? new(),
        ProductTags = source.ProductProductTags?.Where(tag => tag.ProductTag != null).Select(tag => tag.ProductTag.ToDto()).ToList() ?? new(),
        AttributeCombinations = source.ProductAttributeCombination?.Select(combination => combination.ToDto()).ToList() ?? new()
    };

    public static Product ToEntity(this CreateProductDto source) => new()
    {
        Name = source.Name, Slug = source.Slug, SKU = source.SKU, ShortDescription = source.ShortDescription,
        Description = source.Description, TechnicalDetails = source.TechnicalDetails, DeliveryInstallationDetails = source.DeliveryInstallationDetails, DocumentsDetails = source.DocumentsDetails, BasePrice = source.BasePrice, DiscountPrice = source.DiscountPrice,
        IsPublished = source.IsPublished, Quantity = source.Quantity, MetaTitle = source.MetaTitle,
        MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords, CanonicalUrl = source.CanonicalUrl,
        OgTitle = source.OgTitle, OgDescription = source.OgDescription, OgImage = source.OgImage,
        TwitterCardType = source.TwitterCardType, Brand = source.Brand, BrandLogoUrl = source.BrandLogoUrl, CardHighlightsJson = SerializeHighlights(source.CardHighlights), GTIN = source.GTIN, MPN = source.MPN,
        CategoryId = source.CategoryId, AdditionalCategoryIdsJson = SerializeCategoryIds(source.AdditionalCategoryIds, source.CategoryId), IsActive = true, IsDeleted = false
    };

    public static void ApplyTo(this UpdateProductDto source, Product target)
    {
        target.Name = source.Name; target.Slug = source.Slug; target.SKU = source.SKU; target.ShortDescription = source.ShortDescription;
        target.Description = source.Description; target.TechnicalDetails = source.TechnicalDetails; target.DeliveryInstallationDetails = source.DeliveryInstallationDetails; target.DocumentsDetails = source.DocumentsDetails; target.BasePrice = source.BasePrice; target.DiscountPrice = source.DiscountPrice;
        target.IsPublished = source.IsPublished; target.Quantity = source.Quantity; target.MetaTitle = source.MetaTitle;
        target.MetaDescription = source.MetaDescription; target.MetaKeywords = source.MetaKeywords; target.CanonicalUrl = source.CanonicalUrl;
        target.OgTitle = source.OgTitle; target.OgDescription = source.OgDescription; target.OgImage = source.OgImage;
        target.TwitterCardType = source.TwitterCardType; target.Brand = source.Brand; target.BrandLogoUrl = source.BrandLogoUrl; target.CardHighlightsJson = SerializeHighlights(source.CardHighlights); target.GTIN = source.GTIN; target.MPN = source.MPN;
        target.CategoryId = source.CategoryId; target.AdditionalCategoryIdsJson = SerializeCategoryIds(source.AdditionalCategoryIds, source.CategoryId); target.LastModifiedAt = DateTime.UtcNow;
    }

    public static ProductImage ToEntity(this CreateProductImageDto source) => new() { ImageId = source.ImageId, SortOrder = source.SortOrder };
    public static ProductImage ToEntity(this UpdateProductImageDto source) => new() { ImageId = source.ImageId, SortOrder = source.SortOrder };
    public static ProductAttributeCombination ToEntity(this CreateProductAttributeCombinationDto source) => new() { Sku = source.Sku, Price = source.Price, Quantity = source.Quantity };
    public static ProductAttributeCombination ToEntity(this ECommerce.Service.Dtos.ProductAttributeCombinationDtos.UpdateProductAttributeCombinationDto source) => new() { Id = source.Id, ProductId = source.ProductId, Sku = source.Sku, Price = source.Price, Quantity = source.Quantity };
    public static ProductPrice ToEntity(this ProductPriceDto source) => new() { ProductId = source.ProductId, ProductAttributeCombinationId = source.ProductAttributeCombinationId, Price = source.Price, DiscountPrice = source.DiscountPrice, Currency = source.Currency, CustomerGroupId = source.CustomerGroupId, StartDate = source.StartDate, EndDate = source.EndDate };
    public static ProductPriceDto ToDto(this ProductPrice source) => new() { ProductId = source.ProductId, ProductAttributeCombinationId = source.ProductAttributeCombinationId, Price = source.Price, DiscountPrice = source.DiscountPrice, Currency = source.Currency, CustomerGroupId = source.CustomerGroupId, StartDate = source.StartDate, EndDate = source.EndDate };
    public static void ApplyTo(this ProductPriceDto source, ProductPrice target) { target.ProductId = source.ProductId; target.ProductAttributeCombinationId = source.ProductAttributeCombinationId; target.Price = source.Price; target.DiscountPrice = source.DiscountPrice; target.Currency = source.Currency; target.CustomerGroupId = source.CustomerGroupId; target.StartDate = source.StartDate; target.EndDate = source.EndDate; }
    private static ProductImageDto ToDto(this ProductImage source) => new() { Id = source.Id, ProductId = source.ProductId, ImageId = source.ImageId, SortOrder = source.SortOrder, Image = source.Image == null ? null : new ImageDto { Id = source.Image.Id, Title = source.Image.Title, AltText = source.Image.AltText, Caption = source.Image.Caption, Url = source.Image.Url, Width = source.Image.Width, Height = source.Image.Height, FileExtension = source.Image.FileExtension, SizeInBytes = source.Image.SizeInBytes } };
    public static ProductTagDto ToDto(this ProductTag source) => new() { Id = source.Id, Name = source.Name, Slug = source.Slug, IsActive = source.IsActive };
    public static ProductTag ToEntity(this ProductTagCreateDto source) => new() { Name = source.Name, Slug = source.Slug, IsActive = source.IsActive };
    public static ProductTag ToEntity(this ProductTagUpdateDto source) => new() { Id = source.Id, Name = source.Name, Slug = source.Slug, IsActive = source.IsActive };
    public static ProductAttributeCombinationDto ToDto(this ProductAttributeCombination source) => new()
    {
        Id = source.Id,
        ProductId = source.ProductId,
        Sku = source.Sku,
        Price = source.Price,
        Quantity = source.Quantity,
        AttributeValues = source.ProductAttributeCombinationValues?.Select(value => new ProductAttributeCombinationValueDto
        {
            Id = value.Id,
            ProductAttributeId = value.ProductAttributeId,
            AttributeName = value.ProductAttributeValue?.ProductAttribute?.Name ?? value.ProductAttribute?.Name ?? string.Empty,
            ProductAttributeValueId = value.ProductAttributeValueId,
            AttributeValue = value.ProductAttributeValue?.Value ?? string.Empty,
            PersonalizationText = value.PersonalizationText ?? string.Empty
        }).ToList() ?? new()
    };

    private static string SerializeCategoryIds(IEnumerable<int>? ids, int primaryId) => JsonSerializer.Serialize((ids ?? []).Where(id => id > 0 && id != primaryId).Distinct().ToList());
    private static List<int> DeserializeCategoryIds(string? value)
    {
        try { return JsonSerializer.Deserialize<List<int>>(value ?? "[]")?.Where(id => id > 0).Distinct().ToList() ?? new(); }
        catch { return new(); }
    }

    private static string SerializeHighlights(IEnumerable<string>? values) =>
        JsonSerializer.Serialize((values ?? []).Select(value => value?.Trim()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(3).ToList());

    private static List<string> DeserializeHighlights(string? value)
    {
        try { return JsonSerializer.Deserialize<List<string>>(value ?? "[]")?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Take(3).ToList() ?? new(); }
        catch { return new(); }
    }
}
