using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos.BlogDtos;
using ECommerce.Service.Mapping.Manual;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class BlogCategoryService : Service<BlogCategory>, IBlogCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogCategoryService(IBlogCategoryRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BlogCategory> CreateCategoryAsync(BlogCategoryCreateDto dto)
        {
            var category = new BlogCategory
            {
                Name = dto.Name,
                Slug = dto.Slug.ToLower(),
                Description = dto.Description,
                MetaTitle = string.IsNullOrEmpty(dto.MetaTitle) ? dto.Name : dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                MetaKeywords = dto.MetaKeywords,
                OgTitle = dto.OgTitle,
                OgDescription = dto.OgDescription,
                OgImageUrl = dto.OgImageUrl,
                CanonicalUrl = dto.CanonicalUrl,
                IsActive = true,
                IsDeleted = false

            };

            var addedCategory = await _repository.AddAsync(category);
            await _unitOfWork.CompleteAsync();
            return addedCategory;
        }
        public async Task<BlogCategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.BlogCategoryRepository.GetByIdAsync(id);
            if (category == null) return null;

            return category.ToDto();
        }
        public async Task<BlogCategory> UpdateCategoryAsync(int id, BlogCategoryCreateDto dto)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) throw new Exception("Category not found");

            category.Name = dto.Name;
            category.Slug = dto.Slug.ToLower();
            category.Description = dto.Description;
            category.MetaTitle = string.IsNullOrEmpty(dto.MetaTitle) ? dto.Name : dto.MetaTitle;
            category.MetaDescription = dto.MetaDescription;
            category.MetaKeywords = dto.MetaKeywords;
            category.OgTitle = dto.OgTitle;
            category.OgDescription = dto.OgDescription;
            category.OgImageUrl = dto.OgImageUrl;
            category.CanonicalUrl = dto.CanonicalUrl;

            await _repository.UpdateAsync(category);
            await _unitOfWork.CompleteAsync();
            return category;
        }

        public async Task<IReadOnlyList<BlogCategory>> GetAllActiveCategoriesAsync()
        {
            return await ((IBlogCategoryRepository)_repository).GetAllActiveCategoriesAsync();
        }

        public async Task<BlogCategory> GetCategoryBySlugAsync(string slug)
        {
            return await ((IBlogCategoryRepository)_repository).Query()
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive && !c.IsDeleted);
        }

        public async Task<BlogCategoryDto> GetCategoryByIdDetailAsync(int id)
        {
            var category = await _unitOfWork.BlogCategoryRepository.GetByIdDetailAsync(id);
            if (category == null) return null;

            return category.ToDto();
        }
    }
}
