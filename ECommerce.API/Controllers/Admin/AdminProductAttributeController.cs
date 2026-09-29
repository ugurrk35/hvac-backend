using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Ürün nitelikleri (attribute) ve değerleri için yönetim uç noktaları.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminProductAttributeController : ControllerBase
    {
        private readonly IProductAttributeService _productAttributeService;

        public AdminProductAttributeController(IProductAttributeService productAttributeService)
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
        public async Task<ActionResult<DataResponse<ProductAttributeDto>>> Create(ProductAttributeCreateDto dto)
        {
            var entity = dto.ToEntity();
            var now = DateTime.UtcNow;
            var auditUser = User.FindFirst("sub")?.Value ?? User.Identity?.Name ?? "system";

            entity.CreatedAt = now;
            entity.CreatedBy = auditUser;
            entity.IsActive = true;
            entity.LastModifiedAt = now;
            entity.LastModifiedBy = auditUser;

            foreach (var value in entity.ProductAttributeValues)
            {
                value.CreatedAt = now;
                value.CreatedBy = auditUser;
                value.IsActive = true;
                value.LastModifiedAt = now;
                value.LastModifiedBy = auditUser;
            }

            var created = await _productAttributeService.AddAsync(entity);
            var result = created.ToDto();
            return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result, "Attribute created successfully"));
        }

        //[HttpPut]
        //public async Task<ActionResult<DataResponse<ProductAttributeDto>>> Update(ProductAttributeUpdateDto dto)
        //{
        //    var entity = _mapper.Map<ProductAttribute>(dto);
        //    await _productAttributeService.UpdateAsync(entity);
        //    var result = _mapper.Map<ProductAttributeDto>(entity);
        //    return Ok(DataResponse<ProductAttributeDto>.CreateSuccess(result, "Attribute updated successfully"));
        //}
        [HttpPut]
        public async Task<ActionResult<DataResponse<ProductAttributeUpdateDto>>> Update(int id, [FromBody] ProductAttributeUpdateDto dto)
        {
            // 1. Mevcut entity DB'den çek
            var entity = await _productAttributeService.GetAttributeWithValuesByIdAsync(id);
            if (entity == null)
                return NotFound();

            // 2. DTO'daki değişiklikleri mevcut entity üzerine uygula
            dto.ApplyTo(entity);

            // 3. Audit alanlarını set et
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = User.FindFirst("sub")?.Value ?? "system";

            // 4. Child collection (ProductAttributeValues) için audit set et
            foreach (var value in entity.ProductAttributeValues)
            {
                if (value.Id == 0) // yeni eklenen
                {
                    value.CreatedBy = User.FindFirst("sub")?.Value ?? "system";
                    value.CreatedAt = DateTime.UtcNow;
                    value.LastModifiedAt = DateTime.UtcNow;
                    value.LastModifiedBy = User.FindFirst("sub")?.Value ?? "system";
                }
                else
                {
                    if (value.CreatedBy==null)
                    {
                        value.CreatedBy = User.FindFirst("sub")?.Value ?? "system";
                    }
                    value.LastModifiedAt = DateTime.UtcNow;
                    value.LastModifiedBy = User.FindFirst("sub")?.Value ?? "system";
                }
            }

            await _productAttributeService.UpdateAsync(entity);

            var result = new ProductAttributeUpdateDto { Id = entity.Id, Name = entity.Name, IsPersonalizationText = entity.IsPersonalizationText, TextPrompt = entity.TextPrompt, MaxLength = entity.MaxLength, ProductAttributeValues = entity.ProductAttributeValues.Select(value => new ProductAttributeValueUpdateDto { Id = value.Id, Value = value.Value, PriceModifier = value.PriceModifier }).ToList() };
            return Ok(DataResponse<ProductAttributeUpdateDto>.CreateSuccess(result, "Attribute updated successfully"));
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            var entity = await _productAttributeService.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new BaseResponse { Success = false, Message = "Attribute not found" });

            await _productAttributeService.DeleteAsync(entity);
            return Ok(new BaseResponse { Success = true, Message = "Attribute deleted successfully" });
        }

        [HttpDelete("soft/{id}")]
        public async Task<ActionResult<BaseResponse>> SoftDelete(int id)
        {
            var entity = await _productAttributeService.GetByIdAsync(id);
            if (entity == null)
                return NotFound(new BaseResponse { Success = false, Message = "Attribute not found" });

            await _productAttributeService.SoftDeleteAsync(entity);
            return Ok(new BaseResponse { Success = true, Message = "Attribute soft deleted successfully" });
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
