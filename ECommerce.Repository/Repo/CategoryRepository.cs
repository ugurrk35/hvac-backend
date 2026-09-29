using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Category> GetBySlugAsync(string slug)
        {
            return await _dbContext.Categories
                .Include(c => c.products)
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        }

        public async Task<IReadOnlyList<Category>> GetAllActiveAsync()
        {
            return await _dbContext.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _dbContext.Categories.AnyAsync(c => c.Slug == slug);
        }

        public async Task<Category> GetByIdWithProductsAsync(int id)
        {
            return await _dbContext.Categories
                .Include(c => c.products)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<IReadOnlyList<Category>> SearchAsync(string keyword)
        {
            return await _dbContext.Categories
                .Where(c =>
                    c.IsActive &&
                    (c.Name.Contains(keyword) || c.Description.Contains(keyword)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Category>> GetTopCategoriesAsync(int count)
        {
            return await _dbContext.Categories
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.products.Count) // Ürün sayısına göre sırala
                .Take(count)
                .Include(c => c.products)
                .ToListAsync();
        }

        public async Task<bool> UpdateSlugAsync(int categoryId, string newSlug)
        {
            var category = await _dbContext.Categories.FindAsync(categoryId);
            if (category == null) return false;

            category.Slug = newSlug;
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByIdAsync(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null) return false;

            category.IsActive = false; // Soft delete
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
