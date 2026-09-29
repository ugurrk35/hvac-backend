using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.ProductTagDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTagController : BaseApiController
    {
        private readonly IProductTagService _productTagService;

        public ProductTagController(IProductTagService productTagService)
        {
            _productTagService = productTagService;
        }

       
        [HttpGet("{id}")]
        public async Task<ActionResult<DataResponse<ProductTagDto>>> GetById(int id)
        {
            var tag = await _productTagService.GetTagWithProductsByIdAsync(id);
            if (tag == null)
                return NotFound(DataResponse<ProductTagDto>.CreateFailure("Tag not found"));

            var result = tag.ToDto();
            return Ok(DataResponse<ProductTagDto>.CreateSuccess(result));
        }

        [HttpPost]
        public async Task<ActionResult<DataResponse<ProductTagDto>>> Create(ProductTagCreateDto dto)
        {
            var entity = dto.ToEntity();
            var created = await _productTagService.AddAsync(entity);
            return Ok(DataResponse<ProductTagDto>.CreateSuccess(created.ToDto(), "Tag created successfully"));
        }

        [HttpPut]
        public async Task<ActionResult<DataResponse<ProductTagDto>>> Update(ProductTagUpdateDto dto)
        {
            var entity = dto.ToEntity();
            await _productTagService.UpdateAsync(entity);
            return Ok(DataResponse<ProductTagDto>.CreateSuccess(entity.ToDto(), "Tag updated successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponse>> Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(id));

            // Check if category has products - optional business rule
            var productTag = await _productTagService.GetByIdAsync(id);
            var success =  _productTagService.DeleteAsync(productTag);

            return Ok(new BaseResponse { Success = true, Message = "Tag deleted successfully" });
        }

        //[HttpGet("search")]
        //public async Task<ActionResult<DataResponse<List<ProductTagDto>>>> Search([FromQuery] string keyword)
        //{
        //    var result = await _productTagService.SearchAsync(keyword);
        //    return Ok(DataResponse<List<ProductTagDto>>.CreateSuccess(_mapper.Map<List<ProductTagDto>>(result)));
        //}
    }
}
