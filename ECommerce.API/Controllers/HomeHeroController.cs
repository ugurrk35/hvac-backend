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
    private static HeroSettings Default() => new() { HeroEyebrow = "Evinizin iklimi, uzman kontrolünde", HeroTitle = "Her mevsim doğru konfor.", HeroSummary = "Isı pompası, klima, kombi ve kazan çözümlerini; ihtiyacınıza uygun kapasite, şeffaf kurulum ve güçlü satış sonrası destekle seçin.", PrimaryCtaLabel = "Ürünleri incele", PrimaryCtaHref = "/kategori/isi-pompasi", SecondaryCtaLabel = "Ücretsiz keşif planla", SecondaryCtaHref = "/kesif", HeroImageUrl = "/uploads/home-hero-default.png", HeroImageAlt = "Kombi Klima Burada ana sayfa kampanyası" };
    private async Task<HeroSettings> Read() { var value = Default(); try { var path = PathToSettings(); if (!System.IO.File.Exists(path)) return value; var saved = JsonSerializer.Deserialize<HeroSettings>(await System.IO.File.ReadAllTextAsync(path)); if (saved == null) return value; value.HeroEyebrow = string.IsNullOrWhiteSpace(saved.HeroEyebrow) ? value.HeroEyebrow : saved.HeroEyebrow; value.HeroTitle = string.IsNullOrWhiteSpace(saved.HeroTitle) ? value.HeroTitle : saved.HeroTitle; value.HeroSummary = string.IsNullOrWhiteSpace(saved.HeroSummary) ? value.HeroSummary : saved.HeroSummary; value.PrimaryCtaLabel = string.IsNullOrWhiteSpace(saved.PrimaryCtaLabel) ? value.PrimaryCtaLabel : saved.PrimaryCtaLabel; value.PrimaryCtaHref = string.IsNullOrWhiteSpace(saved.PrimaryCtaHref) ? value.PrimaryCtaHref : saved.PrimaryCtaHref; value.SecondaryCtaLabel = string.IsNullOrWhiteSpace(saved.SecondaryCtaLabel) ? value.SecondaryCtaLabel : saved.SecondaryCtaLabel; value.SecondaryCtaHref = string.IsNullOrWhiteSpace(saved.SecondaryCtaHref) ? value.SecondaryCtaHref : saved.SecondaryCtaHref; value.HeroImageUrl = string.IsNullOrWhiteSpace(saved.HeroImageUrl) ? value.HeroImageUrl : saved.HeroImageUrl; value.HeroImageAlt = string.IsNullOrWhiteSpace(saved.HeroImageAlt) ? value.HeroImageAlt : saved.HeroImageAlt; } catch { } return value; }

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
