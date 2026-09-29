using System.Security.Claims;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.API.Services;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly EmailConfirmationService _emailConfirmation;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;
    public AccountController(ApplicationDbContext db, EmailConfirmationService emailConfirmation, IEmailService email, IConfiguration configuration)
        => (_db, _emailConfirmation, _email, _configuration) = (db, emailConfirmation, email, configuration);

    private int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var items = await _db.CustomerAddresses.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.IsDefault).ThenByDescending(item => item.LastModifiedAt ?? item.CreatedAt).Select(AddressResponse.FromEntity).ToListAsync();
        return Ok(DataResponse<List<AddressResponse>>.CreateSuccess(items));
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> CreateAddress([FromBody] SaveAddressRequest request)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var validation = Validate(request); if (validation != null) return BadRequest(BaseResponse.CreateFailure(validation));
        var hasAddress = await _db.CustomerAddresses.AnyAsync(item => item.UserId == userId && !item.IsDeleted);
        if (request.IsDefault || !hasAddress) await _db.CustomerAddresses.Where(item => item.UserId == userId && !item.IsDeleted).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDefault, false));
        var item = request.ToEntity(userId.Value); item.IsDefault = request.IsDefault || !hasAddress;
        _db.CustomerAddresses.Add(item); await _db.SaveChangesAsync();
        return Ok(DataResponse<AddressResponse>.CreateSuccess(AddressResponse.From(item)));
    }

    [HttpPut("addresses/{addressId:int}")]
    public async Task<IActionResult> UpdateAddress(int addressId, [FromBody] SaveAddressRequest request)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var validation = Validate(request); if (validation != null) return BadRequest(BaseResponse.CreateFailure(validation));
        var item = await _db.CustomerAddresses.FirstOrDefaultAsync(value => value.Id == addressId && value.UserId == userId && !value.IsDeleted);
        if (item == null) return NotFound(BaseResponse.CreateFailure("Adres bulunamadı."));
        if (request.IsDefault) await _db.CustomerAddresses.Where(value => value.UserId == userId && value.Id != addressId && !value.IsDeleted).ExecuteUpdateAsync(setters => setters.SetProperty(value => value.IsDefault, false));
        request.Apply(item); await _db.SaveChangesAsync();
        return Ok(DataResponse<AddressResponse>.CreateSuccess(AddressResponse.From(item)));
    }

    [HttpDelete("addresses/{addressId:int}")]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var item = await _db.CustomerAddresses.FirstOrDefaultAsync(value => value.Id == addressId && value.UserId == userId && !value.IsDeleted);
        if (item == null) return NotFound(BaseResponse.CreateFailure("Adres bulunamadı."));
        item.IsDeleted = true; item.IsDefault = false; await _db.SaveChangesAsync();
        return Ok(BaseResponse.CreateSuccess("Adres silindi."));
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var items = await _db.CustomerFavorites.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted && item.Product!.IsPublished && !item.Product.IsDeleted)
            .OrderByDescending(item => item.CreatedAt).Select(item => new FavoriteResponse(item.ProductId, item.Product!.Name, item.Product.Slug, item.Product.DiscountPrice ?? item.Product.BasePrice, item.Product.ProductImages.OrderBy(image => image.SortOrder).Select(image => image.Image.Url).FirstOrDefault())).ToListAsync();
        return Ok(DataResponse<List<FavoriteResponse>>.CreateSuccess(items));
    }

    [HttpGet("marketing-consent")]
    public async Task<IActionResult> GetMarketingConsent()
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var item = await _db.CustomerMarketingConsents.AsNoTracking().FirstOrDefaultAsync(value => value.UserId == userId && !value.IsDeleted);
        return Ok(DataResponse<MarketingConsentResponse>.CreateSuccess(item == null ? new(false, false, false, null) : new(item.EmailMarketing, item.SmsMarketing, item.WhatsAppMarketing, item.UpdatedAt)));
    }

    [HttpPut("marketing-consent")]
    public async Task<IActionResult> UpdateMarketingConsent([FromBody] UpdateMarketingConsentRequest request)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var user = await _db.Users.FindAsync(new object[] { userId.Value }, HttpContext.RequestAborted);
        if (user == null || string.IsNullOrWhiteSpace(user.Email)) return BadRequest(BaseResponse.CreateFailure("Kampanya izni için geçerli bir e-posta adresi gereklidir."));
        var item = await _db.CustomerMarketingConsents.FirstOrDefaultAsync(value => value.UserId == userId);
        if (item == null) { item = new CustomerMarketingConsent { UserId = userId.Value }; _db.CustomerMarketingConsents.Add(item); }
        item.IsDeleted = false; item.IsActive = true; item.SmsMarketing = request.SmsMarketing; item.WhatsAppMarketing = request.WhatsAppMarketing; item.UpdatedAt = DateTime.UtcNow; item.Source = "account";
        if (!request.EmailMarketing)
        {
            item.EmailMarketing = false;
        }
        else if (!item.EmailMarketing)
        {
            var confirmationToken = await _emailConfirmation.CreateAsync(user.Email, EmailConfirmationPurposes.Marketing, user.Id, HttpContext.RequestAborted);
            var siteUrl = (_configuration["Email:PublicSiteUrl"] ?? "https://www.kombiklimaburada.com").TrimEnd('/');
            await _email.SendMarketingOptInAsync(user.Email, $"{siteUrl}/newsletter/confirm?token={Uri.EscapeDataString(confirmationToken)}&type=marketing", HttpContext.RequestAborted);
        }
        await _db.SaveChangesAsync();
        var message = request.EmailMarketing && !item.EmailMarketing ? "Bülten iznini tamamlamak için e-posta adresinize gönderilen bağlantıyı onaylayın." : "Pazarlama tercihleriniz güncellendi.";
        return Ok(DataResponse<MarketingConsentResponse>.CreateSuccess(new(item.EmailMarketing, item.SmsMarketing, item.WhatsAppMarketing, item.UpdatedAt), message));
    }

    [HttpPost("favorites/{productId:int}")]
    public async Task<IActionResult> AddFavorite(int productId)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        if (!await _db.Products.AnyAsync(product => product.Id == productId && product.IsPublished && !product.IsDeleted)) return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı."));
        var item = await _db.CustomerFavorites.FirstOrDefaultAsync(value => value.UserId == userId && value.ProductId == productId);
        if (item == null) _db.CustomerFavorites.Add(new CustomerFavorite { UserId = userId.Value, ProductId = productId });
        else { item.IsDeleted = false; item.IsActive = true; }
        await _db.SaveChangesAsync();
        return Ok(BaseResponse.CreateSuccess("Ürün favorilere eklendi."));
    }

    [HttpPost("price-alerts/{productId:int}")]
    public async Task<IActionResult> SubscribePriceAlert(int productId)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (string.IsNullOrWhiteSpace(user?.Email)) return BadRequest(BaseResponse.CreateFailure("Fiyat alarmı için geçerli bir e-posta adresi gereklidir."));
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == productId && item.IsPublished && !item.IsDeleted);
        if (product == null) return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı."));
        var item = await _db.PriceDropSubscriptions.FirstOrDefaultAsync(value => value.ProductId == productId && value.Email == user.Email);
        if (item == null) _db.PriceDropSubscriptions.Add(new PriceDropSubscription { ProductId = productId, UserId = userId, Email = user.Email.ToLowerInvariant(), ReferencePrice = product.DiscountPrice ?? product.BasePrice });
        else { item.IsDeleted = false; item.IsActive = true; item.UserId = userId; item.ReferencePrice = product.DiscountPrice ?? product.BasePrice; item.IsNotified = false; item.NotifiedAt = null; }
        await _db.SaveChangesAsync();
        return Ok(BaseResponse.CreateSuccess("Fiyat düştüğünde size haber vereceğiz."));
    }

    [HttpDelete("favorites/{productId:int}")]
    public async Task<IActionResult> RemoveFavorite(int productId)
    {
        var userId = CurrentUserId(); if (!userId.HasValue) return Unauthorized();
        var item = await _db.CustomerFavorites.FirstOrDefaultAsync(value => value.UserId == userId && value.ProductId == productId && !value.IsDeleted);
        if (item == null) return NotFound(BaseResponse.CreateFailure("Favori bulunamadı."));
        item.IsDeleted = true; await _db.SaveChangesAsync();
        return Ok(BaseResponse.CreateSuccess("Ürün favorilerden çıkarıldı."));
    }

    private static string? Validate(SaveAddressRequest request) => string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.RecipientName) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.District) || string.IsNullOrWhiteSpace(request.AddressLine) ? "Adres başlığı, alıcı adı, il, ilçe ve açık adres zorunludur." : null;
}

