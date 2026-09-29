using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using ECommerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// İletişim (bize yazın) mesajlarını karşılar ve kaydeder.
    /// Honeypot ve IP/ülke bazlı kontroller ile temel spam koruması içerir.
    /// İsteğe bağlı olarak Slack webhook bildirimi gönderebilir.
    /// </summary>
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ContactController> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;
        private readonly ITurnstileValidator _turnstileValidator;
        private readonly IEmailService _emailService;

        /// <summary>
        /// Denetleyiciyi başlatır.
        /// </summary>
        public ContactController(ApplicationDbContext db, ILogger<ContactController> logger, IDistributedCache cache, IConfiguration config, ITurnstileValidator turnstileValidator, IEmailService emailService)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
            _config = config;
            _turnstileValidator = turnstileValidator;
            _emailService = emailService;
        }

        public class CreateContactMessageDto
        {
            /// <summary>Gönderenin adı</summary>
            public string Name { get; set; }
            /// <summary>Gönderenin e-posta adresi</summary>
            public string Email { get; set; }
            /// <summary>Mesaj konusu (opsiyonel)</summary>
            public string? Subject { get; set; }
            /// <summary>Mesaj içeriği</summary>
            public string Message { get; set; }
            // Honeypot (should be empty)
            public string? Website { get; set; }
            public string? TurnstileToken { get; set; }
        }

        /// <summary>
        /// İletişim formundan gelen mesajı kaydeder.
        /// Honeypot alanını, IP/ülke engellemesini ve 10 dakikalık hız limitini uygular.
        /// </summary>
        /// <param name="dto">İletişim mesajı</param>
        /// <returns>Kaydedilen mesaj veya hata</returns>
        [HttpPost("api/contact")]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateContactMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(DataResponse<string>.CreateFailure("Ad, e-posta ve mesaj zorunludur"));

            // Honeypot check
            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                return BadRequest(DataResponse<string>.CreateFailure("Geçersiz istek"));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var turnstileResult = await _turnstileValidator.ValidateAsync(dto.TurnstileToken, ip, "contact", HttpContext.RequestAborted);
            if (!turnstileResult.Success)
            {
                return BadRequest(DataResponse<string>.CreateFailure("Güvenlik doğrulaması başarısız. Lütfen tekrar deneyin."));
            }

            // Blocked IPs / Countries via configuration
            var blockedIps = (_config["Security:BlockedIps"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (blockedIps.Contains(ip))
                return StatusCode(403, DataResponse<string>.CreateFailure("Erişim engellendi"));

            var country = HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault()
                          ?? HttpContext.Request.Headers["X-Country-Code"].FirstOrDefault()
                          ?? HttpContext.Request.Headers["X-AppEngine-Country"].FirstOrDefault();
            var blockedCountries = (_config["Security:BlockedCountries"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(country) && blockedCountries.Contains(country.ToUpperInvariant()))
                return StatusCode(403, DataResponse<string>.CreateFailure("Erişim engellendi"));

            // Basic rate limiting per IP (5 req / 10 min)
            try
            {
                var key = $"contact:rate:{ip}";
                var current = await _cache.GetStringAsync(key);
                var count = 0;
                _ = int.TryParse(current, out count);
                int.TryParse(_config["Contact:RateLimitPer10Minutes"], out var maxPer10);
                maxPer10 = maxPer10 > 0 ? maxPer10 : 5;
                if (count >= maxPer10)
                {
                    return StatusCode(429, DataResponse<string>.CreateFailure("Çok fazla istek - lütfen daha sonra tekrar deneyin"));
                }
                await _cache.SetStringAsync(key, (count + 1).ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }
            catch { }

            try
            {
                var item = new ContactMessage
                {
                    Name = dto.Name.Trim(),
                    Email = dto.Email.Trim(),
                    Subject = dto.Subject?.Trim(),
                    Message = dto.Message.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false
                };
                _db.ContactMessages.Add(item);
                await _db.SaveChangesAsync();

                var notificationRecipient = _config["Contact:NotificationEmail"] ?? _config["Email:FromAddress"];
                if (!string.IsNullOrWhiteSpace(notificationRecipient))
                {
                    var notificationSent = await _emailService.SendContactNotificationAsync(item, notificationRecipient, HttpContext.RequestAborted);
                    if (!notificationSent)
                        _logger.LogWarning("İletişim mesajı e-posta bildirimi gönderilemedi. Mesaj Id: {ContactMessageId}", item.Id);
                }

                // Optional Slack notification
                try
                {
                    var webhook = _config["Notifications:SlackWebhook"];
                    if (!string.IsNullOrWhiteSpace(webhook))
                    {
                        using var http = new HttpClient();
                        var payload = new
                        {
                            text = $":email: Yeni iletişim mesajı\n*Ad:* {item.Name}\n*E-posta:* {item.Email}\n*Konu:* {item.Subject}\n*Mesaj:* {item.Message}"
                        };
                        var json = System.Text.Json.JsonSerializer.Serialize(payload);
                        var resp = await http.PostAsync(webhook, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                        _ = resp.StatusCode; // ignore result
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Slack notification failed");
                }

                return Ok(DataResponse<ContactMessage>.CreateSuccess(item, "Mesajınız alındı"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact message save failed");
                return StatusCode(500, DataResponse<string>.CreateFailure("Mesaj kaydedilemedi"));
            }
        }
    }
}
