using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductAttributeCombinationDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Ürün varyasyonları (attribute kombinasyonları) için yönetim uç noktaları.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminProductAttributeCombinationController : ControllerBase
    {
        private readonly IProductAttributeCombinationService _service;

        public AdminProductAttributeCombinationController(IProductAttributeCombinationService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> GetById(int id)
        {
            var entity = await _service.GetCombinationWithValuesAsync(id);
            if (entity == null)
                return NotFound(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Combination not found"));

            var dto = entity.ToDto();
            return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(dto));
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<DataResponse<List<ProductAttributeCombinationDto>>>> GetByProductId(int productId)
        {
            var entities = await _service.GetCombinationsByProductIdAsync(productId);
            var dtos = entities.Select(entity => entity.ToDto()).ToList();
            return Ok(DataResponse<List<ProductAttributeCombinationDto>>.CreateSuccess(dtos));
        }

        [HttpPost]
        public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> Create(CreateProductAttributeCombinationDto dto)
        {
            var entity = dto.ToEntity();
            var validationErrors = await _service.ValidateCombinationAsync(entity);

            if (validationErrors.Count > 0)
                return BadRequest(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Validation failed", validationErrors));

            var created = await _service.AddAsync(entity);
            var resultDto = created.ToDto();

            return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(resultDto, "Combination created successfully"));
        }

        [HttpPut]
        public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> Update(UpdateProductAttributeCombinationDto dto)
        {
            var entity = dto.ToEntity();
            var validationErrors = await _service.ValidateCombinationAsync(entity);

            if (validationErrors.Count > 0)
                return BadRequest(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Validation failed", validationErrors));

            await _service.UpdateAsync(entity);
            var updated = await _service.GetCombinationWithValuesAsync(entity.Id);
            var resultDto = updated.ToDto();

            return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(resultDto, "Combination updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null)
                return NotFound(BaseResponse.CreateFailure("Combination not found"));

            await _service.SoftDeleteAsync(entity);
            return Ok(BaseResponse.CreateSuccess("Combination deleted successfully"));
        }

        [HttpGet("is-sku-unique")]
        public async Task<ActionResult<DataResponse<bool>>> IsSkuUnique([FromQuery] string sku, [FromQuery] int? id = null)
        {
            var isUnique = await _service.IsSkuUniqueAsync(sku, id);
            return Ok(DataResponse<bool>.CreateSuccess(isUnique));
        }
    }
}
