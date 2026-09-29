using System.Security.Claims;
using ECommerce.API.Services;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/email-confirmation")]
public class EmailConfirmationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly EmailConfirmationService _confirmation;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;

    public EmailConfirmationController(ApplicationDbContext db, EmailConfirmationService confirmation, IEmailService email, IConfiguration configuration)
        => (_db, _confirmation, _email, _configuration) = (db, confirmation, email, configuration);

    [HttpPost("confirm-account")]
    public async Task<IActionResult> ConfirmAccount([FromBody] ConfirmTokenRequest request)
    {
        var item = await _confirmation.ConsumeAsync(request.Token, EmailConfirmationPurposes.Account, HttpContext.RequestAborted);
        if (item?.UserId is not int userId) return BadRequest(BaseResponse.CreateFailure("Doğrulama bağlantısı geçersiz veya süresi dolmuş."));
        var user = await _db.Users.FindAsync(new object[] { userId }, HttpContext.RequestAborted);
        if (user == null) return BadRequest(BaseResponse.CreateFailure("Kullanıcı bulunamadı."));
        user.EmailConfirmed = true;
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        await _email.SendWelcomeAsync(user, HttpContext.RequestAborted);
        return Ok(BaseResponse.CreateSuccess("E-posta adresiniz doğrulandı."));
    }

    [HttpPost("confirm-marketing")]
    public async Task<IActionResult> ConfirmMarketing([FromBody] ConfirmTokenRequest request)
    {
        var item = await _confirmation.ConsumeAsync(request.Token, EmailConfirmationPurposes.Marketing, HttpContext.RequestAborted);
        if (item?.UserId is not int userId) return BadRequest(BaseResponse.CreateFailure("Onay bağlantısı geçersiz veya süresi dolmuş."));
        var consent = await _db.CustomerMarketingConsents.SingleOrDefaultAsync(value => value.UserId == userId, HttpContext.RequestAborted);
        if (consent == null) { consent = new CustomerMarketingConsent { UserId = userId }; _db.CustomerMarketingConsents.Add(consent); }
        consent.IsActive = true; consent.IsDeleted = false; consent.EmailMarketing = true; consent.UpdatedAt = DateTime.UtcNow; consent.Source = "double-opt-in";
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(BaseResponse.CreateSuccess("Bülten aboneliğiniz onaylandı."));
    }

    [HttpPost("newsletter")]
    public async Task<IActionResult> SubscribeNewsletter([FromBody] NewsletterRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) return BadRequest(BaseResponse.CreateFailure("Geçerli bir e-posta adresi girin."));
        var subscription = await _db.NewsletterSubscriptions.SingleOrDefaultAsync(item => item.Email == email, HttpContext.RequestAborted);
        if (subscription == null)
        {
            subscription = new NewsletterSubscription { Email = email, Source = "footer", UnsubscribeToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant() };
            _db.NewsletterSubscriptions.Add(subscription);
        }
        subscription.IsConfirmed = false; subscription.ConfirmedAt = null; subscription.UnsubscribedAt = null; subscription.Source = "footer";
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        var token = await _confirmation.CreateAsync(email, EmailConfirmationPurposes.Newsletter, null, HttpContext.RequestAborted);
        await _email.SendMarketingOptInAsync(email, BuildSiteUrl($"/newsletter/confirm?token={Uri.EscapeDataString(token)}"), HttpContext.RequestAborted);
        return Ok(BaseResponse.CreateSuccess("Aboneliğinizi tamamlamak için e-posta adresinize gönderilen bağlantıyı onaylayın."));
    }

    [HttpPost("confirm-newsletter")]
    public async Task<IActionResult> ConfirmNewsletter([FromBody] ConfirmTokenRequest request)
    {
        var item = await _confirmation.ConsumeAsync(request.Token, EmailConfirmationPurposes.Newsletter, HttpContext.RequestAborted);
        if (item == null) return BadRequest(BaseResponse.CreateFailure("Onay bağlantısı geçersiz veya süresi dolmuş."));
        var subscription = await _db.NewsletterSubscriptions.SingleOrDefaultAsync(value => value.Email == item.Email, HttpContext.RequestAborted);
        if (subscription == null) return BadRequest(BaseResponse.CreateFailure("Abonelik bulunamadı."));
        subscription.IsConfirmed = true; subscription.ConfirmedAt = DateTime.UtcNow; subscription.UnsubscribedAt = null;
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(BaseResponse.CreateSuccess("Bülten aboneliğiniz onaylandı."));
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var subscription = await _db.NewsletterSubscriptions.SingleOrDefaultAsync(item => item.UnsubscribeToken == request.Token, HttpContext.RequestAborted);
        if (subscription == null) return BadRequest(BaseResponse.CreateFailure("Abonelik bağlantısı geçersiz."));
        subscription.IsConfirmed = false; subscription.UnsubscribedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(BaseResponse.CreateSuccess("E-posta aboneliğiniz sonlandırıldı."));
    }

    private string BuildSiteUrl(string path) => (_configuration["Email:PublicSiteUrl"] ?? "https://www.kombiklimaburada.com").TrimEnd('/') + path;
}

public record ConfirmTokenRequest(string Token);
public record NewsletterRequest(string? Email);
public record UnsubscribeRequest(string Token);
