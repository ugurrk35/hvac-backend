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
    public class BlogTagRepository : GenericRepository<BlogTag>, IBlogTagRepository
    {
        public BlogTagRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<BlogTag> GetTagWithPostsAsync(int tagId)
        {
            return await _dbContext.BlogTags
                .Include(t => t.BlogPostTags)
                    .ThenInclude(pt => pt.BlogPost)
                .FirstOrDefaultAsync(t => t.Id == tagId && t.IsActive && !t.IsDeleted);
        }
    }
}
