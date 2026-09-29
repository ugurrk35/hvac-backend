using System.Text.Json;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/product-campaigns/events")]
public class ProductCampaignEventsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.OrdinalIgnoreCase) { "view", "form_started", "location_selected", "quote_viewed", "add_to_cart", "whatsapp_clicked" };

    [HttpPost]
    public async Task<IActionResult> Track([FromBody] ProductCampaignEventRequest request)
    {
        if (request.ProductId <= 0 || !AllowedEvents.Contains(request.EventType ?? string.Empty)) return BadRequest(BaseResponse.CreateFailure("Geçersiz kampanya olayı."));
        db.ProductCampaignEvents.Add(new ProductCampaignEvent { ProductId = request.ProductId, ProductCampaignPackageId = request.PackageId, EventType = request.EventType!.Trim().ToLowerInvariant(), VisitorId = request.VisitorId?.Trim()[..Math.Min(request.VisitorId.Trim().Length, 100)], MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata), IsActive = true });
        await db.SaveChangesAsync(); return NoContent();
    }
}

public record ProductCampaignEventRequest(int ProductId, int? PackageId, string? EventType, string? VisitorId, Dictionary<string, string>? Metadata);
