using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Repo.Blog;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos.BlogDtos;
using ECommerce.Service.Response;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class BlogPostService : Service<BlogPost>, IBlogPostService
    {
        private static readonly HtmlSanitizer ContentSanitizer = CreateContentSanitizer();
        private readonly IUnitOfWork _unitOfWork;

        public BlogPostService(IBlogPostRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static HtmlSanitizer CreateContentSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith([
                "a", "b", "blockquote", "br", "code", "details", "div", "em", "h1", "h2", "h3", "h4", "h5", "h6",
                "hr", "i", "img", "li", "ol", "p", "pre", "span", "strong", "summary", "u", "ul"
            ]);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(["alt", "height", "href", "rel", "src", "target", "title", "width"]);
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
            return sanitizer;
        }
        public async Task<BlogPostDetailDto> GetPostDetailAsync(int postId)
        {
            var post = await _unitOfWork.BlogPostRepository.GetPostWithDetailsAsync(postId);

            if (post == null)
                return null;

            var dto = new BlogPostDetailDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = ContentSanitizer.Sanitize(post.Content ?? string.Empty),
                PublishDate = post.PublishDate,
                IsPublished = post.IsPublished,
                IsFeatured = post.IsFeatured,
                Views = post.Views,
                BlogCategoryId = post.BlogCategoryId,
                BlogCategoryName = post.BlogCategory?.Name,
                Images = post.BlogPostImages.Select(i => i.Image.Url).ToList(),
                Tags = post.BlogPostTags.Select(t => t.BlogTag.Name).ToList(),
                Comments = post.Comments.Select(c => new BlogCommentDto
                {
                    Id = c.Id,
                    Name = c.AuthorName,
                    Content = c.Content,
                    CreatedDate = c.CreatedAt
                }).ToList(),
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                MetaKeywords = post.MetaKeywords,
                OgTitle = post.OgTitle,
                OgDescription = post.OgDescription,
                OgImageUrl = post.OgImageUrl,
                CanonicalUrl = post.CanonicalUrl,
                SchemaJson = post.SchemaJson
            };

            return dto;
        }

        public async Task<PagedResponse<BlogPostListDto>> GetPagedPostsAsync(
        int pageNumber, int pageSize, int? categoryId = null, int? tagId = null)
        {
            var (items, totalCount) = await _unitOfWork.BlogPostRepository
                .GetPagedPostsAsync(pageNumber, pageSize, categoryId, tagId);

           

            var postDtos = new List<BlogPostListDto>();

            foreach (var item in items)
            {
                var postDto = new BlogPostListDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Slug = item.Slug,
                    Excerpt = item.Excerpt,
                    PublishDate = item.PublishDate,
                    Views = item.Views,
                    ImageUrl = item.OgImageUrl,
                    BlogCategory = new BlogCategoryListDto
                    {
                        Name = item.BlogCategory != null ? item.BlogCategory.Name : "Uncategorized"
                    }
                };

                postDtos.Add(postDto);
            }

            return new PagedResponse<BlogPostListDto>
            {
                Items = postDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Success = true,
                Message = "Postlar başarıyla getirildi"
            };
        }

        public async Task<BlogPost> CreatePostAsync(BlogPostCreateDto dto)
        {
            var post = new BlogPost
            {
                Title = dto.Title,
                Slug = dto.Slug.ToLower(),
                Content = ContentSanitizer.Sanitize(dto.Content ?? string.Empty),
                Excerpt = dto.Excerpt,
                BlogCategoryId = dto.BlogCategoryId,
                PublishDate = dto.PublishDate,
                IsPublished = dto.IsPublished,
                IsFeatured = dto.IsFeatured,
                MetaTitle = string.IsNullOrEmpty(dto.MetaTitle) ? dto.Title : dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                MetaKeywords = dto.MetaKeywords,
                OgTitle = dto.OgTitle,
                OgDescription = dto.OgDescription,
                OgImageUrl = dto.OgImageUrl,
                CanonicalUrl = dto.CanonicalUrl,
                IsActive = true,
                SchemaJson =dto.SchemaJson,
                BlogPostTags = dto.TagIds != null && dto.TagIds.Any(tid => tid != 0)
        ? dto.TagIds.Where(tid => tid != 0)
                    .Select(tid => new BlogPostTag { BlogTagId = tid })
                    .ToList()
        : new List<BlogPostTag>(),


                BlogPostImages = dto.ImageIds.Select(imgId => new BlogPostImage { ImageId = imgId, SortOrder = 0  ,}).ToList()
            };

            var addedPost = await _repository.AddAsync(post);
            await _unitOfWork.CompleteAsync();
            return addedPost;
        }

        public async Task<BlogPost> UpdatePostAsync(int id, BlogPostCreateDto dto)
        {
            var post = await _unitOfWork.BlogPostRepository.GetPostWithDetailsAsync(id);
            if (post == null) throw new Exception("Blog post not found");

            post.Title = dto.Title;
            post.Slug = dto.Slug.ToLower();
            post.Content = ContentSanitizer.Sanitize(dto.Content ?? string.Empty);
            post.Excerpt = dto.Excerpt;
            post.BlogCategoryId = dto.BlogCategoryId;
            post.IsPublished = dto.IsPublished;
            post.IsFeatured = dto.IsFeatured;
            post.MetaTitle = string.IsNullOrEmpty(dto.MetaTitle) ? dto.Title : dto.MetaTitle;
            post.MetaDescription = dto.MetaDescription;
            post.MetaKeywords = dto.MetaKeywords;
            post.OgTitle = dto.OgTitle;
            post.OgDescription = dto.OgDescription;
            post.OgImageUrl = dto.OgImageUrl;
            post.CanonicalUrl = dto.CanonicalUrl;

            // Tag ilişkileri
            post.BlogPostTags.Clear();
            foreach (var tagId in dto.TagIds)
                post.BlogPostTags.Add(new BlogPostTag { BlogPostId = post.Id, BlogTagId = tagId });

            // Image ilişkileri
            post.BlogPostImages.Clear();
            foreach (var imgId in dto.ImageIds)
                post.BlogPostImages.Add(new BlogPostImage { BlogPostId = post.Id, ImageId = imgId, SortOrder = 0 });

            await _repository.UpdateAsync(post);
            await _unitOfWork.CompleteAsync();
            return post;
        }

        public async Task<BlogPost> GetPostBySlugAsync(string slug)
        {
            var post = await ((IBlogPostRepository)_repository).Query()
                .Include(p => p.BlogCategory)
                .Include(p => p.BlogPostTags).ThenInclude(t => t.BlogTag)
                .Include(p => p.BlogPostImages).ThenInclude(i => i.Image)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && !p.IsDeleted);
            if (post != null) post.Content = ContentSanitizer.Sanitize(post.Content ?? string.Empty);
            return post;
        }

        public async Task<IReadOnlyList<BlogPost>> GetPublishedPostsAsync()
        {
            var posts = await ((IBlogPostRepository)_repository).GetPublishedPostsAsync();
            foreach (var post in posts) post.Content = ContentSanitizer.Sanitize(post.Content ?? string.Empty);
            return posts;
        }

        public async Task PublishPostAsync(int id, DateTime? publishDate = null)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) throw new Exception("Blog post not found");

            post.IsPublished = true;
            post.PublishDate = publishDate ?? DateTime.UtcNow;
            await _repository.UpdateAsync(post);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UnpublishPostAsync(int id)
        {
            var post = await _repository.GetByIdAsync(id);
            if (post == null) throw new Exception("Blog post not found");

            post.IsPublished = false;
            await _repository.UpdateAsync(post);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IReadOnlyList<BlogPost>> SearchPostsAsync(string query)
        {
            var posts = await ((IBlogPostRepository)_repository).Query()
                .Where(p => p.Title.Contains(query) || p.Content.Contains(query) || p.Excerpt.Contains(query))
                .Include(p => p.BlogCategory)
                .ToListAsync();
            foreach (var post in posts) post.Content = ContentSanitizer.Sanitize(post.Content ?? string.Empty);
            return posts;
        }
        public async Task<BlogPostEditDto> GetPostForEditAsync(int postId)
        {
            // Repository'den hazır metodu kullanıyoruz
            var post = await _unitOfWork.BlogPostRepository.GetPostWithDetailsAsync(postId);

            if (post == null)
                return null;

            var dto = new BlogPostEditDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = ContentSanitizer.Sanitize(post.Content ?? string.Empty),
                PublishDate = post.PublishDate,
                IsPublished = post.IsPublished,
                IsFeatured = post.IsFeatured,
                Views = post.Views,
                BlogCategoryId = post.BlogCategoryId,
                BlogCategoryName = post.BlogCategory?.Name,
                Images = post.BlogPostImages.Select(i => new BlogPostImageDto
                {
                    Id = i.Id,
                    Url = i.Image.Url
                }).ToList(),
                Tags = post.BlogPostTags.Select(t => new BlogPostTagDto
                {
                    Id = t.BlogTagId,
                    Name = t.BlogTag.Name
                }).ToList(),
                Comments = post.Comments.Select(c => new BlogCommentDto
                {
                    Id = c.Id,
                    Name = c.AuthorName,
                    Content = c.Content,
                    CreatedDate = c.CreatedAt
                }).ToList(),
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                MetaKeywords = post.MetaKeywords,
                OgTitle = post.OgTitle,
                OgDescription = post.OgDescription,
                OgImageUrl = post.OgImageUrl,
                CanonicalUrl = post.CanonicalUrl,
                SchemaJson = post.SchemaJson
            };

            return dto;
        }

    }
}
