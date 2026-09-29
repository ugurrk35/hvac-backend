using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductAttributeController : BaseApiController
    {
        private readonly IProductAttributeService _productAttributeService;

        public ProductAttributeController(IProductAttributeService productAttributeService)
        {
            _productAttributeService = productAttributeService;
        }

        [HttpGet]
        public async Task<ActionResult<DataResponse<List<ProductAttributeDto>>>> GetAll()
        {
            var attributes = await _productAttributeService.GetAllAsync();
            var result = attributes.Select(attribute => attribute.ToDto()).ToList();
            return Ok(DataResponse<List<ProductAttributeDto>>.CreateSuccess(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataResponse<ProductAttributeDto>>> GetById(int id)
        {
            var attribute = await _productAttributeService.GetByIdAsync(id);
            if (attribute == null)
                return NotFound(DataResponse<ProductAttributeDto>.CreateFailure("Attribute not found"));

            var result = attribute.ToDto();
            return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result));
        }

        [HttpPost]
        public async Task<ActionResult<DataResponse<ProductAttributeDto>>> Create([FromBody] ProductAttributeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(DataResponse<ProductAttributeDto>.CreateFailure("Validation failed", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList()));

            var entity = dto.ToEntity();
            var created = await _productAttributeService.AddAsync(entity);
            var result = created.ToDto();
            return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result, "Attribute created successfully"));
        }

        [HttpPut]
        public async Task<ActionResult<DataResponse<ProductAttributeDto>>> Update([FromBody] ProductAttributeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(DataResponse<ProductAttributeDto>.CreateFailure("Validation failed", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList()));

            var entity = dto.ToEntity();
            await _productAttributeService.UpdateAsync(entity);
            var result = entity.ToDto();
            return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result, "Attribute updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            var entity = await _productAttributeService.GetByIdAsync(id);
            if (entity == null)
                return NotFound(BaseResponse.CreateFailure("Attribute not found"));

            await _productAttributeService.DeleteAsync(entity);
            return Ok(BaseResponse.CreateSuccess("Attribute deleted successfully"));
        }

        [HttpDelete("soft/{id}")]
        public async Task<ActionResult<BaseResponse>> SoftDelete(int id)
        {
            var entity = await _productAttributeService.GetByIdAsync(id);
            if (entity == null)
                return NotFound(BaseResponse.CreateFailure("Attribute not found"));

            await _productAttributeService.SoftDeleteAsync(entity);
            return Ok(BaseResponse.CreateSuccess("Attribute soft deleted successfully"));
        }

        [HttpGet("with-values")]
        public async Task<ActionResult<DataResponse<List<ProductAttributeDto>>>> GetAttributesWithValues()
        {
            var attributes = await _productAttributeService.GetAttributesWithValuesAsync();
            var result = attributes.Select(attribute => attribute.ToDto()).ToList();
            return Ok(DataResponse<List<ProductAttributeDto>>.CreateSuccess(result));
        }

        [HttpGet("with-values/{id}")]
        public async Task<ActionResult<DataResponse<ProductAttributeDto>>> GetAttributeWithValuesById(int id)
        {
            var attribute = await _productAttributeService.GetAttributeWithValuesByIdAsync(id);
            if (attribute == null)
                return NotFound(DataResponse<ProductAttributeDto>.CreateFailure("Attribute not found"));

            var result = attribute.ToDto();
            return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result));
        }

        [HttpGet("personalization")]
        public async Task<ActionResult<DataResponse<List<ProductAttributeDto>>>> GetPersonalizationAttributes()
        {
            var attributes = await _productAttributeService.GetPersonalizationAttributesAsync();
            var result = attributes.Select(attribute => attribute.ToDto()).ToList();
            return Ok(DataResponse<List<ProductAttributeDto>>.CreateSuccess(result));
        }
    }
}
