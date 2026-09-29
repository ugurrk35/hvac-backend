using ECommerce.Domain.Dtos;
using ECommerce.Service.Abstract;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.CategoryDtos;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// İstemciler için hafif lookup/veri sözlüğü uç noktalarını sağlar (kategori, tag vb. kısa listeler).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LookupController : ControllerBase
    {
        private readonly ILookupService _lookupService;

        public LookupController(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        /// <summary>
        /// Kategorilerig getirir
        /// <returns>List of categories</returns>
        [HttpGet("lookup-categories")]
        public async Task<ActionResult<DataResponse<List<LookupDto>>>> GetCategories()
        {
            var categories = await _lookupService.GetCategoriesAsync();
            return Ok(DataResponse<List<LookupDto>>.CreateSuccess(categories));
         
        }
        [HttpGet("lookup-blogcategories")]
        public async Task<ActionResult<DataResponse<List<LookupDto>>>> GetBlogCategories()
        {
            var categories = await _lookupService.BlogCategory();
            return Ok(DataResponse<List<LookupDto>>.CreateSuccess(categories));

        }
        /// <summary>
        /// ürün özelliklerini getirir
        /// <returns>List of product attributes</returns>
        [HttpGet("lookup-product-attributes")]
        public async Task<ActionResult<DataResponse<List<LookupPersonalizationDto>>>> GetProductAttributes()
        {
            var attributes = await _lookupService.GetProductAttributesAsync();
            return Ok(DataResponse<List<LookupPersonalizationDto>>.CreateSuccess(attributes));
        }

        /// <summary>
        /// ürün özellik değerlerini getirir
        [HttpGet("lookup-product-attribute-values/{productAttributeId}")]
        public async Task<ActionResult<DataResponse<List<LookupDto>>>> GetProductAttributeValues(int productAttributeId)
        {
            var values = await _lookupService.GetProductAttributeValuesByAttributeIdAsync(productAttributeId);
            return Ok(DataResponse<List<LookupDto>>.CreateSuccess(values));
        }
        /// <summary>
        /// Arama terimine göre aktif etiketleri (tag) getirir (min 3 harf).
        /// GET /api/lookup/tags/search?term=ren gibi bir istekle çalışır.
        /// </summary>
        /// <param name="term">Aranacak terim</param>
        [HttpGet("tags/search")]
        public async Task<ActionResult<DataResponse<List<LookupDto>>>> SearchTags([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
            {
                return BadRequest(DataResponse<List<LookupDto>>.CreateFailure("Arama terimi en az 3 karakter olmalıdır."));
            }

            var result = await _lookupService.SearchTagsAsync(term);
            return Ok(DataResponse<List<LookupDto>>.CreateSuccess(result));
        }
    }
}
