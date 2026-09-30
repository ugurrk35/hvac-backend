using System.Text.Json;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
public class HomeHeroController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    public HomeHeroController(IWebHostEnvironment environment) => _environment = environment;
    public class HeroSettings { public string? HeroEyebrow { get; set; } public string? HeroTitle { get; set; } public string? HeroSummary { get; set; } public string? PrimaryCtaLabel { get; set; } public string? PrimaryCtaHref { get; set; } public string? SecondaryCtaLabel { get; set; } public string? SecondaryCtaHref { get; set; } public string? HeroImageUrl { get; set; } public string? HeroImageAlt { get; set; } }
    private string PathToSettings() => Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "home-hero.json");
    private async Task<HeroSettings> Read() { try { var path = PathToSettings(); if (System.IO.File.Exists(path)) return JsonSerializer.Deserialize<HeroSettings>(await System.IO.File.ReadAllTextAsync(path)) ?? new(); } catch { } return new(); }

    [HttpGet("api/home/hero")]
    [AllowAnonymous]
    public async Task<ActionResult> Get() => Ok(DataResponse<HeroSettings>.CreateSuccess(await Read()));

    [HttpGet("api/admin/home/settings/hero")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetAdmin() => Ok(await Read());

    [HttpPost("api/admin/home/settings/hero")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Save([FromBody] HeroSettings settings)
    {
        var path = PathToSettings(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await System.IO.File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        return Ok(settings);
    }
}