public record SaveAddressRequest(string Title, string RecipientName, string? Phone, string? Country, string City, string District, string? Neighborhood, string AddressLine, string? PostalCode, bool IsDefault)
{
    public CustomerAddress ToEntity(int userId) => new() { UserId = userId, Title = Title.Trim(), RecipientName = RecipientName.Trim(), Phone = Phone?.Trim(), Country = string.IsNullOrWhiteSpace(Country) ? "Türkiye" : Country.Trim(), City = City.Trim(), District = District.Trim(), Neighborhood = Neighborhood?.Trim(), AddressLine = AddressLine.Trim(), PostalCode = PostalCode?.Trim(), IsDefault = IsDefault };
    public void Apply(CustomerAddress item) { item.Title = Title.Trim(); item.RecipientName = RecipientName.Trim(); item.Phone = Phone?.Trim(); item.Country = string.IsNullOrWhiteSpace(Country) ? "Türkiye" : Country.Trim(); item.City = City.Trim(); item.District = District.Trim(); item.Neighborhood = Neighborhood?.Trim(); item.AddressLine = AddressLine.Trim(); item.PostalCode = PostalCode?.Trim(); item.IsDefault = IsDefault; }
}
public record AddressResponse(int Id, string Title, string RecipientName, string? Phone, string Country, string City, string District, string? Neighborhood, string AddressLine, string? PostalCode, bool IsDefault)
{
    public static AddressResponse From(CustomerAddress item) => new(item.Id, item.Title, item.RecipientName, item.Phone, item.Country, item.City, item.District, item.Neighborhood, item.AddressLine, item.PostalCode, item.IsDefault);
    public static readonly System.Linq.Expressions.Expression<Func<CustomerAddress, AddressResponse>> FromEntity = item => new AddressResponse(item.Id, item.Title, item.RecipientName, item.Phone, item.Country, item.City, item.District, item.Neighborhood, item.AddressLine, item.PostalCode, item.IsDefault);
}
public record FavoriteResponse(int ProductId, string Name, string Slug, decimal Price, string? ImageUrl);
public record UpdateMarketingConsentRequest(bool EmailMarketing, bool SmsMarketing, bool WhatsAppMarketing);
public record MarketingConsentResponse(bool EmailMarketing, bool SmsMarketing, bool WhatsAppMarketing, DateTime? UpdatedAt);
