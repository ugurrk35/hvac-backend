using ECommerce.API.Dtos.Blog;
using ECommerce.API.Helper;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Blog yorumlarıyla ilgili herkese açık ve yönetici işlemlerini sağlar.
    /// - Anonim kullanıcılar yorum ekleyebilir (honeypot ve hız limiti uygulanır).
    /// - Yöneticiler bekleyen yorumları listeleyebilir, onaylayabilir veya reddedebilir.
    /// Güvenlik: IP ve ülke bazlı engelleme, Redis ile hız limiti, honeypot alanı.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BlogCommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IConfiguration _config;

        /// <summary>
        /// Denetleyiciyi başlatır.
        /// </summary>
        /// <param name="context">Veritabanı bağlamı</param>
        /// <param name="redis">Redis bağlantısı (hız limiti sayaçları için)</param>
        /// <param name="config">Yapılandırma (engelli IP/ülkeler ve eşik değerleri)</param>
        public BlogCommentsController(ApplicationDbContext context, IConnectionMultiplexer redis, IConfiguration config)
        {
            _context = context;
            _redis = redis;
            _config = config;
        }

        [HttpPost]
        /// <summary>
        /// Belirtilen blog gönderisine anonim yorum ekler.
        /// Honeypot (Website alanı), IP/ülke kontrolü ve Redis tabanlı hız sınırı uygulanır.
        /// Onay mekanizması nedeniyle yorum başlangıçta yayına alınmaz.
        /// </summary>
        /// <param name="dto">Yorum verisi (BlogId, Name, Email, Comment, Website-honeypot)</param>
        /// <returns>Oluşturulan yorum kimliği (veya 0), bilgilendirici mesaj</returns>
        public async Task<IActionResult> AddComment([FromBody] AddBlogCommentDto dto)
        {
            if (dto.BlogId <= 0 || string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest(BaseResponse.CreateFailure("Invalid data"));

            // Honeypot: silently accept but ignore
            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                return Ok(DataResponse<int>.CreateSuccess(0, "Queued for moderation"));
            }

            // Blocked IPs / Countries via configuration
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var blockedIps = (_config["Security:BlockedIps"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (blockedIps.Contains(ip))
                return StatusCode(403, BaseResponse.CreateFailure("Access denied"));

            // Country via CDN headers (optional)
            var country = HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault()
                          ?? HttpContext.Request.Headers["X-Country-Code"].FirstOrDefault()
                          ?? HttpContext.Request.Headers["X-AppEngine-Country"].FirstOrDefault();
            var blockedCountries = (_config["Security:BlockedCountries"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(country) && blockedCountries.Contains(country.ToUpperInvariant()))
                return StatusCode(403, BaseResponse.CreateFailure("Access denied"));

            // Basic server-side rate limiting by IP using Redis
            try
            {
                var db = _redis.GetDatabase();
                var now = DateTimeOffset.UtcNow;

                // Keys
                var key1m = RedisCacheKeys.BlogCommentRateLimit(_config, $"ip:{ip}:1m");
                var key10m = RedisCacheKeys.BlogCommentRateLimit(_config, $"ip:{ip}:10m");
                var keyBlog10m = RedisCacheKeys.BlogCommentRateLimit(_config, $"blog:{dto.BlogId}:ip:{ip}:10m");

                // Increment counters
                var c1m = await db.StringIncrementAsync(key1m);
                if (c1m == 1) await db.KeyExpireAsync(key1m, TimeSpan.FromMinutes(1));
                var c10m = await db.StringIncrementAsync(key10m);
                if (c10m == 1) await db.KeyExpireAsync(key10m, TimeSpan.FromMinutes(10));
                var cBlog10m = await db.StringIncrementAsync(keyBlog10m);
                if (cBlog10m == 1) await db.KeyExpireAsync(keyBlog10m, TimeSpan.FromMinutes(10));

                // Thresholds from config with safe fallbacks
                int.TryParse(_config["Comments:RateLimits:PerMinute"], out var perMinute);
                int.TryParse(_config["Comments:RateLimits:Per10Minutes"], out var per10);
                int.TryParse(_config["Comments:RateLimits:PerPost10Minutes"], out var perPost10);
                perMinute = perMinute > 0 ? perMinute : 3;
                per10 = per10 > 0 ? per10 : 10;
                perPost10 = perPost10 > 0 ? perPost10 : 5;

                if (c1m > perMinute || c10m > per10 || cBlog10m > perPost10)
                {
                    return StatusCode(429, BaseResponse.CreateFailure("Çok fazla deneme. Lütfen daha sonra tekrar deneyin."));
                }
            }
            catch { /* ignore redis issues */ }

            var exists = await _context.BlogPosts.AnyAsync(b => b.Id == dto.BlogId);
            if (!exists) return NotFound(BaseResponse.CreateFailure("Blog not found"));

            var comment = new BlogPostComment
            {
                BlogPostId = dto.BlogId,
                AuthorName = string.IsNullOrWhiteSpace(dto.Name) ? "Anonim" : dto.Name!,
                AuthorEmail = dto.Email ?? string.Empty,
                Content = dto.Comment,
                IsApproved = false
            };
            _context.BlogPostComments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(DataResponse<int>.CreateSuccess(comment.Id, "Queued for moderation"));
        }

        /// <summary>
        /// (Yönetici) Onay bekleyen tüm yorumları getirir.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingComments()
        {
            var items = await _context.BlogPostComments
                .Where(c => !c.IsApproved && !c.IsDeleted)
                .OrderByDescending(c => c.CommentDate)
                .Select(c => new
                {
                    id = c.Id,
                    blogId = c.BlogPostId,
                    name = c.AuthorName,
                    email = c.AuthorEmail,
                    comment = c.Content,
                    createdAt = c.CommentDate
                })
                .ToListAsync();

            return Ok(DataResponse<object>.CreateSuccess(items));
        }

        /// <summary>
        /// (Yönetici) Yorumları durum, arama ve sayfalama kriterlerine göre listeler.
        /// </summary>
        /// <param name="status">pending | approved | rejected | all</param>
        /// <param name="pageNumber">Sayfa numarası (1…)</param>
        /// <param name="pageSize">Sayfa boyutu</param>
        /// <param name="q">İsteğe bağlı arama terimi (ad, e-posta, içerik, id)</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string status = "pending", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
        {
            var query = _context.BlogPostComments.AsQueryable().Where(c => !c.IsDeleted);

            switch ((status ?? "pending").ToLower())
            {
                case "approved":
                    query = query.Where(c => c.IsApproved && c.IsActive);
                    break;
                case "rejected":
                    query = query.Where(c => !c.IsActive);
                    break;
                case "all":
                    // no extra filter
                    break;
                default:
                    query = query.Where(c => !c.IsApproved);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    (c.AuthorName != null && c.AuthorName.ToLower().Contains(term)) ||
                    (c.AuthorEmail != null && c.AuthorEmail.ToLower().Contains(term)) ||
                    (c.Content != null && c.Content.ToLower().Contains(term)) ||
                    EF.Functions.ILike(c.BlogPostId.ToString(), $"%{term}%") ||
                    EF.Functions.ILike(c.Id.ToString(), $"%{term}%"));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CommentDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    id = c.Id,
                    blogId = c.BlogPostId,
                    name = c.AuthorName,
                    email = c.AuthorEmail,
                    comment = c.Content,
                    createdAt = c.CommentDate
                })
                .ToListAsync();

            var response = new PagedResponse<object>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Success = true,
                Message = "Yorumlar getirildi"
            };

            return Ok(response);
        }

        /// <summary>
        /// (Yönetici) Yorumu onaylar ve yayına alır (IsApproved=true, IsActive=true).
        /// </summary>
        /// <param name="id">Yorum kimliği</param>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveComment(int id)
        {
            var item = await _context.BlogPostComments.FindAsync(id);
            if (item == null) return NotFound(BaseResponse.CreateFailure("Not found"));
            item.IsApproved = true;
            item.IsActive = true; // yayına al
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Approved"));
        }

        /// <summary>
        /// (Yönetici) Yorumu yayından kaldırır (IsActive=false). Kalıcı silme yapılmaz.
        /// </summary>
        /// <param name="id">Yorum kimliği</param>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> RejectComment(int id)
        {
            var item = await _context.BlogPostComments.FindAsync(id);
            if (item == null) return NotFound(BaseResponse.CreateFailure("Not found"));
            item.IsActive = false; // yayından kaldır
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Rejected"));
        }
    }
}
