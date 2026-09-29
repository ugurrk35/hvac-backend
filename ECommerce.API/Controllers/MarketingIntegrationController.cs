using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.API.Services;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.API.Controllers
{
    [ApiController]
    public class MarketingIntegrationController : ControllerBase
    {
        private const string CacheKey = "marketing_integration_public";
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MarketingIntegrationController> _logger;
        private readonly IServerSideConversionService _serverSideConversions;

        public MarketingIntegrationController(
            ApplicationDbContext db,
            IMemoryCache cache,
            ICurrentUserService currentUser,
            ILogger<MarketingIntegrationController> logger,
            IServerSideConversionService serverSideConversions)
        {
            _db = db;
            _cache = cache;
            _currentUser = currentUser;
            _logger = logger;
            _serverSideConversions = serverSideConversions;
        }

        public class PublicMarketingConfigDto
        {
            public string? GTMContainerId { get; set; }
            public string? GoogleAnalyticsId { get; set; }
            public string? FacebookPixelId { get; set; }
            public string? TikTokPixelId { get; set; }
            public bool IsGTMEnabled { get; set; }
            public bool IsGAEnabled { get; set; }
            public bool IsFacebookEnabled { get; set; }
            public bool IsTikTokEnabled { get; set; }
        }

        [HttpGet("api/marketing/settings")]
        [AllowAnonymous]
        public async Task<ActionResult<PublicMarketingConfigDto>> Get()
        {
            if (_cache.TryGetValue<PublicMarketingConfigDto>(CacheKey, out var cached))
            {
                return Ok(cached);
            }
            var settings = await _db.MarketingIntegrationSettings
    .AsNoTracking()
    .OrderBy(x => x.Id)
    .FirstOrDefaultAsync();

            var dto = new PublicMarketingConfigDto
            {
                GTMContainerId = settings?.GTMContainerId,
                GoogleAnalyticsId = settings?.GoogleAnalyticsId,
                FacebookPixelId = settings?.FacebookPixelId,
                TikTokPixelId = settings?.TikTokPixelId,
                IsGAEnabled = settings?.IsGAEnabled ?? false,
                IsGTMEnabled = settings?.IsGTMEnabled ?? false,
                IsFacebookEnabled = settings?.IsFacebookEnabled ?? false,
                IsTikTokEnabled = settings?.IsTikTokEnabled ?? false
            };

            _cache.Set(CacheKey, dto, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return Ok(dto);
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

        public sealed class MetaFunnelEventRequest
        {
            public string? EventName { get; set; }
            public string? EventId { get; set; }
            public string? EventSourceUrl { get; set; }
            public decimal? Value { get; set; }
            public string? Currency { get; set; }
            public string? ProductId { get; set; }
            public string? ProductName { get; set; }
            public int? Quantity { get; set; }
            public decimal? Price { get; set; }
            public List<MetaFunnelItemRequest>? Items { get; set; }
            public string? Fbp { get; set; }
            public string? Fbc { get; set; }
            public string? TestEventCode { get; set; }
        }

        public sealed class MetaFunnelItemRequest
        {
            public string? Id { get; set; }
            public int? Quantity { get; set; }
            public decimal? Price { get; set; }
        }

        [HttpPost("api/marketing/meta-events")]
        [AllowAnonymous]
        public async Task<IActionResult> TrackMetaEvent([FromBody] MetaFunnelEventRequest request, CancellationToken cancellationToken)
        {
            var allowedEvents = new[] { "page_view", "view_item", "add_to_cart", "begin_checkout" };
            if (string.IsNullOrWhiteSpace(request.EventName) || !allowedEvents.Contains(request.EventName, StringComparer.Ordinal)
                || string.IsNullOrWhiteSpace(request.EventId) || request.EventId.Length > 128)
            {
                return BadRequest();
            }

            var clientIpAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var funnelEvent = new MetaFunnelEvent(
                request.EventName,
                request.EventId,
                request.EventSourceUrl,
                request.Value,
                request.Currency,
                request.ProductId,
                request.ProductName,
                request.Quantity,
                request.Price,
                request.Items?
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                    .Select(item => new MetaFunnelItem(item.Id!, item.Quantity ?? 1, item.Price ?? 0))
                    .ToArray(),
                request.Fbp,
                request.Fbc,
                request.TestEventCode);

            await _serverSideConversions.TrackMetaFunnelEventAsync(funnelEvent, clientIpAddress, Request.Headers.UserAgent.ToString(), cancellationToken);
            return Accepted();
        }

        // Admin update endpoints live in Admin/AdminMarketingController
    }
}
