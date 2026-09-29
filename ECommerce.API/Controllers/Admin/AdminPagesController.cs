using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using equals.Domain.Interfaces;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) CMS sayfa yönetimi: listeleme, oluşturma, güncelleme ve silme.
    /// </summary>
    [ApiController]
    [Route("api/admin/pages")]
    [Authorize(Roles = "Admin")]
    public class AdminPagesController : ControllerBase
    {
        private static readonly HtmlSanitizer ContentSanitizer = CreateContentSanitizer();
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public AdminPagesController(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _db.CmsPages.AsNoTracking().OrderByDescending(x => x.Id);
            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new
            {
                items,
                pageNumber,
                pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound(DataResponse<CmsPage>.CreateFailure("Sayfa bulunamadı"));
            return Ok(DataResponse<CmsPage>.CreateSuccess(page));
        }

        public class UpsertPageDto
        {
            public string Title { get; set; }
            public string Slug { get; set; }
            public string ContentHtml { get; set; }
            public bool IsPublished { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? CanonicalUrl { get; set; }
        }

        private static HtmlSanitizer CreateContentSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith([
                "a", "b", "blockquote", "br", "code", "details", "div", "em", "h1", "h2", "h3", "h4",
                "hr", "i", "li", "ol", "p", "pre", "span", "strong", "summary", "u", "ul"
            ]);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(["href", "target", "rel"]);
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
            return sanitizer;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertPageDto dto)
        {
            var exists = await _db.CmsPages.AnyAsync(x => x.Slug == dto.Slug);
            if (exists) return BadRequest(DataResponse<CmsPage>.CreateFailure("Slug zaten kullanılıyor"));
            var page = new CmsPage
            {
                Title = dto.Title.Trim(),
                Slug = dto.Slug.Trim(),
                ContentHtml = ContentSanitizer.Sanitize(dto.ContentHtml ?? string.Empty),
                IsPublished = dto.IsPublished,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                CanonicalUrl = dto.CanonicalUrl,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsActive = true,
                IsDeleted = false,
            };
            _db.CmsPages.Add(page);
            await _db.SaveChangesAsync();
            return Ok(DataResponse<CmsPage>.CreateSuccess(page));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertPageDto dto)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound(DataResponse<CmsPage>.CreateFailure("Sayfa bulunamadı"));
            if (!string.Equals(page.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var slugUsed = await _db.CmsPages.AnyAsync(x => x.Id != id && x.Slug == dto.Slug);
                if (slugUsed) return BadRequest(DataResponse<CmsPage>.CreateFailure("Slug zaten kullanılıyor"));
            }
            page.Title = dto.Title.Trim();
            page.Slug = dto.Slug.Trim();
            page.ContentHtml = ContentSanitizer.Sanitize(dto.ContentHtml ?? string.Empty);
            page.IsPublished = dto.IsPublished;
            page.MetaTitle = dto.MetaTitle;
            page.MetaDescription = dto.MetaDescription;
            page.CanonicalUrl = dto.CanonicalUrl;
            page.LastModifiedAt = DateTime.UtcNow;
            page.LastModifiedBy = _currentUser.UserId;
            await _db.SaveChangesAsync();
            return Ok(DataResponse<CmsPage>.CreateSuccess(page));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound(DataResponse<CmsPage>.CreateFailure("Sayfa bulunamadı"));
            _db.CmsPages.Remove(page);
            await _db.SaveChangesAsync();
            return Ok(DataResponse<string>.CreateSuccess("Silindi"));
        }
    }
}
