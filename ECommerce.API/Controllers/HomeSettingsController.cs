using System.Text.Json;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ana sayfa vitrin ayarlarını (çok satanlar, favoriler vb.) yönetir ve yayınlar.
    /// Admin yetkisiyle konfigürasyon güncellenir; public uçlarla veriler okunur.
    /// </summary>
    [ApiController]
    public class HomeSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HomeSettingsController> _logger;
        private readonly IDistributedCache _cache;

        public HomeSettingsController(
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            ILogger<HomeSettingsController> logger,
            IDistributedCache cache)
        {
            _db = db;
            _currentUser = currentUser;
            _logger = logger;
            _cache = cache;
        }

        public class UpdateHomeBestsellersRequest
        {
            public string? Title { get; set; }
            public bool IsEnabled { get; set; } = true;
            public List<int> ProductIds { get; set; } = new();
            public int? MaxItems { get; set; }
            public int? GridColumns { get; set; }
        }

        public class HomeBestsellersConfigDto
        {
            public string Title { get; set; } = "Çok Satanlar";
            public bool IsEnabled { get; set; }
            public IReadOnlyList<ProductListDto> Items { get; set; } = Array.Empty<ProductListDto>();
            public int GridColumns { get; set; } = 4;
            public int MaxItems { get; set; } = 4;
        }
        public class HomeFeaturedConfigDto
        {
            public string Title { get; set; } = "Favorilerimiz";
            public bool IsEnabled { get; set; }
            public IReadOnlyList<ProductListDto> Items { get; set; } = Array.Empty<ProductListDto>();
            public int GridColumns { get; set; } = 4;
            public int MaxItems { get; set; } = 4;
        }

        [HttpGet("api/admin/home/settings/bestsellers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HomeBestsellersSettings>> GetAdmin()
        {
            var settings = await _db.HomeBestsellersSettings
      .AsNoTracking()
      .OrderBy(x => x.Id)
      .FirstOrDefaultAsync();
            settings ??= new HomeBestsellersSettings
            {
                Title = "Çok Satanlar",
                IsEnabled = false,
                ProductIdsJson = "[]",
            };
            return Ok(settings);
        }

        [HttpPost("api/admin/home/settings/bestsellers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HomeBestsellersSettings>> UpdateAdmin([FromBody] UpdateHomeBestsellersRequest req)
        {
            var settings = await _db.HomeBestsellersSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new HomeBestsellersSettings();
                _db.HomeBestsellersSettings.Add(settings);
            }

            settings.Title = string.IsNullOrWhiteSpace(req.Title) ? "Çok Satanlar" : req.Title!.Trim();
            settings.IsEnabled = req.IsEnabled;
            settings.ProductIdsJson = JsonSerializer.Serialize((req.ProductIds ?? new List<int>()).Distinct().Take(8));
            settings.MaxItems = Math.Clamp(req.MaxItems ?? settings.MaxItems, 1, 8);
            settings.GridColumns = Math.Clamp(req.GridColumns ?? settings.GridColumns, 2, 4);
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();
            await _cache.RemoveAsync("home:bestsellers");
            return Ok(settings);
        }

        [HttpGet("api/home/bestsellers")]
        [AllowAnonymous]
        public async Task<ActionResult<DataResponse<HomeBestsellersConfigDto>>> GetPublic()
        {
            try
            {
                var cachedJson = await _cache.GetStringAsync("home:bestsellers");
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = System.Text.Json.JsonSerializer.Deserialize<DataResponse<HomeBestsellersConfigDto>>(cachedJson);
                    if (cached != null) return Ok(cached);
                }
                var settings = await _db.HomeBestsellersSettings.AsNoTracking().FirstOrDefaultAsync();

                var result = new HomeBestsellersConfigDto
                {
                    Title = settings?.Title ?? "Çok Satanlar",
                    IsEnabled = settings?.IsEnabled ?? false,
                    GridColumns = settings?.GridColumns ?? 4,
                    MaxItems = settings?.MaxItems ?? 4,
                };

                List<int> ids = new();
                if (!string.IsNullOrWhiteSpace(settings?.ProductIdsJson))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<int>>(settings!.ProductIdsJson!);
                        if (parsed != null) ids = parsed.Where(x => x > 0).Distinct().Take(8).ToList();
                    }
                    catch { }
                }

                if (result.IsEnabled && ids.Count > 0)
                {
                    var products = await _db.Products
                        .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
                        .Include(p => p.Category)
                        .Where(p => ids.Contains(p.Id) && p.IsPublished && !p.IsDeleted)
                        .ToListAsync();

                    // Korunan sıra: ids sırasına göre diz
                    var ordered = products.OrderBy(p => ids.IndexOf(p.Id)).Take(result.MaxItems).ToList();
                    var mapped = ordered.Select(product => product.ToListDto()).ToList();
                    result.Items = mapped;
                }
                else
                {
                    // Fallback: 4 ürün (yayında olanlardan)
                    var fallback = await _db.Products
                        .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
                        .Include(p => p.Category)
                        .Where(p => p.IsPublished && !p.IsDeleted)
                        .OrderByDescending(p => p.Id)
                        .Take(result.MaxItems)
                        .ToListAsync();
                    result.Items = fallback.Select(product => product.ToListDto()).ToList();
                }
                var response = DataResponse<HomeBestsellersConfigDto>.CreateSuccess(result);
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                await _cache.SetStringAsync("home:bestsellers", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Home bestsellers getirilemedi");
                return StatusCode(500, DataResponse<HomeBestsellersConfigDto>.CreateFailure("Ayarlar getirilemedi"));
            }
        }

        // --- Featured (Favorilerimiz) ---
        public class UpdateHomeFeaturedRequest
        {
            public string? Title { get; set; }
            public bool IsEnabled { get; set; } = true;
            public List<int> ProductIds { get; set; } = new();
            public int? MaxItems { get; set; }
            public int? GridColumns { get; set; }
        }

        [HttpGet("api/admin/home/settings/featured")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HomeFeaturedSettings>> GetFeaturedAdmin()
        {
            var settings = await _db.HomeFeaturedSettings
    .AsNoTracking()
    .Where(x => x.Id == 1)
    .FirstOrDefaultAsync();
            settings ??= new HomeFeaturedSettings
            {
                Title = "Favorilerimiz",
                IsEnabled = false,
                ProductIdsJson = "[]",
            };
            return Ok(settings);
        }

        [HttpPost("api/admin/home/settings/featured")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<HomeFeaturedSettings>> UpdateFeaturedAdmin(
     [FromBody] UpdateHomeFeaturedRequest req)
        {
            var settings = await _db.HomeFeaturedSettings.OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.Id == 1);

            if (settings == null)
            {
                settings = new HomeFeaturedSettings
                {
                    Id = 1
                };
                _db.HomeFeaturedSettings.Add(settings);
            }

            settings.Title = string.IsNullOrWhiteSpace(req.Title)
                ? "Favorilerimiz"
                : req.Title.Trim();

            settings.IsEnabled = req.IsEnabled;
            settings.ProductIdsJson = JsonSerializer.Serialize(
                (req.ProductIds ?? new List<int>()).Distinct().Take(8)
            );

            settings.MaxItems = Math.Clamp(req.MaxItems ?? settings.MaxItems, 1, 8);
            settings.GridColumns = Math.Clamp(req.GridColumns ?? settings.GridColumns, 2, 4);
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();
            await _cache.RemoveAsync("home:featured");

            return Ok(settings);
        }

        [HttpGet("api/home/featured")]
        [AllowAnonymous]
        public async Task<ActionResult<DataResponse<HomeFeaturedConfigDto>>> GetFeaturedPublic()
        {
            try
            {
                var cachedJson = await _cache.GetStringAsync("home:featured");
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = System.Text.Json.JsonSerializer.Deserialize<DataResponse<HomeFeaturedConfigDto>>(cachedJson);
                    if (cached != null) return Ok(cached);
                }
                var settings = await _db.HomeFeaturedSettings.AsNoTracking().FirstOrDefaultAsync();
                var result = new HomeFeaturedConfigDto
                {
                    Title = settings?.Title ?? "Favorilerimiz",
                    IsEnabled = settings?.IsEnabled ?? false,
                    GridColumns = settings?.GridColumns ?? 4,
                    MaxItems = settings?.MaxItems ?? 4,
                };

                List<int> ids = new();
                if (!string.IsNullOrWhiteSpace(settings?.ProductIdsJson))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<int>>(settings!.ProductIdsJson!);
                        if (parsed != null) ids = parsed.Where(x => x > 0).Distinct().Take(8).ToList();
                    }
                    catch { }
                }

                if (result.IsEnabled && ids.Count > 0)
                {
                    var products = await _db.Products
                        .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
                        .Include(p => p.Category)
                        .Where(p => ids.Contains(p.Id) && p.IsPublished && !p.IsDeleted)
                        .ToListAsync();
                    var ordered = products.OrderBy(p => ids.IndexOf(p.Id)).Take(result.MaxItems).ToList();
                    result.Items = ordered.Select(product => product.ToListDto()).ToList();
                }
                else
                {
                    var fallback = await _db.Products
                        .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
                        .Include(p => p.Category)
                        .Where(p => p.IsPublished && !p.IsDeleted)
                        .OrderByDescending(p => p.Id)
                        .Take(result.MaxItems)
                        .ToListAsync();
                    result.Items = fallback.Select(product => product.ToListDto()).ToList();
                }
                var response = DataResponse<HomeFeaturedConfigDto>.CreateSuccess(result);
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                await _cache.SetStringAsync("home:featured", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Home featured getirilemedi");
                return StatusCode(500, DataResponse<HomeFeaturedConfigDto>.CreateFailure("Ayarlar getirilemedi"));
            }
        }
    }
}
