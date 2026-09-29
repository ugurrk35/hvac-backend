using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Services;

public sealed record MetaFunnelItem(string Id, int Quantity, decimal Price);

public sealed record MetaFunnelEvent(
    string EventName,
    string EventId,
    string? EventSourceUrl,
    decimal? Value,
    string? Currency,
    string? ProductId,
    string? ProductName,
    int? Quantity,
    decimal? Price,
    IReadOnlyList<MetaFunnelItem>? Items,
    string? Fbp,
    string? Fbc,
    string? TestEventCode);

public interface IServerSideConversionService
{
    Task TrackPurchaseAsync(int orderId, CancellationToken cancellationToken = default);
    Task TrackMetaFunnelEventAsync(MetaFunnelEvent funnelEvent, string? clientIpAddress, string? clientUserAgent, CancellationToken cancellationToken = default);
}
public sealed class ServerSideConversionService : IServerSideConversionService
{
    private readonly ApplicationDbContext _db; private readonly IHttpClientFactory _httpClientFactory; private readonly ILogger<ServerSideConversionService> _logger;
    public ServerSideConversionService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, ILogger<ServerSideConversionService> logger) { _db = db; _httpClientFactory = httpClientFactory; _logger = logger; }
    public async Task TrackPurchaseAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var settings = await _db.MarketingIntegrationSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings == null) return;
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == orderId && item.PaymentStatus == PaymentStatus.Paid, cancellationToken);
        if (order == null) return;
        var attribution = await _db.OrderAttributions.AsNoTracking().FirstOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
        await SendGoogleAsync(settings, order, attribution, cancellationToken);
        await SendMetaAsync(settings, order, attribution, cancellationToken);
        await SendTikTokAsync(settings, order, attribution, cancellationToken);
    }

    public async Task TrackMetaFunnelEventAsync(MetaFunnelEvent funnelEvent, string? clientIpAddress, string? clientUserAgent, CancellationToken cancellationToken = default)
    {
        var metaEventName = funnelEvent.EventName switch
        {
            "page_view" => "PageView",
            "view_item" => "ViewContent",
            "add_to_cart" => "AddToCart",
            "begin_checkout" => "InitiateCheckout",
            _ => null
        };

        if (metaEventName is null || string.IsNullOrWhiteSpace(funnelEvent.EventId)) return;

        var settings = await _db.MarketingIntegrationSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsMetaServerSideEnabled || string.IsNullOrWhiteSpace(settings.FacebookPixelId) || string.IsNullOrWhiteSpace(settings.MetaConversionApiAccessToken)) return;

        try
        {
            var contents = BuildContents(funnelEvent);
            var contentIds = contents?.Select(item => item.Id).ToArray();
            var metaContents = contents?.Select(item => new { id = item.Id, quantity = item.Quantity, item_price = item.ItemPrice }).ToArray();
            var sourceUrl = NormalizeEventSourceUrl(funnelEvent.EventSourceUrl);
            var body = new
            {
                data = new[]
                {
                    new
                    {
                        event_name = metaEventName,
                        event_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        event_id = funnelEvent.EventId,
                        test_event_code = NormalizeTestEventCode(funnelEvent.TestEventCode),
                        action_source = "website",
                        event_source_url = sourceUrl,
                        user_data = new
                        {
                            client_ip_address = clientIpAddress,
                            client_user_agent = clientUserAgent,
                            fbp = funnelEvent.Fbp,
                            fbc = funnelEvent.Fbc
                        },
                        custom_data = new
                        {
                            currency = NormalizeCurrency(funnelEvent.Currency),
                            value = funnelEvent.Value ?? CalculateValue(contents),
                            content_type = contents?.Length > 0 ? "product" : null,
                            content_ids = contentIds,
                            contents = metaContents,
                            content_name = funnelEvent.ProductName
                        }
                    }
                }
            };

            var url = $"https://graph.facebook.com/v22.0/{Uri.EscapeDataString(settings.FacebookPixelId)}/events?access_token={Uri.EscapeDataString(settings.MetaConversionApiAccessToken)}";
            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync(url, body, cancellationToken);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("Meta CAPI funnel event gönderilemedi. Event: {Event}, Status: {Status}", metaEventName, (int)response.StatusCode);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Meta CAPI funnel event gönderim hatası. Event: {Event}", metaEventName);
        }
    }

    private async Task SendGoogleAsync(MarketingIntegrationSettings settings, Order order, OrderAttribution? attribution, CancellationToken cancellationToken)
    {
        if (!settings.IsGoogleServerSideEnabled || string.IsNullOrWhiteSpace(settings.GoogleAnalyticsId) || string.IsNullOrWhiteSpace(settings.GoogleMeasurementProtocolApiSecret)) return;
        var clientId = attribution?.VisitorId ?? $"order.{order.Id}";
        var url = $"https://www.google-analytics.com/mp/collect?measurement_id={Uri.EscapeDataString(settings.GoogleAnalyticsId)}&api_secret={Uri.EscapeDataString(settings.GoogleMeasurementProtocolApiSecret)}";
        try
        {
            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync(url, new { client_id = clientId, events = new[] { new { name = "purchase", @params = new { transaction_id = order.OrderNumber ?? order.Id.ToString(), value = order.TotalAmount, currency = "TRY", source = attribution?.Source, medium = attribution?.Medium, campaign = attribution?.Campaign } } } }, cancellationToken);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("Google server-side purchase gönderilemedi. OrderId: {OrderId}, Status: {Status}", order.Id, (int)response.StatusCode);
        }
        catch (Exception exception) { _logger.LogWarning(exception, "Google server-side purchase gönderim hatası. OrderId: {OrderId}", order.Id); }
    }

    private async Task SendMetaAsync(MarketingIntegrationSettings settings, Order order, OrderAttribution? attribution, CancellationToken cancellationToken)
    {
        if (!settings.IsMetaServerSideEnabled || string.IsNullOrWhiteSpace(settings.FacebookPixelId) || string.IsNullOrWhiteSpace(settings.MetaConversionApiAccessToken)) return;
        try
        {
            var eventId = $"order-{order.Id}"; var email = order.CustomerEmail?.Trim().ToLowerInvariant();
            var body = new { data = new[] { new { event_name = "Purchase", event_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), event_id = eventId, action_source = "website", event_source_url = "https://www.kombiklimaburada.com/checkout/success", user_data = new { em = string.IsNullOrWhiteSpace(email) ? null : new[] { Hash(email) }, external_id = order.UserId?.ToString() }, custom_data = new { currency = "TRY", value = order.TotalAmount, order_id = order.OrderNumber ?? order.Id.ToString(), source = attribution?.Source, campaign = attribution?.Campaign } } } };
            var url = $"https://graph.facebook.com/v22.0/{Uri.EscapeDataString(settings.FacebookPixelId)}/events?access_token={Uri.EscapeDataString(settings.MetaConversionApiAccessToken)}";
            var response = await _httpClientFactory.CreateClient().PostAsJsonAsync(url, body, cancellationToken);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("Meta CAPI purchase gönderilemedi. OrderId: {OrderId}, Status: {Status}", order.Id, (int)response.StatusCode);
        }
        catch (Exception exception) { _logger.LogWarning(exception, "Meta CAPI purchase gönderim hatası. OrderId: {OrderId}", order.Id); }
    }

    private async Task SendTikTokAsync(MarketingIntegrationSettings settings, Order order, OrderAttribution? attribution, CancellationToken cancellationToken)
    {
        if (!settings.IsTikTokServerSideEnabled || string.IsNullOrWhiteSpace(settings.TikTokPixelId) || string.IsNullOrWhiteSpace(settings.TikTokEventsApiAccessToken)) return;
        try
        {
            var body = new { pixel_code = settings.TikTokPixelId, @event = "CompletePayment", event_id = $"order-{order.Id}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), properties = new { currency = "TRY", value = order.TotalAmount, order_id = order.OrderNumber ?? order.Id.ToString(), source = attribution?.Source, campaign = attribution?.Campaign } };
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://business-api.tiktok.com/open_api/v1.3/pixel/track/") { Content = JsonContent.Create(body) };
            request.Headers.TryAddWithoutValidation("Access-Token", settings.TikTokEventsApiAccessToken);
            var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("TikTok Events API purchase gönderilemedi. OrderId: {OrderId}, Status: {Status}", order.Id, (int)response.StatusCode);
        }
        catch (Exception exception) { _logger.LogWarning(exception, "TikTok Events API purchase gönderim hatası. OrderId: {OrderId}", order.Id); }
    }

    private sealed record MetaContent(string Id, int Quantity, decimal ItemPrice);

    private static MetaContent[]? BuildContents(MetaFunnelEvent funnelEvent)
    {
        if (funnelEvent.Items?.Count > 0)
        {
            return funnelEvent.Items
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Take(50)
                .Select(item => new MetaContent(item.Id, Math.Clamp(item.Quantity, 1, 100), Math.Max(item.Price, 0)))
                .ToArray();
        }

        if (string.IsNullOrWhiteSpace(funnelEvent.ProductId)) return null;
        return new[]
        {
            new MetaContent(funnelEvent.ProductId, Math.Clamp(funnelEvent.Quantity ?? 1, 1, 100), Math.Max(funnelEvent.Price ?? funnelEvent.Value ?? 0, 0))
        };
    }

    private static decimal CalculateValue(IReadOnlyCollection<MetaContent>? contents) => contents?.Sum(item => item.Quantity * item.ItemPrice) ?? 0;

    private static string NormalizeCurrency(string? currency) => string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase) ? "TRY" : "TRY";

    private static string? NormalizeTestEventCode(string? testEventCode) => !string.IsNullOrWhiteSpace(testEventCode)
        && testEventCode.Length <= 128
        && testEventCode.All(character => char.IsLetterOrDigit(character) || character is '_' or '-')
            ? testEventCode
            : null;

    private static string NormalizeEventSourceUrl(string? eventSourceUrl)
    {
        if (Uri.TryCreate(eventSourceUrl, UriKind.Absolute, out var parsed)
            && (string.Equals(parsed.Host, "www.kombiklimaburada.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parsed.Host, "kombiklimaburada.com", StringComparison.OrdinalIgnoreCase)))
        {
            return parsed.GetLeftPart(UriPartial.Path) + parsed.Query;
        }

        return "https://www.kombiklimaburada.com/";
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
