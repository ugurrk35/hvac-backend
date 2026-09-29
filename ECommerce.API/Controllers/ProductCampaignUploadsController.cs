using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/product-campaigns/uploads")]
public class ProductCampaignUploadsController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(BaseResponse.CreateFailure("Fotoğraf seçmelisiniz."));
        if (file.Length > 5 * 1024 * 1024 || !AllowedTypes.Contains(file.ContentType)) return BadRequest(BaseResponse.CreateFailure("Yalnızca JPG, PNG veya WebP ve en fazla 5 MB dosya yükleyebilirsiniz."));
        var extension = file.ContentType.ToLowerInvariant() switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".webp" };
        var root = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "campaigns");
        Directory.CreateDirectory(root);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(root, name));
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(DataResponse<object>.CreateSuccess(new { url = $"/uploads/campaigns/{name}" }));
    }
}
