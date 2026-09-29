using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductPriceController : ControllerBase
    {
        private readonly IProductPriceService _service;
        public ProductPriceController(IProductPriceService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductPriceDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductPriceDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("paged")]
        public async Task<IActionResult> GetPaged([FromBody] ProductPriceFilterDto filter, int page = 1, int pageSize = 20)
        {
            var (items, total) = await _service.GetPagedAsync(page, pageSize, filter);
            return Ok(new { items, total });
        }
        /// <summary>
        /// Toplu ürün fiyat ekleme/güncelleme
        /// </summary>
        [HttpPost("bulk-upsert")]
        public async Task<IActionResult> BulkUpsert([FromBody] BulkProductPriceDto dto)
        {
            if (dto == null)
                return BadRequest("DTO cannot be null");

            try
            {
                await _service.BulkUpsertProductPricesAsync(dto);
                return Ok(new { Message = "Product prices updated successfully." });
            }
            catch (Exception ex)
            {
                // Ýsteðe baðlý: detaylý logging ekleyebilirsin
                return StatusCode(500, new { Message = "An error occurred while updating product prices.", Details = ex.Message });
            }
        }
    }
}
