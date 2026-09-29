using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/marketing")]
    [Authorize(Roles = "Admin")]
    public class AdminMarketingController : ControllerBase
    {
        private const string CacheKey = "marketing_integration_settings";
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AdminMarketingController> _logger;

        public AdminMarketingController(
            ApplicationDbContext db,
            IMemoryCache cache,
            ICurrentUserService currentUser,
            ILogger<AdminMarketingController> logger)
        {
            _db = db;
            _cache = cache;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet("settings")]
        public async Task<ActionResult<MarketingIntegrationSettings>> Get()
        {
            if (_cache.TryGetValue<MarketingIntegrationSettings>(CacheKey, out var cached))
            {
                return Ok(cached);
            }

            var settings = await _db.MarketingIntegrationSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new MarketingIntegrationSettings
                {
                    IsGAEnabled = false,
                    IsGTMEnabled = false,
                    IsFacebookEnabled = false,
                    IsTikTokEnabled = false
                };
            }

            _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            return Ok(settings);
        }

        public class UpdateMarketingSettingsRequest
        {
            public string? GTMContainerId { get; set; }
            public string? GoogleAnalyticsId { get; set; }
            public string? FacebookPixelId { get; set; }
            public string? TikTokPixelId { get; set; }
            public string? MetaConversionApiKey { get; set; }
            public bool IsGTMEnabled { get; set; }
            public bool IsGAEnabled { get; set; }
            public bool IsFacebookEnabled { get; set; }
            public bool IsTikTokEnabled { get; set; }
        }

        [HttpPost("settings")]
        public async Task<ActionResult<MarketingIntegrationSettings>> Update([FromBody] UpdateMarketingSettingsRequest request)
        {
            var settings = await _db.MarketingIntegrationSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new MarketingIntegrationSettings();
                _db.MarketingIntegrationSettings.Add(settings);
            }

            settings.GTMContainerId = request.GTMContainerId;
            settings.GoogleAnalyticsId = request.GoogleAnalyticsId;
            settings.FacebookPixelId = request.FacebookPixelId;
            settings.TikTokPixelId = request.TikTokPixelId;
            settings.MetaConversionApiKey = request.MetaConversionApiKey;
            settings.IsGTMEnabled = request.IsGTMEnabled;
            settings.IsGAEnabled = request.IsGAEnabled;
            settings.IsFacebookEnabled = request.IsFacebookEnabled;
            settings.IsTikTokEnabled = request.IsTikTokEnabled;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = _currentUser.UserId;

            await _db.SaveChangesAsync();

            _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            _cache.Remove("marketing_integration_public");

            return Ok(settings);
        }

        [HttpGet("server-side-settings")]
        public async Task<IActionResult> GetServerSideSettings()
        {
            var settings = await _db.MarketingIntegrationSettings.AsNoTracking().FirstOrDefaultAsync();
            return Ok(new ServerSideTrackingSettingsResponse(settings?.IsMetaServerSideEnabled ?? false, settings?.IsGoogleServerSideEnabled ?? false, settings?.IsTikTokServerSideEnabled ?? false, !string.IsNullOrWhiteSpace(settings?.MetaConversionApiAccessToken), !string.IsNullOrWhiteSpace(settings?.GoogleMeasurementProtocolApiSecret), !string.IsNullOrWhiteSpace(settings?.TikTokEventsApiAccessToken), settings?.GoogleAdsConversionId));
        }

        [HttpPost("server-side-settings")]
        public async Task<IActionResult> UpdateServerSideSettings([FromBody] UpdateServerSideTrackingSettingsRequest request)
        {
            var settings = await _db.MarketingIntegrationSettings.FirstOrDefaultAsync();
            if (settings == null) { settings = new MarketingIntegrationSettings(); _db.MarketingIntegrationSettings.Add(settings); }
            settings.IsMetaServerSideEnabled = request.IsMetaServerSideEnabled; settings.IsGoogleServerSideEnabled = request.IsGoogleServerSideEnabled; settings.IsTikTokServerSideEnabled = request.IsTikTokServerSideEnabled;
            if (!string.IsNullOrWhiteSpace(request.MetaAccessToken)) settings.MetaConversionApiAccessToken = request.MetaAccessToken.Trim();
            if (!string.IsNullOrWhiteSpace(request.GoogleMeasurementProtocolApiSecret)) settings.GoogleMeasurementProtocolApiSecret = request.GoogleMeasurementProtocolApiSecret.Trim();
            if (!string.IsNullOrWhiteSpace(request.TikTokAccessToken)) settings.TikTokEventsApiAccessToken = request.TikTokAccessToken.Trim();
            if (!string.IsNullOrWhiteSpace(request.GoogleAdsConversionId)) settings.GoogleAdsConversionId = request.GoogleAdsConversionId.Trim();
            settings.UpdatedAt = DateTime.UtcNow; settings.UpdatedBy = _currentUser.UserId;
            await _db.SaveChangesAsync(); _cache.Remove(CacheKey); _cache.Remove("marketing_integration_public");
            return Ok(new ServerSideTrackingSettingsResponse(settings.IsMetaServerSideEnabled, settings.IsGoogleServerSideEnabled, settings.IsTikTokServerSideEnabled, !string.IsNullOrWhiteSpace(settings.MetaConversionApiAccessToken), !string.IsNullOrWhiteSpace(settings.GoogleMeasurementProtocolApiSecret), !string.IsNullOrWhiteSpace(settings.TikTokEventsApiAccessToken), settings.GoogleAdsConversionId));
        }
    }

    public record UpdateServerSideTrackingSettingsRequest(bool IsMetaServerSideEnabled, bool IsGoogleServerSideEnabled, bool IsTikTokServerSideEnabled, string? MetaAccessToken, string? GoogleMeasurementProtocolApiSecret, string? GoogleAdsConversionId, string? TikTokAccessToken);
    public record ServerSideTrackingSettingsResponse(bool IsMetaServerSideEnabled, bool IsGoogleServerSideEnabled, bool IsTikTokServerSideEnabled, bool HasMetaAccessToken, bool HasGoogleMeasurementProtocolApiSecret, bool HasTikTokAccessToken, string? GoogleAdsConversionId);
}
