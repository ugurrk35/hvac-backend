using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminPerformanceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminPerformanceController(ApplicationDbContext db) => _db = db;
    [HttpGet("web-vitals")]
    public async Task<IActionResult> GetWebVitals([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 90); var since = DateTime.UtcNow.AddDays(-days);
        var items = await _db.WebVitalMetrics.AsNoTracking().Where(item => !item.IsDeleted && item.CreatedAt >= since).GroupBy(item => item.Name).Select(group => new WebVitalSummary(group.Key, group.Count(), group.Average(item => item.Value), group.OrderBy(item => item.Value).Select(item => item.Value).FirstOrDefault())).ToListAsync();
        return Ok(DataResponse<List<WebVitalSummary>>.CreateSuccess(items));
    }
}
public record WebVitalSummary(string Name, int SampleCount, double Average, double Minimum);
