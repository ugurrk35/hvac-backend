using System.Text.Json;
using ECommerce.API.Models;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/product-detail-settings")]
public class ProductDetailSettingsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<DataResponse<ProductDetailConfigDto>>> Get([FromQuery] int? categoryId = null)
    {
        var rows = await db.ProductDetailSettings.AsNoTracking()
            .Where(x => x.CategoryId == null || x.CategoryId == categoryId)
            .ToListAsync();
        var row = categoryId.HasValue
            ? rows.FirstOrDefault(x => x.CategoryId == categoryId) ?? rows.FirstOrDefault(x => x.CategoryId == null)
            : rows.FirstOrDefault(x => x.CategoryId == null);
        var config = Deserialize(row?.ConfigJson);
        return Ok(DataResponse<ProductDetailConfigDto>.CreateSuccess(config));
    }

    internal static ProductDetailConfigDto Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return ProductDetailConfigDto.CreateDefault();
        try { return JsonSerializer.Deserialize<ProductDetailConfigDto>(json) ?? ProductDetailConfigDto.CreateDefault(); }
        catch (JsonException) { return ProductDetailConfigDto.CreateDefault(); }
    }
}
