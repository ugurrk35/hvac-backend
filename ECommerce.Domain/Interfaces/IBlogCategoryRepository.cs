using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IBlogCategoryRepository : IRepository<BlogCategory>
    {
        Task<BlogCategory> GetCategoryWithPostsAsync(int categoryId);
        Task<IReadOnlyList<BlogCategory>> GetAllActiveCategoriesAsync();
        Task<BlogCategory> GetByIdDetailAsync(int id);
    }
}
