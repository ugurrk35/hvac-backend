using System.ComponentModel.DataAnnotations;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockNotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StockNotificationsController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] StockNotificationRequest request)
    {
        if (!new EmailAddressAttribute().IsValid(request.Email)) return BadRequest(BaseResponse.CreateFailure("Geçerli bir e-posta adresi girin."));
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.ProductId && item.IsPublished && !item.IsDeleted);
        if (product == null) return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı."));
        if (product.Quantity > 0) return BadRequest(BaseResponse.CreateFailure("Bu ürün şu anda stokta; bildirim kaydı gerekli değil."));
        var email = request.Email.Trim().ToLowerInvariant();
        var item = await _db.BackInStockSubscriptions.FirstOrDefaultAsync(value => value.ProductId == request.ProductId && value.Email == email);
        if (item == null) _db.BackInStockSubscriptions.Add(new BackInStockSubscription { ProductId = request.ProductId, Email = email });
        else { item.IsDeleted = false; item.IsActive = true; item.IsNotified = false; item.NotifiedAt = null; }
        await _db.SaveChangesAsync();
        return Ok(BaseResponse.CreateSuccess("Ürün yeniden stokta olduğunda size e-posta göndereceğiz."));
    }
}
public record StockNotificationRequest(int ProductId, string Email);
