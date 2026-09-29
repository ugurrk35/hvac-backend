using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/roas")]
[Authorize(Roles = "Admin")]
public class AdminRoasController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminRoasController(ApplicationDbContext db) => _db = db;

    [HttpGet("spends")]
    public async Task<IActionResult> GetSpends([FromQuery] int days = 30)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365)));
        return Ok(await _db.AdvertisingSpends.AsNoTracking().Where(x => !x.IsDeleted && x.SpendDate >= since).OrderByDescending(x => x.SpendDate).ToListAsync());
    }

    [HttpPost("spends")]
    public async Task<IActionResult> SaveSpend([FromBody] SaveAdvertisingSpendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform) || request.Amount < 0) return BadRequest("Platform ve geçerli harcama tutarı zorunludur.");
        var item = request.Id.HasValue ? await _db.AdvertisingSpends.FindAsync(request.Id.Value) : null;
        if (request.Id.HasValue && item == null) return NotFound();
        item ??= new AdvertisingSpend { IsActive = true }; if (item.Id == 0) _db.AdvertisingSpends.Add(item);
        item.Platform = request.Platform.Trim().ToLowerInvariant(); item.Campaign = string.IsNullOrWhiteSpace(request.Campaign) ? null : request.Campaign.Trim(); item.SpendDate = request.SpendDate; item.Amount = request.Amount; item.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(); item.LastModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return Ok(item);
    }

    [HttpDelete("spends/{id:int}")]
    public async Task<IActionResult> DeleteSpend(int id)
    {
        var item = await _db.AdvertisingSpends.FindAsync(id); if (item == null || item.IsDeleted) return NotFound();
        item.IsDeleted = true; await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
        var revenue = await (from attribution in _db.OrderAttributions.AsNoTracking()
                             join order in _db.Orders.AsNoTracking() on attribution.OrderId equals order.Id
                             where !attribution.IsDeleted && !order.IsDeleted && order.PaymentStatus == PaymentStatus.Paid && order.CreatedAt >= since
                             group order by new { Platform = (attribution.Source ?? "direct").ToLower(), Campaign = attribution.Campaign ?? "(kampanya yok)" } into groupedOrders
                             select new { groupedOrders.Key.Platform, groupedOrders.Key.Campaign, Revenue = groupedOrders.Sum(x => x.TotalAmount), Orders = groupedOrders.Count() }).ToListAsync();
        var spend = await _db.AdvertisingSpends.AsNoTracking().Where(x => !x.IsDeleted && x.SpendDate >= DateOnly.FromDateTime(since)).GroupBy(x => new { Platform = x.Platform.ToLower(), Campaign = x.Campaign ?? "(kampanya yok)" }).Select(groupedSpend => new { groupedSpend.Key.Platform, groupedSpend.Key.Campaign, Spend = groupedSpend.Sum(x => x.Amount) }).ToListAsync();
        var keys = revenue.Select(x => (x.Platform, x.Campaign)).Union(spend.Select(x => (x.Platform, x.Campaign))).OrderBy(x => x.Platform).ThenBy(x => x.Campaign);
        return Ok(keys.Select(key => { var r = revenue.FirstOrDefault(x => x.Platform == key.Platform && x.Campaign == key.Campaign); var s = spend.FirstOrDefault(x => x.Platform == key.Platform && x.Campaign == key.Campaign); var amount = s?.Spend ?? 0; return new RoasRow(key.Platform, key.Campaign, r?.Orders ?? 0, r?.Revenue ?? 0, amount, amount == 0 ? null : Math.Round((r?.Revenue ?? 0) / amount, 2)); }));
    }
}
public record SaveAdvertisingSpendRequest(int? Id, string Platform, string? Campaign, DateOnly SpendDate, decimal Amount, string? Currency);
public record RoasRow(string Platform, string Campaign, int Orders, decimal Revenue, decimal Spend, decimal? Roas);
