using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Statik sayfa içeriklerini (ör. hakkımızda, teslimat/kargo vb.) herkese açık olarak sunar.
    /// </summary>
    [ApiController]
    public class PagesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PagesController> _logger;

        public PagesController(ApplicationDbContext db, ILogger<PagesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class PublicPageDto
        {
            public string Title { get; set; }
            public string Slug { get; set; }
            public string ContentHtml { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? CanonicalUrl { get; set; }
        }

        [HttpGet("api/pages/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            try
            {
                var page = await _db.CmsPages.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished && !x.IsDeleted);
                if (page == null) return NotFound(DataResponse<PublicPageDto>.CreateFailure("Sayfa bulunamadı"));
                var dto = new PublicPageDto
                {
                    Title = page.Title,
                    Slug = page.Slug,
                    ContentHtml = page.ContentHtml,
                    MetaTitle = page.MetaTitle,
                    MetaDescription = page.MetaDescription,
                    CanonicalUrl = page.CanonicalUrl
                };
                return Ok(DataResponse<PublicPageDto>.CreateSuccess(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Page fetch error for slug {Slug}", slug);
                return StatusCode(500, DataResponse<PublicPageDto>.CreateFailure("Sayfa yüklenemedi"));
            }
        }
    }
}
