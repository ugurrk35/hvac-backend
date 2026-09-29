using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category> GetBySlugAsync(string slug);
        Task<IReadOnlyList<Category>> GetAllActiveAsync();
        Task<bool> SlugExistsAsync(string slug);
        Task<Category> GetByIdWithProductsAsync(int id);
        Task<IReadOnlyList<Category>> SearchAsync(string keyword);
        Task<IReadOnlyList<Category>> GetTopCategoriesAsync(int count);
        Task<bool> UpdateSlugAsync(int categoryId, string newSlug);
        Task<bool> DeleteByIdAsync(int id);
    }
}
