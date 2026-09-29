using System.Text.Json;
using ECommerce.Domain.Entity;
using ECommerce.API.Models;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Alt menü (footer) içeriklerini okur ve yönetir; cache kullanarak hızlı yanıt verir.
    /// </summary>
    [ApiController]
    public class FooterController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FooterController> _logger;
        private readonly IDistributedCache _cache;

        public FooterController(ApplicationDbContext db, ILogger<FooterController> logger, IDistributedCache cache)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
        }

        // DTOs moved to ECommerce.API.Models to avoid Swagger schema name collisions

        [HttpGet("api/footer")]
        [AllowAnonymous]
        public async Task<ActionResult<DataResponse<FooterConfigDto>>> Get()
        {
            try
            {
                var cacheKey = "footer:config";
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var cachedObj = System.Text.Json.JsonSerializer.Deserialize<DataResponse<FooterConfigDto>>(cached);
                    if (cachedObj != null) return Ok(cachedObj);
                }
                var s = await _db.FooterSettings.AsNoTracking().FirstOrDefaultAsync();
                if (s == null || string.IsNullOrWhiteSpace(s.ConfigJson))
                {
                    // fallback default minimal columns
                    var fallback = new FooterConfigDto
                    {
                        columns = new List<FooterColumnDto>
                        {
                            new FooterColumnDto
                            {
                                title = "Alışveriş",
                                sort = 0,
                                links = new List<FooterLinkDto>
                                {
                                    new FooterLinkDto{ label = "Tüm Ürünler", url = "/products", isExternal = false, sort = 0 },
                                    new FooterLinkDto{ label = "Kategoriler", url = "/categories", isExternal = false, sort = 1 },
                                    new FooterLinkDto{ label = "Blog", url = "/blog", isExternal = false, sort = 2 },
                                }
                            },
                            new FooterColumnDto
                            {
                                title = "Destek",
                                sort = 1,
                                links = new List<FooterLinkDto>
                                {
                                    new FooterLinkDto{ label = "İletişim", pageSlug = "iletisim", isExternal = false, sort = 0 },
                                    new FooterLinkDto{ label = "SSS", pageSlug = "sss", isExternal = false, sort = 1 },
                                }
                            },
                        },
                        showNewsletter = false,
                        social = new List<FooterLinkDto>
                        {
                            new FooterLinkDto{ label = "instagram", url = "https://instagram.com", isExternal = true, sort = 0 },
                        }
                    };
                    var response = DataResponse<FooterConfigDto>.CreateSuccess(fallback);
                    var json = System.Text.Json.JsonSerializer.Serialize(response);
                    await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });
                    return Ok(response);
                }

                var obj = JsonSerializer.Deserialize<FooterConfigDto>(s.ConfigJson);
                obj ??= new FooterConfigDto();
                // Ensure sort ordering
                obj.columns = obj.columns.OrderBy(c => c.sort).Select(c =>
                {
                    c.links = (c.links ?? new List<FooterLinkDto>()).OrderBy(l => l.sort).ToList();
                    return c;
                }).ToList();
                obj.social = (obj.social ?? new List<FooterLinkDto>()).OrderBy(x => x.sort).ToList();
                var ok = DataResponse<FooterConfigDto>.CreateSuccess(obj);
                var cacheJson = System.Text.Json.JsonSerializer.Serialize(ok);
                await _cache.SetStringAsync(cacheKey, cacheJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
                return Ok(ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Footer config read error");
                return StatusCode(500, DataResponse<FooterConfigDto>.CreateFailure("Footer yüklenemedi"));
            }
        }
    }
}
