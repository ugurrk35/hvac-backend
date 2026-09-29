using System.Text.RegularExpressions;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// WhatsApp entegrasyonu için ayarların okunması/güncellenmesi ve istemci tarafı bağlantı üretimi için verilerin sunulması.
    /// </summary>
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(
            ApplicationDbContext db,
            IDistributedCache cache,
            ICurrentUserService currentUser,
            ILogger<WhatsAppController> logger)
        {
            _db = db;
            _cache = cache;
            _currentUser = currentUser;
            _logger = logger;
        }

        public class WhatsAppConfigDto
        {
            public bool IsEnabled { get; set; }
            public string? PhoneNumber { get; set; }
            public string? ProductTemplate { get; set; }
            public string? CartTemplate { get; set; }
            public string? CheckoutTemplate { get; set; }
        }

        public class UpdateWhatsAppRequest
        {
            public bool IsEnabled { get; set; }
            public string? PhoneNumber { get; set; }
            public string? ProductTemplate { get; set; }
            public string? CartTemplate { get; set; }
            public string? CheckoutTemplate { get; set; }
        }

        private static string? NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = Regex.Replace(input, "[^0-9]", "");
            if (string.IsNullOrEmpty(digits)) return null;
            return digits;
        }

        [HttpGet("api/admin/whatsapp/settings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WhatsAppSettings>> GetAdmin()
        {
            var s = await _db.WhatsAppSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
            s ??= new WhatsAppSettings { IsEnabled = false, PhoneNumber = null };
            return Ok(s);
        }

        [HttpPost("api/admin/whatsapp/settings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WhatsAppSettings>> SaveAdmin([FromBody] UpdateWhatsAppRequest req)
        {
            var s = await _db.WhatsAppSettings.FirstOrDefaultAsync();
            if (s == null)
            {
                s = new WhatsAppSettings();
                _db.WhatsAppSettings.Add(s);
            }
            s.IsEnabled = req.IsEnabled;
            s.PhoneNumber = NormalizePhone(req.PhoneNumber);
            s.ProductTemplate = string.IsNullOrWhiteSpace(req.ProductTemplate) ? s.ProductTemplate : req.ProductTemplate?.Trim();
            s.CartTemplate = string.IsNullOrWhiteSpace(req.CartTemplate) ? s.CartTemplate : req.CartTemplate?.Trim();
            s.CheckoutTemplate = string.IsNullOrWhiteSpace(req.CheckoutTemplate) ? s.CheckoutTemplate : req.CheckoutTemplate?.Trim();
            s.UpdatedAt = DateTime.UtcNow;
            s.UpdatedBy = _currentUser.UserId;
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync("site:whatsapp");
            return Ok(s);
        }

        [HttpGet("api/whatsapp")]
        [AllowAnonymous]
        public async Task<ActionResult<DataResponse<WhatsAppConfigDto>>> GetPublic()
        {
            try
            {
                var cached = await _cache.GetStringAsync("site:whatsapp");
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var c = System.Text.Json.JsonSerializer.Deserialize<DataResponse<WhatsAppConfigDto>>(cached);
                    if (c != null) return Ok(c);
                }

                var s = await _db.WhatsAppSettings.AsNoTracking().FirstOrDefaultAsync();
                var dto = new WhatsAppConfigDto
                {
                    IsEnabled = s?.IsEnabled ?? false,
                    PhoneNumber = NormalizePhone(s?.PhoneNumber),
                    ProductTemplate = s?.ProductTemplate ?? "Merhaba, {productName} ürünü hakkında bilgi almak istiyorum. Ürün linki: {productUrl}",
                    CartTemplate = s?.CartTemplate ?? "Merhaba, sepetim hakkında bilgi almak istiyorum. Sepet linki: {cartUrl}",
                    CheckoutTemplate = s?.CheckoutTemplate ?? "Merhaba, sipariş/ödeme hakkında destek rica ediyorum. Checkout linki: {checkoutUrl}"
                };
                var resp = DataResponse<WhatsAppConfigDto>.CreateSuccess(dto);
                var json = System.Text.Json.JsonSerializer.Serialize(resp);
                await _cache.SetStringAsync("site:whatsapp", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsApp config getirilemedi");
                return StatusCode(500, DataResponse<WhatsAppConfigDto>.CreateFailure("Ayarlar getirilemedi"));
            }
        }
    }
}
