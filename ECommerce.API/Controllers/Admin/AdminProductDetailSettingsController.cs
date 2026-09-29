using System.Text.Json;
using ECommerce.API.Models;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/product-detail-settings")]
[Authorize(Roles = "Admin")]
public class AdminProductDetailSettingsController(ApplicationDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductDetailConfigDto>> Get([FromQuery] int? categoryId = null)
    {
        var rows = await db.ProductDetailSettings.AsNoTracking()
            .Where(x => x.CategoryId == null || x.CategoryId == categoryId)
            .ToListAsync();
        var row = categoryId.HasValue
            ? rows.FirstOrDefault(x => x.CategoryId == categoryId) ?? rows.FirstOrDefault(x => x.CategoryId == null)
            : rows.FirstOrDefault(x => x.CategoryId == null);
        return Ok(ProductDetailSettingsController.Deserialize(row?.ConfigJson));
    }

    [HttpPut]
    public async Task<ActionResult<ProductDetailConfigDto>> Update([FromBody] ProductDetailConfigDto config, [FromQuery] int? categoryId = null)
    {
        config.TrustItems = config.TrustItems.Take(6).Select(Normalize).ToList();
        config.FeatureItems = config.FeatureItems.Take(6).Select(Normalize).ToList();
        var row = await db.ProductDetailSettings.FirstOrDefaultAsync(x => x.CategoryId == categoryId);
        if (row == null)
        {
            row = new ProductDetailSettings { CategoryId = categoryId };
            db.ProductDetailSettings.Add(row);
        }
        row.ConfigJson = JsonSerializer.Serialize(config);
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = currentUser.UserId;
        await db.SaveChangesAsync();
        return Ok(config);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCategoryOverride([FromQuery] int categoryId)
    {
        var row = await db.ProductDetailSettings.FirstOrDefaultAsync(x => x.CategoryId == categoryId);
        if (row != null)
        {
            db.ProductDetailSettings.Remove(row);
            await db.SaveChangesAsync();
        }
        return NoContent();
    }

    private static ProductDetailInfoItemDto Normalize(ProductDetailInfoItemDto item) => new()
    {
        Icon = (item.Icon ?? "heart").Trim(),
        Title = (item.Title ?? string.Empty).Trim(),
        Description = (item.Description ?? string.Empty).Trim()
    };
}
