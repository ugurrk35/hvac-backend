using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Domain route (slug -> varlık) kayıtlarının listelenmesi ve yeniden oluşturulması.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/routes")]
    public class AdminRoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public AdminRoutesController(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? q = null)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var query = _db.DomainRoutes.AsNoTracking().OrderBy(r => r.Slug).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(r => r.Slug.Contains(q));
            }
            var total = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { items, pageNumber, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
        }

        [HttpPost("rebuild")] 
        public async Task<IActionResult> Rebuild()
        {
            // Build routes for products and categories
            var dict = new Dictionary<string, DomainRoute>(StringComparer.OrdinalIgnoreCase);
            var products = await _db.Products.AsNoTracking().Where(p => p.IsPublished && !p.IsDeleted).Select(p => new { p.Id, p.Slug }).ToListAsync();
            foreach (var p in products)
            {
                if (string.IsNullOrWhiteSpace(p.Slug)) continue;
                if (!dict.ContainsKey(p.Slug))
                    dict[p.Slug] = new DomainRoute { Slug = p.Slug, EntityType = "product", EntityId = p.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
            }
            var categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive && !c.IsDeleted).Select(c => new { c.Id, c.Slug }).ToListAsync();
            foreach (var c in categories)
            {
                if (string.IsNullOrWhiteSpace(c.Slug)) continue;
                if (!dict.ContainsKey(c.Slug))
                    dict[c.Slug] = new DomainRoute { Slug = c.Slug, EntityType = "category", EntityId = c.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
            }

            // Upsert: remove existing then re-add
            var all = await _db.DomainRoutes.ToListAsync();
            _db.DomainRoutes.RemoveRange(all);
            await _db.SaveChangesAsync();
            _db.DomainRoutes.AddRange(dict.Values);
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess($"{dict.Count} route oluşturuldu"));
        }

        [HttpGet("redirects")]
        public async Task<IActionResult> ListRedirects() => Ok(await _db.RedirectRules.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.SourcePath).ToListAsync());

        [HttpPost("redirects")]
        public async Task<IActionResult> SaveRedirect([FromBody] RedirectRule request)
        {
            var source = Normalize(request.SourcePath); var target = Normalize(request.TargetPath);
            if (source == "/" || source == target || !target.StartsWith('/')) return BadRequest("Kaynak ve hedef farklı, geçerli site içi yollar olmalıdır.");
            if (await _db.RedirectRules.AnyAsync(x => x.Id != request.Id && x.SourcePath == source && !x.IsDeleted)) return Conflict("Bu kaynak yol zaten tanımlı.");
            if (await _db.RedirectRules.AnyAsync(x => x.SourcePath == target && x.TargetPath == source && !x.IsDeleted)) return BadRequest("İki yönlü yönlendirme döngüsü oluşturulamaz.");
            var item = request.Id == 0 ? new RedirectRule { IsActive = true } : await _db.RedirectRules.FindAsync(request.Id); if (item == null) return NotFound(); if (item.Id == 0) _db.RedirectRules.Add(item);
            item.SourcePath = source; item.TargetPath = target; item.StatusCode = request.StatusCode == 302 ? 302 : 301; item.IsActive = request.IsActive; item.LastModifiedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); return Ok(item);
        }
        [HttpDelete("redirects/{id:int}")]
        public async Task<IActionResult> DeleteRedirect(int id) { var item = await _db.RedirectRules.FindAsync(id); if (item == null) return NotFound(); item.IsDeleted = true; await _db.SaveChangesAsync(); return NoContent(); }
        private static string Normalize(string? path) { var value = (path ?? string.Empty).Trim(); if (!value.StartsWith('/')) value = "/" + value; return value.Length > 1 ? value.TrimEnd('/') : value; }
    }
}
