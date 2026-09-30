using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/product-documents")]
[Authorize(Roles = "Admin")]
public class AdminProductDocumentsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf", ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp"
    };

    public AdminProductDocumentsController(IWebHostEnvironment environment) => _environment = environment;

    [HttpPost("upload")]
    [RequestSizeLimit(MaximumFileSize)]
    public async Task<ActionResult> Upload([FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest(DataResponse<object>.CreateFailure("Yüklenecek dosya bulunamadı."));
        if (file.Length > MaximumFileSize) return BadRequest(DataResponse<object>.CreateFailure("Dosya en fazla 10 MB olabilir."));
        if (!Extensions.TryGetValue(file.ContentType, out var extension)) return BadRequest(DataResponse<object>.CreateFailure("Yalnızca PDF, JPG, PNG veya WEBP yükleyebilirsiniz."));

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, "uploads", "product-documents");
        Directory.CreateDirectory(directory);
        var savedName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(directory, savedName))) await file.CopyToAsync(stream);

        return Ok(DataResponse<object>.CreateSuccess(new {
            url = $"/uploads/product-documents/{savedName}", fileName = Path.GetFileName(file.FileName),
            documentType = file.ContentType == "application/pdf" ? "PDF" : "Görsel"
        }));
    }
}
