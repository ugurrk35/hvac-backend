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
    public class BlogCategoryRepository : GenericRepository<BlogCategory>, IBlogCategoryRepository
    {
        public BlogCategoryRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<BlogCategory> GetCategoryWithPostsAsync(int categoryId)
        {
            return await _dbContext.BlogCategories
                .Include(c => c.BlogPosts.Where(p => p.IsPublished && !p.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive && !c.IsDeleted);
        }

        public async Task<IReadOnlyList<BlogCategory>> GetAllActiveCategoriesAsync()
        {
            return await _dbContext.BlogCategories
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<BlogCategory> GetByIdDetailAsync(int id)
        {
            return await _dbContext.BlogCategories
           //.Include(c => c.BlogPosts) 
           .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
