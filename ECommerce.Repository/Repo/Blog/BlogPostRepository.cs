using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo.Blog
{
    public class BlogPostRepository : GenericRepository<BlogPost>, IBlogPostRepository
    {
        public BlogPostRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        public async Task<(IReadOnlyList<BlogPost> Items, int TotalCount)> GetPagedPostsAsync(
    int pageNumber, int pageSize, int? categoryId = null, int? tagId = null)
        {
            var query = _dbContext.BlogPosts
                .AsNoTracking() // tracking kapat, performans artar
                .Where(p => !p.IsDeleted);

            if (categoryId.HasValue)
                query = query.Where(p => p.BlogCategoryId == categoryId.Value);

            if (tagId.HasValue)
                query = query.Where(p => p.BlogPostTags.Any(t => t.BlogTagId == tagId.Value));

            // önce sadece count çekiyoruz (çok hızlıdır)
            var totalCount = await query.CountAsync();

            // sadece ihtiyaç olan navigation’ları dahil et
            var items = await query
                .OrderByDescending(p => p.PublishDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new BlogPost
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    PublishDate = p.PublishDate,
                    Views = p.Views,
                    OgImageUrl = p.BlogPostImages
                        .OrderBy(image => image.SortOrder)
                        .Select(image => image.Image.Url)
                        .FirstOrDefault() ?? p.OgImageUrl,

                    BlogCategory = new BlogCategory
                    {
                        Id = p.BlogCategory.Id,
                        Name = p.BlogCategory.Name,
                        Slug = p.BlogCategory.Slug
                    },
                })
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<BlogPost> GetPostWithDetailsAsync(int postId)
        {
            return await _dbContext.BlogPosts
                .Include(p => p.BlogCategory)
                .Include(p => p.BlogPostImages)
                    .ThenInclude(i => i.Image)
                .Include(p => p.BlogPostTags)
                    .ThenInclude(t => t.BlogTag)
                .Include(p => p.Comments.Where(c => c.IsApproved))
                .FirstOrDefaultAsync(p => p.Id == postId  && !p.IsDeleted);
        }

        public async Task<IReadOnlyList<BlogPost>> GetPublishedPostsAsync()
        {
            return await _dbContext.BlogPosts
                .Where(p => p.IsPublished && p.IsActive && !p.IsDeleted)
                .Include(p => p.BlogCategory)
                .Include(p => p.BlogPostImages)
                .ToListAsync();
        }
    }
}
