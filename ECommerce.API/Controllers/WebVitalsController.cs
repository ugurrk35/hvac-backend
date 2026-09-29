using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebVitalsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WebVitalsController(ApplicationDbContext db) => _db = db;
    [HttpPost]
    public async Task<IActionResult> Record([FromBody] WebVitalRequest request)
    {
        if (request.Name is not ("LCP" or "CLS" or "INP") || !double.IsFinite(request.Value) || request.Value < 0) return BadRequest();
        _db.WebVitalMetrics.Add(new WebVitalMetric { Name = request.Name, Value = request.Value, Path = Trim(request.Path, 500), VisitorId = Trim(request.VisitorId, 120) });
        await _db.SaveChangesAsync(); return NoContent();
    }
    private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
public record WebVitalRequest(string Name, double Value, string? Path, string? VisitorId);
