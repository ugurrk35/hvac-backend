using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos;
using ECommerce.Service.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class CategoryService : Service<Category>, ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork) : base(categoryRepository, unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets a category by its slug including related products
        /// </summary>
        /// <param name="slug">Category slug</param>
        /// <returns>Category with products or null if not found</returns>
        public async Task<Category> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug cannot be null or empty", nameof(slug));

            return await _categoryRepository.GetBySlugAsync(slug);
        }

        /// <summary>
        /// Gets all active categories ordered by name
        /// </summary>
        /// <returns>List of active categories</returns>
        public async Task<IReadOnlyList<Category>> GetAllActiveAsync()
        {
            return await _categoryRepository.GetAllActiveAsync();
        }

        /// <summary>
        /// Checks if a slug already exists in the database
        /// </summary>
        /// <param name="slug">Slug to check</param>
        /// <returns>True if slug exists, false otherwise</returns>
        public async Task<bool> SlugExistsAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            return await _categoryRepository.SlugExistsAsync(slug);
        }

        /// <summary>
        /// Gets a category by ID including its products
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category with products or null if not found</returns>
        public async Task<Category> GetByIdWithProductsAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(id));

            return await _categoryRepository.GetByIdWithProductsAsync(id);
        }

        /// <summary>
        /// Searches categories by keyword in name or description
        /// </summary>
        /// <param name="keyword">Search keyword</param>
        /// <returns>List of matching categories</returns>
        public async Task<IReadOnlyList<Category>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<Category>();

            return await _categoryRepository.SearchAsync(keyword.Trim());
        }

        /// <summary>
        /// Gets top categories by product count
        /// </summary>
        /// <param name="count">Number of categories to return</param>
        /// <returns>List of top categories</returns>
        public async Task<IReadOnlyList<Category>> GetTopCategoriesAsync(int count = 10)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));

            return await _categoryRepository.GetTopCategoriesAsync(count);
        }

        /// <summary>
        /// Creates a new category with validation
        /// </summary>
        /// <param name="category">Category to create</param>
        /// <returns>Created category</returns>
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(category.Name))
                throw new ArgumentException("Category name is required", nameof(category));

            if (string.IsNullOrWhiteSpace(category.Slug))
                throw new ArgumentException("Category slug is required", nameof(category));

            // Check if slug already exists
            if (await SlugExistsAsync(category.Slug))
                throw new InvalidOperationException($"A category with slug '{category.Slug}' already exists");

            // Set default values
            category.IsActive = true;
            category.LastModifiedAt = DateTime.UtcNow;

            var result = await _unitOfWork.CategoryRepository.AddAsync(category);
            await _unitOfWork.CompleteAsync();

            return result;
        }

        /// <summary>
        /// Updates an existing category
        /// </summary>
        /// <param name="category">Category to update</param>
        /// <returns>Updated category</returns>
        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            if (category.Id <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(category));

            // Check if category exists
            var existingCategory = await GetByIdAsync(category.Id);
            if (existingCategory == null)
                throw new InvalidOperationException($"Category with ID {category.Id} not found");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(category.Name))
                throw new ArgumentException("Category name is required", nameof(category));

            if (string.IsNullOrWhiteSpace(category.Slug))
                throw new ArgumentException("Category slug is required", nameof(category));

            // Check if slug already exists for different category
            var slugExists = await _unitOfWork.CategoryRepository.SlugExistsAsync(category.Slug);
            if (slugExists && existingCategory.Slug != category.Slug)
            {
                var existingWithSlug = await _unitOfWork.CategoryRepository.GetBySlugAsync(category.Slug);
                if (existingWithSlug != null && existingWithSlug.Id != category.Id)
                    throw new InvalidOperationException($"A category with slug '{category.Slug}' already exists");
            }

            // Update fields
            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.Slug = category.Slug;
            existingCategory.IsActive = true;
            existingCategory.LastModifiedAt = DateTime.UtcNow;

            await _unitOfWork.CategoryRepository.UpdateAsync(existingCategory);
            await _unitOfWork.CompleteAsync();
            return existingCategory;
        }

        /// <summary>
        /// Updates only the slug of a category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="newSlug">New slug value</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateSlugAsync(int categoryId, string newSlug)
        {
            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));

            if (string.IsNullOrWhiteSpace(newSlug))
                throw new ArgumentException("New slug cannot be null or empty", nameof(newSlug));

            // Check if new slug already exists
            if (await SlugExistsAsync(newSlug))
            {
                var existingCategory = await GetBySlugAsync(newSlug);
                if (existingCategory != null && existingCategory.Id != categoryId)
                    throw new InvalidOperationException($"A category with slug '{newSlug}' already exists");
            }

            return await _unitOfWork.CategoryRepository.UpdateSlugAsync(categoryId, newSlug);
        }

        /// <summary>
        /// Soft deletes a category by ID
        /// </summary>
        /// <param name="id">Category ID to delete</param>
        /// <returns>True if successful, false if category not found</returns>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(id));

            // Check if category has products - optional business rule
            var categoryWithProducts = await GetByIdWithProductsAsync(id);
            if (categoryWithProducts?.products?.Any() == true)
            {
                // You might want to handle this differently based on business rules
                // For now, we'll allow deletion but you could throw an exception
                // throw new InvalidOperationException("Cannot delete category that contains products");
            }

            return await _unitOfWork.CategoryRepository.DeleteByIdAsync(id);
        }

        /// <summary>
        /// Gets category statistics
        /// </summary>
        /// <returns>Category statistics object</returns>
        public async Task<CategoryStatistics> GetCategoryStatisticsAsync()
        {
            var allCategories = await _unitOfWork.CategoryRepository.GetAllActiveAsync();
            var topCategories = await _unitOfWork.CategoryRepository.GetTopCategoriesAsync(5);

            return new CategoryStatistics
            {
                TotalActiveCategories = allCategories.Count,
                TotalProducts = topCategories.Sum(c => c.products?.Count ?? 0),
                TopCategoryByProducts = topCategories.FirstOrDefault()?.Name,
                AverageProductsPerCategory = allCategories.Count > 0
                    ? (double)topCategories.Sum(c => c.products?.Count ?? 0) / allCategories.Count
                    : 0
            };
        }

        /// <summary>
        /// Validates category data
        /// </summary>
        /// <param name="category">Category to validate</param>
        /// <returns>List of validation errors</returns>
        public async Task<List<string>> ValidateCategoryAsync(Category category)
        {
            var errors = new List<string>();

            if (category == null)
            {
                errors.Add("Category cannot be null");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(category.Name))
                errors.Add("Category name is required");
            else if (category.Name.Length > 100) // Assuming max length
                errors.Add("Category name cannot exceed 100 characters");

            if (string.IsNullOrWhiteSpace(category.Slug))
                errors.Add("Category slug is required");
            else if (category.Slug.Length > 100) // Assuming max length
                errors.Add("Category slug cannot exceed 100 characters");

            if (!string.IsNullOrWhiteSpace(category.Description) && category.Description.Length > 500)
                errors.Add("Category description cannot exceed 500 characters");

            // Check slug uniqueness for new categories or changed slugs
            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                var existingCategory = await _unitOfWork.CategoryRepository.GetBySlugAsync(category.Slug);
                if (existingCategory != null && existingCategory.Id != category.Id)
                    errors.Add($"A category with slug '{category.Slug}' already exists");
            }

            return errors;
        }

        /// <summary>
        /// Generates a unique slug from category name
        /// </summary>
        /// <param name="name">Category name</param>
        /// <returns>Unique slug</returns>
        public async Task<string> GenerateUniqueSlugAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            var baseSlug = GenerateSlugFromName(name);
            var slug = baseSlug;
            var counter = 1;

            while (await SlugExistsAsync(slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Helper method to generate slug from name
        /// </summary>
        /// <param name="name">Category name</param>
        /// <returns>Generated slug</returns>
        private static string GenerateSlugFromName(string name)
        {
            return name.ToLowerInvariant()
                      .Replace(" ", "-")
                      .Replace("ç", "c")
                      .Replace("ğ", "g")
                      .Replace("ı", "i")
                      .Replace("ö", "o")
                      .Replace("ş", "s")
                      .Replace("ü", "u")
                      .Replace("İ", "i")
                      .Replace("Ç", "c")
                      .Replace("Ğ", "g")
                      .Replace("Ö", "o")
                      .Replace("Ş", "s")
                      .Replace("Ü", "u")
                      .Trim('-');
        }

        public Task<bool> CanDeleteCategoryAsync(int categoryId)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<Category>> GetCategoriesPagedAsync(int pageNumber = 1, int pageSize = 10, string searchKeyword = null, bool includeInactive = false)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Category>> GetCategoryHierarchyAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetCategoryStatusAsync(int categoryId, bool isActive)
        {
            throw new NotImplementedException();
        }
    }
}

