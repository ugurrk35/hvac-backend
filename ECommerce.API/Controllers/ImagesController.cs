using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ImageDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Görsel yükleme ve erişim işlemlerini yönetir.
    /// Ürün/blog görselleri için uygun uç noktaları sağlar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        // Public upload for authenticated users (e.g., review photos)
        [Authorize]
        [EnableRateLimiting("upload")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [HttpPost("upload-public")]
        public async Task<IActionResult> UploadPublic([FromForm] ImageUploadDto dto)
        {
            if (dto?.File == null || dto.File.Length == 0)
                return BadRequest(BaseResponse.CreateFailure("Dosya gerekli"));

            var image = await _imageService.UploadImageAsync(dto.File, dto.Title ?? "review-photo", dto.AltText, dto.Caption);
            var result = image.ToDto();
            return Ok(DataResponse<ImageDto>.CreateSuccess(result));
        }
    }
}
