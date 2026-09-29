using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductAttributeCombinationDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductAttributeCombinationController : BaseApiController
    {
        //private readonly IProductAttributeCombinationService _service;
        //private readonly IMapper _mapper;

        //public ProductAttributeCombinationController(IProductAttributeCombinationService service, IMapper mapper)
        //{
        //    _service = service;
        //    _mapper = mapper;
        //}

        //[HttpGet("{id}")]
        //public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> GetById(int id)
        //{
        //    var entity = await _service.GetCombinationWithValuesAsync(id);
        //    if (entity == null)
        //        return NotFound(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Combination not found"));

        //    var dto = _mapper.Map<ProductAttributeCombinationDto>(entity);
        //    return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(dto));
        //}

        //[HttpGet("product/{productId}")]
        //public async Task<ActionResult<DataResponse<List<ProductAttributeCombinationDto>>>> GetByProductId(int productId)
        //{
        //    var entities = await _service.GetCombinationsByProductIdAsync(productId);
        //    var dtos = _mapper.Map<List<ProductAttributeCombinationDto>>(entities);
        //    return Ok(DataResponse<List<ProductAttributeCombinationDto>>.CreateSuccess(dtos));
        //}

        //[HttpPost]
        //public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> Create(CreateProductAttributeCombinationDto dto)
        //{
        //    var entity = _mapper.Map<ProductAttributeCombination>(dto);
        //    var validationErrors = await _service.ValidateCombinationAsync(entity);

        //    if (validationErrors.Count > 0)
        //        return BadRequest(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Validation failed", validationErrors));

        //    var created = await _service.AddAsync(entity);
        //    var resultDto = _mapper.Map<ProductAttributeCombinationDto>(created);

        //    return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(resultDto, "Combination created successfully"));
        //}

        //[HttpPut]
        //public async Task<ActionResult<DataResponse<ProductAttributeCombinationDto>>> Update(UpdateProductAttributeCombinationDto dto)
        //{
        //    var entity = _mapper.Map<ProductAttributeCombination>(dto);
        //    var validationErrors = await _service.ValidateCombinationAsync(entity);

        //    if (validationErrors.Count > 0)
        //        return BadRequest(DataResponse<ProductAttributeCombinationDto>.CreateFailure("Validation failed", validationErrors));

        //    await _service.UpdateAsync(entity);
        //    var updated = await _service.GetCombinationWithValuesAsync(entity.Id);
        //    var resultDto = _mapper.Map<ProductAttributeCombinationDto>(updated);

        //    return Ok(DataResponse<ProductAttributeCombinationDto>.CreateSuccess(resultDto, "Combination updated successfully"));
        //}

        //[HttpDelete("{id}")]
        //public async Task<ActionResult<BaseResponse>> Delete(int id)
        //{
        //    var entity = await _service.GetByIdAsync(id);
        //    if (entity == null)
        //        return NotFound(BaseResponse.CreateFailure("Combination not found"));

        //    await _service.SoftDeleteAsync(entity);
        //    return Ok(BaseResponse.CreateSuccess("Combination deleted successfully"));
        //}

        //[HttpGet("is-sku-unique")]
        //public async Task<ActionResult<DataResponse<bool>>> IsSkuUnique([FromQuery] string sku, [FromQuery] int? id = null)
        //{
        //    var isUnique = await _service.IsSkuUniqueAsync(sku, id);
        //    return Ok(DataResponse<bool>.CreateSuccess(isUnique));
        //}
    }
}
