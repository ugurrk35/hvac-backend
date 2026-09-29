using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminAutomationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminAutomationController(ApplicationDbContext db) => _db = db;

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] MarketingJobStatus? status = null, [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        var query = _db.MarketingAutomationJobs.AsNoTracking().Where(item => !item.IsDeleted);
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        var items = await query.OrderByDescending(item => item.ScheduledAt).Take(take).Select(item => new AutomationJobResponse(item.Id, item.Type, item.Channel, item.Status, item.Recipient, item.ScheduledAt, item.ProcessedAt, item.AttemptCount, item.ErrorMessage, item.PayloadJson)).ToListAsync();
        return Ok(DataResponse<List<AutomationJobResponse>>.CreateSuccess(items));
    }

    [HttpGet("back-in-stock-subscriptions")]
    public async Task<IActionResult> GetBackInStockSubscriptions([FromQuery] int take = 100)
    {
        var items = await _db.BackInStockSubscriptions.AsNoTracking().Include(item => item.Product).Where(item => !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Take(Math.Clamp(take, 1, 500)).Select(item => new SubscriptionResponse(item.Id, item.ProductId, item.Product!.Name, item.Email, item.IsNotified, item.CreatedAt)).ToListAsync();
        return Ok(DataResponse<List<SubscriptionResponse>>.CreateSuccess(items));
    }

    [HttpPost("jobs/{id:int}/retry")]
    public async Task<IActionResult> Retry(int id)
    {
        var job = await _db.MarketingAutomationJobs.FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (job == null) return NotFound(BaseResponse.CreateFailure("Otomasyon işi bulunamadı."));
        job.Status = MarketingJobStatus.Pending; job.ScheduledAt = DateTime.UtcNow; job.ProcessedAt = null; job.ErrorMessage = null;
        await _db.SaveChangesAsync(); return Ok(BaseResponse.CreateSuccess("İş yeniden kuyruğa alındı."));
    }

    [HttpGet("price-drop-subscriptions")]
    public async Task<IActionResult> GetPriceDropSubscriptions([FromQuery] int take = 100)
    {
        var items = await _db.PriceDropSubscriptions.AsNoTracking().Include(item => item.Product).Where(item => !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Take(Math.Clamp(take, 1, 500)).Select(item => new PriceDropSubscriptionResponse(item.Id, item.ProductId, item.Product!.Name, item.Email, item.ReferencePrice, item.IsNotified, item.CreatedAt)).ToListAsync();
        return Ok(DataResponse<List<PriceDropSubscriptionResponse>>.CreateSuccess(items));
    }
}
public record AutomationJobResponse(int Id, MarketingAutomationType Type, MarketingChannel Channel, MarketingJobStatus Status, string Recipient, DateTime ScheduledAt, DateTime? ProcessedAt, int AttemptCount, string? ErrorMessage, string PayloadJson);
public record SubscriptionResponse(int Id, int ProductId, string ProductName, string Email, bool IsNotified, DateTime CreatedAt);
public record PriceDropSubscriptionResponse(int Id, int ProductId, string ProductName, string Email, decimal ReferencePrice, bool IsNotified, DateTime CreatedAt);
