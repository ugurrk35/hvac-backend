using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.BlogDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IBlogCategoryService : IService<BlogCategory>
    {
        Task<BlogCategory> CreateCategoryAsync(BlogCategoryCreateDto dto);
        Task<BlogCategory> UpdateCategoryAsync(int id, BlogCategoryCreateDto dto);
        Task<IReadOnlyList<BlogCategory>> GetAllActiveCategoriesAsync();
        Task<BlogCategory> GetCategoryBySlugAsync(string slug);
   
        Task<BlogCategoryDto> GetCategoryByIdDetailAsync(int id);
    }
}
