using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.BlogDtos;

namespace ECommerce.Service.Mapping.Manual;

public static class BlogMappings
{
    public static BlogCategoryDto ToDto(this BlogCategory source) => new()
    {
        Id = source.Id, Name = source.Name, Slug = source.Slug, Description = source.Description,
        MetaTitle = source.MetaTitle, MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords,
        OgTitle = source.OgTitle, OgDescription = source.OgDescription, OgImageUrl = source.OgImageUrl,
        CanonicalUrl = source.CanonicalUrl, PostCount = source.BlogPosts?.Count ?? 0,
        Posts = source.BlogPosts?.Select(post => post.ToShortDto()).ToList() ?? new()
    };

    public static BlogPostShortDto ToShortDto(this BlogPost source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Slug = source.Slug,
        CreatedDate = source.CreatedAt
    };

    public static BlogPostEditDto ToEditDto(this BlogPost source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Slug = source.Slug,
        Excerpt = source.Excerpt,
        Content = source.Content,
        PublishDate = source.PublishDate,
        IsPublished = source.IsPublished,
        IsFeatured = source.IsFeatured,
        Views = source.Views,
        BlogCategoryId = source.BlogCategoryId,
        BlogCategoryName = source.BlogCategory?.Name,
        Images = source.BlogPostImages?.Select(image => new BlogPostImageDto
        {
            Id = image.Id,
            Url = image.Image?.Url
        }).ToList() ?? new(),
        Tags = source.BlogPostTags?.Where(tag => tag.BlogTag != null).Select(tag => new BlogPostTagDto
        {
            Id = tag.BlogTag.Id,
            Name = tag.BlogTag.Name
        }).ToList() ?? new(),
        Comments = source.Comments?.Select(comment => new BlogCommentDto
        {
            Id = comment.Id,
            Name = comment.AuthorName,
            Content = comment.Content,
            CreatedDate = comment.CommentDate
        }).ToList() ?? new(),
        MetaTitle = source.MetaTitle,
        MetaDescription = source.MetaDescription,
        MetaKeywords = source.MetaKeywords,
        OgTitle = source.OgTitle,
        OgDescription = source.OgDescription,
        OgImageUrl = source.OgImageUrl,
        CanonicalUrl = source.CanonicalUrl,
        SchemaJson = source.SchemaJson
    };
}
