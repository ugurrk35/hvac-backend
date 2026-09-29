using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.API.Helper;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// SEO dostu URL çözümleme (slug -> varlık) uç noktalarını sağlar.
    /// DomainRoutes tablosu üzerinden veya doğrudan Ürün/Kategori tablolarından çözümleme yapar.
    /// </summary>
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RoutesController> _logger;

        public RoutesController(ApplicationDbContext db, ILogger<RoutesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("api/routes/resolve/{slug}")]
        [HttpGet("api/resolve-slug/{slug}")] // alias as requested
        [CacheResponse(60)] // Redis cache 60s via attribute
        [AllowAnonymous]
        public async Task<IActionResult> Resolve(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(new { sCategory = false, sProduct = false, message = "Slug gerekli" });
            try
            {
                var sourcePath = "/" + slug.Trim('/');
                var redirect = await _db.RedirectRules.AsNoTracking().FirstOrDefaultAsync(x => x.SourcePath == sourcePath && x.IsActive && !x.IsDeleted);
                if (redirect != null) return Ok(new { sCategory = false, sProduct = false, redirectTo = redirect.TargetPath, redirectStatus = redirect.StatusCode });
                // 1) DomainRoutes tablosu (opsiyonel). Tablo yoksa sessizce fallback'a geç.
                try
                {
                    var route = await _db.DomainRoutes.AsNoTracking().FirstOrDefaultAsync(r => r.Slug == slug && r.IsActive);
                    if (route != null)
                    {
                        if (string.Equals(route.EntityType, "product", StringComparison.OrdinalIgnoreCase))
                        {
                            var p = await _db.Products.AsNoTracking()
                                .Include(x => x.ProductImages).ThenInclude(pi => pi.Image)
                                .Where(x => x.Id == route.EntityId && x.IsPublished && !x.IsDeleted)
                                .Select(x => new
                                {
                                    id = x.Id,
                                    name = x.Name,
                                    slug = x.Slug,
                                    effectivePrice = (decimal?)(x.DiscountPrice ?? x.BasePrice),
                                    mainImageUrl = x.ProductImages.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.Url).FirstOrDefault()
                                })
                                .FirstOrDefaultAsync();
                            if (p != null)
                                return Ok(new { sCategory = false, sProduct = true, data = p });
                        }
                        else if (string.Equals(route.EntityType, "category", StringComparison.OrdinalIgnoreCase))
                        {
                            var c = await _db.Categories.AsNoTracking()
                                .Where(x => x.Id == route.EntityId && x.IsActive && !x.IsDeleted)
                                .Select(x => new { id = x.Id, name = x.Name, slug = x.Slug, description = x.Description })
                                .FirstOrDefaultAsync();
                            if (c != null)
                                return Ok(new { sCategory = true, sProduct = false, data = c });
                        }
                    }
                }
                catch { /* DomainRoutes tablosu yoksa fallback'a geç */ }

                // 2) Fallback - Products / Categories tablolarından kontrol
                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished && !p.IsDeleted);
                if (product != null)
                {
                    var p = await _db.Products.AsNoTracking()
                        .Include(x => x.ProductImages).ThenInclude(pi => pi.Image)
                        .Where(x => x.Id == product.Id)
                        .Select(x => new
                        {
                            id = x.Id,
                            name = x.Name,
                            slug = x.Slug,
                            effectivePrice = (decimal?)(x.DiscountPrice ?? x.BasePrice),
                            mainImageUrl = x.ProductImages.OrderBy(pi => pi.SortOrder).Select(pi => pi.Image.Url).FirstOrDefault()
                        })
                        .FirstAsync();
                    return Ok(new { sCategory = false, sProduct = true, data = p });
                }
                var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive && !c.IsDeleted);
                if (category != null)
                {
                    var c = new { id = category.Id, name = category.Name, slug = category.Slug, description = category.Description };
                    return Ok(new { sCategory = true, sProduct = false, data = c });
                }

                return NotFound(new { sCategory = false, sProduct = false, message = "Bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slug resolve error for {Slug}", slug);
                return StatusCode(500, new { sCategory = false, sProduct = false, message = "Çözümlenemedi" });
            }
        }
    }
}
