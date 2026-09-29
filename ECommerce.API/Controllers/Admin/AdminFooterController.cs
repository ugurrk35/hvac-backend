using System.Text.Json;
using ECommerce.Domain.Entity;
using ECommerce.API.Models;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using equals.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Footer yapılandırmasının okunması ve güncellenmesi.
    /// </summary>
    [Route("api/admin/footer")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminFooterController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IDistributedCache _cache;

        public AdminFooterController(ApplicationDbContext db, ICurrentUserService currentUser, IDistributedCache cache)
        {
            _db = db;
            _currentUser = currentUser;
            _cache = cache;
        }

        // DTOs moved to ECommerce.API.Models to avoid Swagger schema name collisions

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var s = await _db.FooterSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
            s ??= new FooterSettings { ConfigJson = null };
            return Ok(s);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] FooterConfigDto config)
        {
            // sanitize basics
            foreach (var c in config.columns)
            {
                c.title = (c.title ?? string.Empty).Trim();
                c.links = c.links
                    .OrderBy(l => l.sort)
                    .Select(l => { l.label = (l.label ?? string.Empty).Trim(); return l; })
                    .ToList();
            }
            config.columns = config.columns.OrderBy(c => c.sort).ToList();

            var settings = await _db.FooterSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new FooterSettings();
                _db.FooterSettings.Add(settings);
            }
            settings.ConfigJson = JsonSerializer.Serialize(config);
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = _currentUser.UserId;
            await _db.SaveChangesAsync();
            await _cache.RemoveAsync("footer:config");
            return Ok(settings);
        }
    }
}
