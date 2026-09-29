using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ImageDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] ImageUploadDto dto)
        {
            try
            {
                var image = await _imageService.UploadImageAsync(dto.File, dto.Title, dto.AltText, dto.Caption);
                var result = image.ToDto();
                return Ok(DataResponse<ImageDto>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                return BadRequest(BaseResponse.CreateFailure(ex.Message));
            }
        }

        [HttpGet("by-extension")]
        public async Task<IActionResult> GetByExtension([FromQuery] string extension)
        {
            var images = await _imageService.GetImagesByExtensionAsync(extension);
            var result = images.Select(image => image.ToDto()).ToList();
            return Ok(DataResponse<List<ImageDto>>.CreateSuccess(result));
        }

        [HttpGet("by-size")]
        public async Task<IActionResult> GetBySize([FromQuery] int minSize, [FromQuery] int maxSize)
        {
            var images = await _imageService.GetImagesBySizeRangeAsync(minSize, maxSize);
            var result = images.Select(image => image.ToDto()).ToList();
            return Ok(DataResponse<List<ImageDto>>.CreateSuccess(result));
        }

        [HttpGet("by-url")]
        public async Task<IActionResult> GetByUrl([FromQuery] string url)
        {
            var image = await _imageService.GetImageByUrlAsync(url);
            if (image == null) return NotFound(BaseResponse.CreateFailure("Image not found"));
            var result = image.ToDto();
            return Ok(DataResponse<ImageDto>.CreateSuccess(result));
        }

        [HttpDelete("unused")]
        public async Task<IActionResult> DeleteUnused()
        {
            var result = await _imageService.DeleteUnusedImagesAsync();
            if (result)
                return Ok(BaseResponse.CreateSuccess("Unused images deleted."));
            else
                return BadRequest(BaseResponse.CreateFailure("Error deleting unused images."));
        }

        [HttpPost("{id}/optimize")]
        public async Task<IActionResult> Optimize(int id)
        {
            var success = await _imageService.OptimizeImageAsync(id);
            return success
                ? Ok(BaseResponse.CreateSuccess("Image optimized."))
                : NotFound(BaseResponse.CreateFailure("Image not found or optimization failed."));
        }

        [HttpPost("{id}/resize")]
        public async Task<IActionResult> Resize(int id, [FromQuery] int width, [FromQuery] int height)
        {
            var image = await _imageService.ResizeImageAsync(id, width, height);
            if (image == null)
                return NotFound(BaseResponse.CreateFailure("Image not found or resize failed."));
            var result = image.ToDto();
            return Ok(DataResponse<ImageDto>.CreateSuccess(result));
        }

        /// <summary>
        /// Delete image by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteImage(int id)
        {
            try
            {
                var image = await _imageService.GetByIdAsync(id);
                if (image == null)
                    return NotFound(BaseResponse.CreateFailure("Görsel bulunamadı"));
                await _imageService.DeleteAsync(image);
                return Ok(BaseResponse.CreateSuccess("Görsel başarıyla silindi"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse.CreateFailure($"Silme sırasında hata: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update image details
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateImage(int id, [FromBody] UpdateImageDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(BaseResponse.CreateFailure("ID uyuşmuyor"));
                var updated = await _imageService.UpdateImageAsync(dto);
                return Ok(DataResponse<ImageDto>.CreateSuccess(updated.ToDto(), "Görsel güncellendi"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse.CreateFailure($"Güncelleme sırasında hata: {ex.Message}"));
            }
        }

        /// <summary>
        /// Search images by term
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult> SearchImages([FromQuery] string searchTerm)
        {
            try
            {
                var results = await _imageService.SearchImagesAsync(searchTerm);
                var dtos = results.Select(image => image.ToDto()).ToList();
                return Ok(DataResponse<List<ImageDto>>.CreateSuccess(dtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse.CreateFailure($"Arama sırasında hata: {ex.Message}"));
            }
        }
    }
}
