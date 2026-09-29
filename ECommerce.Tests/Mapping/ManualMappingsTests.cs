using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Mapping.Manual;
using Xunit;

namespace ECommerce.Tests.Mapping;

public class ManualMappingsTests
{
    [Fact]
    public void ProductToDto_MapsRelatedData()
    {
        var product = new Product
        {
            Id = 7,
            Name = "Body",
            Category = new ECommerce.Domain.Entity.Category { Name = "Bebek" },
            ProductImages = new List<ProductImage>
            {
                new() { Id = 2, SortOrder = 1, Image = new Image { Url = "/second.jpg" } },
                new() { Id = 1, SortOrder = 0, Image = new Image { Url = "/main.jpg" } }
            },
            ProductProductTags = new List<ProductProductTag>
            {
                new() { ProductTag = new ProductTag { Id = 3, Name = "Yeni" } }
            }
        };

        var dto = product.ToDto();
        var listDto = product.ToListDto();

        Assert.Equal("Bebek", dto.CategoryName);
        Assert.Equal(2, dto.ProductImages.Count);
        Assert.Equal("Yeni", dto.ProductTags.Single().Name);
        Assert.Equal("/main.jpg", listDto.MainImageUrl);
    }

    [Fact]
    public void ProductToDto_AllowsMissingNavigations()
    {
        var dto = new Product { Id = 1, Name = "Ürün" }.ToDto();

        Assert.Null(dto.CategoryName);
        Assert.Empty(dto.ProductImages);
        Assert.Empty(dto.ProductTags);
        Assert.Empty(dto.AttributeCombinations);
    }

    [Fact]
    public void ProductAttributeUpdate_AppliesOnlySupportedValues()
    {
        var attribute = new ECommerce.Domain.Entity.ProductAttribute { Id = 5, Name = "Eski", IsActive = true };
        var update = new ProductAttributeUpdateDto
        {
            Name = "Renk",
            IsPersonalizationText = true,
            TextPrompt = "Not",
            MaxLength = 20,
            ProductAttributeValues = new List<ProductAttributeValueUpdateDto>
            {
                new() { Id = 9, Value = "Mavi", PriceModifier = 4 }
            }
        };

        update.ApplyTo(attribute);

        Assert.Equal("Renk", attribute.Name);
        Assert.True(attribute.IsActive);
        Assert.Single(attribute.ProductAttributeValues);
        Assert.Equal(5, attribute.ProductAttributeValues.Single().ProductAttributeId);
    }

    [Fact]
    public void BlogPostToEditDto_MapsImagesTagsAndComments()
    {
        var post = new BlogPost
        {
            Id = 4,
            Title = "Rehber",
            BlogCategory = new BlogCategory { Name = "Bakım" },
            BlogPostImages = new List<BlogPostImage> { new() { Id = 10, Image = new Image { Url = "/post.jpg" } } },
            BlogPostTags = new List<BlogPostTag> { new() { BlogTag = new BlogTag { Id = 11, Name = "İpucu" } } },
            Comments = new List<BlogPostComment> { new() { Id = 12, AuthorName = "Ada", Content = "Güzel", CommentDate = new DateTime(2026, 1, 1) } }
        };

        var dto = post.ToEditDto();

        Assert.Equal("Bakım", dto.BlogCategoryName);
        Assert.Equal("/post.jpg", dto.Images.Single().Url);
        Assert.Equal("İpucu", dto.Tags.Single().Name);
        Assert.Equal("Ada", dto.Comments.Single().Name);
    }
}
