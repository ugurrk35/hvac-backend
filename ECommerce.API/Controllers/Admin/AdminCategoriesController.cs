using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.CategoryDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Dtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.API.Helper;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Kategori yönetimi: oluşturma, güncelleme, silme ve listeleme işlemleri.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ECommerce.Repository.Data.ApplicationDbContext _db;
        private readonly StackExchange.Redis.IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;

        public AdminCategoriesController(ICategoryService categoryService, ECommerce.Repository.Data.ApplicationDbContext db, StackExchange.Redis.IConnectionMultiplexer redis, IConfiguration configuration)
        {
            _categoryService = categoryService;
            _db = db;
            _redis = redis;
            _configuration = configuration;
        }

        private async Task UpsertDomainRouteAsync(string slug, int entityId)
        {
            if (string.IsNullOrWhiteSpace(slug) || entityId <= 0) return;
            var existing = await _db.DomainRoutes.FirstOrDefaultAsync(r => r.Slug == slug);
            if (existing == null)
            {
                _db.DomainRoutes.Add(new ECommerce.Domain.Entity.DomainRoute
                {
                    Slug = slug,
                    EntityType = "category",
                    EntityId = entityId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.EntityType = "category";
                existing.EntityId = entityId;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            // invalidate resolve cache keys
            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/resolve-slug/{slug}"));
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/routes/resolve/{slug}"));
            }
            catch { }
        }

        private async Task DeactivateDomainRouteAsync(int categoryId)
        {
            var rows = await _db.DomainRoutes.Where(r => r.EntityType == "category" && r.EntityId == categoryId).ToListAsync();
            if (rows.Count > 0)
            {
                foreach (var r in rows)
                {
                    r.IsActive = false;
                    r.UpdatedAt = DateTime.UtcNow;
                    try
                    {
                        var db = _redis.GetDatabase();
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/resolve-slug/{r.Slug}"));
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/routes/resolve/{r.Slug}"));
                    }
                    catch { }
                }
                await _db.SaveChangesAsync();
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<DataResponse<List<CategoryDto>>>> GetAllActive()
        {
            var categories = await _categoryService.GetAllActiveAsync();
            var result = categories.Select(category => category.ToDto()).ToList();
            return Ok(DataResponse<List<CategoryDto>>.CreateSuccess(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataResponse<CategoryDto>>> GetById(int id)
        {
            var category = await _categoryService.GetByIdWithProductsAsync(id);
            if (category == null)
                return NotFound(DataResponse<CategoryDto>.CreateFailure("Category not found"));

            var result = category.ToDto();
            return Ok(DataResponse<CategoryDto>.CreateSuccess(result));
        }

        [HttpPost]
        public async Task<ActionResult<DataResponse<CategoryDto>>> Create(CategoryCreateDto dto)
        {
            var entity = dto.ToEntity();
            var validationErrors = await _categoryService.ValidateCategoryAsync(entity);

            if (validationErrors.Count > 0)
                return BadRequest(DataResponse<CategoryDto>.CreateFailure("Validation failed", validationErrors));

            var created = await _categoryService.CreateCategoryAsync(entity);
            var resultDto = created.ToDto();
            await UpsertDomainRouteAsync(resultDto.Slug, resultDto.Id);
            return Ok(DataResponse<CategoryDto>.CreateSuccess(resultDto, "Category created successfully"));
        }

        [HttpPut]
        public async Task<ActionResult<DataResponse<CategoryDto>>> Update(int id, CategoryUpdateDto dto)
        {
            var entity = dto.ToEntity();
            var validationErrors = await _categoryService.ValidateCategoryAsync(entity);

            if (validationErrors.Count > 0)
                return BadRequest(DataResponse<CategoryDto>.CreateFailure("Validation failed", validationErrors));

            var updated = await _categoryService.UpdateCategoryAsync(entity);
            var resultDto = updated.ToDto();
            await UpsertDomainRouteAsync(resultDto.Slug, resultDto.Id);
            return Ok(DataResponse<CategoryDto>.CreateSuccess(resultDto, "Category updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (!success)
                return NotFound(new BaseResponse { Success = false, Message = "Category not found or could not be deleted" });
            await DeactivateDomainRouteAsync(id);
            return Ok(new BaseResponse { Success = true, Message = "Category deleted successfully" });
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<DataResponse<CategoryDto>>> GetBySlug(string slug)
        {
            var category = await _categoryService.GetBySlugAsync(slug);
            if (category == null)
                return NotFound(DataResponse<CategoryDto>.CreateFailure("Category not found"));

            return Ok(DataResponse<CategoryDto>.CreateSuccess(category.ToDto()));
        }

        [HttpGet("search")]
        public async Task<ActionResult<DataResponse<List<CategoryDto>>>> Search([FromQuery] string keyword)
        {
            var result = await _categoryService.SearchAsync(keyword);
            return Ok(DataResponse<List<CategoryDto>>.CreateSuccess(result.Select(category => category.ToDto()).ToList()));
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<DataResponse<CategoryStatistics>>> GetStatistics()
        {
            var stats = await _categoryService.GetCategoryStatisticsAsync();
            return Ok(DataResponse<CategoryStatistics>.CreateSuccess(stats));
        }
    }
}
