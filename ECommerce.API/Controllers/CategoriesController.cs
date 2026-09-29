using ECommerce.Domain.Entity;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.CategoryDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Kategoriler için herkese açık uç noktaları sağlar.
    /// Aktif kategorileri listeler, kimliğe veya slug'a göre detay getirir ve yönetim işlemlerini sunar.
    /// </summary>
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("active")]
        public async Task<ActionResult<DataResponse<List<CategoryDto>>>> GetAllActive()
        {
            var categories = await _categoryService.GetAllActiveAsync();
            var result = categories.Select(category => category.ToDto()).ToList();
            return Ok(DataResponse<List<CategoryDto>>.CreateSuccess(result));
        }

        [HttpGet("slug/{slug}")]
        [Obsolete("Use GET /api/resolve-slug/{slug} instead")] 
        public async Task<ActionResult<DataResponse<CategoryDto>>> GetBySlug(string slug)
        {
            var category = await _categoryService.GetBySlugAsync(slug);
            if (category == null)
                return NotFound(DataResponse<CategoryDto>.CreateFailure("Kategori bulunamadı."));

            var result = category.ToDto();
            return Ok(DataResponse<CategoryDto>.CreateSuccess(result));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DataResponse<CategoryDto>>> GetById(int id)
        {
            var category = await _categoryService.GetByIdWithProductsAsync(id);
            if (category == null)
                return NotFound(DataResponse<CategoryDto>.CreateFailure("Kategori bulunamadı."));

            var result = category.ToDto();
            return Ok(DataResponse<CategoryDto>.CreateSuccess(result));
        }


        [HttpPost]
        public async Task<ActionResult<DataResponse<CategoryDto>>> Create([FromBody] CategoryCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                return BadRequest(DataResponse<CategoryDto>.CreateFailure("Validation failed", modelErrors));
            }

            var entity = dto.ToEntity();
            var validationErrors = await _categoryService.ValidateCategoryAsync(entity);

            if (validationErrors.Count > 0)
                return BadRequest(DataResponse<CategoryDto>.CreateFailure("Validation failed", validationErrors));

            var created = await _categoryService.CreateCategoryAsync(entity);
            var result = created.ToDto();
            return Ok(DataResponse<CategoryDto>.CreateSuccess(result, "Category created successfully"));
        }

        //[HttpGet("search")]
        //public async Task<ActionResult<DataResponse<List<CategoryDto>>>> Search([FromQuery] string keyword)
        //{
        //    var result = await _categoryService.SearchAsync(keyword);
        //    return Ok(DataResponse<List<CategoryDto>>.CreateSuccess(_mapper.Map<List<CategoryDto>>(result)));
        //}

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            var deleted = await _categoryService.DeleteCategoryAsync(id);
            if (!deleted)
            {
                return NotFound(BaseResponse.CreateFailure("Category could not be deleted"));
            }

            return Ok(BaseResponse.CreateSuccess("Category deleted successfully"));
        }

        //[HttpGet("statistics")]
        //public async Task<ActionResult<DataResponse<CategoryStatistics>>> GetStatistics()
        //{
        //    var stats = await _categoryService.GetCategoryStatisticsAsync();
        //    return Ok(DataResponse<CategoryStatistics>.CreateSuccess(stats));
        //}
    }
}
