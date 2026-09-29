using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using equals.Domain.Interfaces;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Roles = "Admin")]
public class AdminEmailTemplatesController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, (string Subject, string Html)> Defaults = new Dictionary<string, (string, string)>
    {
        ["welcome"] = ("Kombi Klima Burada'ya hoş geldiniz", "<p>Merhaba {customerName},</p><p>Kombi Klima Burada hesabınız başarıyla oluşturuldu.</p><p>Ürünlerimizi keşfetmeye devam edebilirsiniz.</p>"),
        ["email-verification"] = ("E-posta adresinizi doğrulayın", "<p>Merhaba {customerName},</p><p>Kombi Klima Burada hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın.</p><p><a href=\"{verificationUrl}\">E-posta adresimi doğrula</a></p><p>Bu bağlantı 24 saat geçerlidir.</p>"),
        ["marketing-opt-in"] = ("Bülten aboneliğinizi onaylayın", "<p>Merhaba,</p><p>Kampanya ve ürün haberlerini almak için e-posta adresinizi onaylayın.</p><p><a href=\"{confirmationUrl}\">Bülten aboneliğini onayla</a></p><p>Bu bağlantı 24 saat geçerlidir.</p>"),
        ["order-paid"] = ("Siparişiniz alındı · #{orderNumber}", "<p>Merhaba {customerName},</p><p><strong>#{orderNumber}</strong> numaralı siparişinizin ödemesi başarıyla alındı.</p><ul>{orderItems}</ul><p><strong>Toplam: {orderTotal}</strong></p><p>Siparişinizi özenle hazırlayıp en kısa sürede kargoya vereceğiz.</p>")
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public AdminEmailTemplatesController(ApplicationDbContext db, ICurrentUserService currentUser) { _db = db; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var saved = await _db.EmailTemplates.AsNoTracking().ToDictionaryAsync(item => item.TemplateKey);
        return Ok(Defaults.Select(item => saved.TryGetValue(item.Key, out var value)
            ? value
            : new EmailTemplate { TemplateKey = item.Key, Subject = item.Value.Subject, HtmlBody = item.Value.Html, IsActive = true }));
    }

    [HttpPut("{templateKey}")]
    public async Task<IActionResult> Save(string templateKey, [FromBody] SaveEmailTemplateRequest request)
    {
        if (!Defaults.ContainsKey(templateKey)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.HtmlBody)) return BadRequest("Konu ve içerik zorunludur.");
        var template = await _db.EmailTemplates.SingleOrDefaultAsync(item => item.TemplateKey == templateKey);
        if (template == null) { template = new EmailTemplate { TemplateKey = templateKey }; _db.EmailTemplates.Add(template); }
        template.Subject = request.Subject.Trim();
        template.HtmlBody = new HtmlSanitizer().Sanitize(request.HtmlBody);
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync();
        return Ok(template);
    }

    public sealed class SaveEmailTemplateRequest { public string Subject { get; set; } = string.Empty; public string HtmlBody { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
}
