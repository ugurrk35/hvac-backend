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
    /// (Admin) İletişim mesajlarının listelenmesi, görüntülenmesi, çözüm durumunun güncellenmesi ve silinmesi işlemleri.
    /// </summary>
    [ApiController]
    [Route("api/admin/contact-messages")]
    [Authorize(Roles = "Admin")]
    public class AdminContactController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public AdminContactController(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isResolved = null)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var q = _db.ContactMessages.AsNoTracking().OrderByDescending(x => x.CreatedAt).AsQueryable();
            if (isResolved.HasValue) q = q.Where(x => x.IsResolved == isResolved.Value);
            var total = await q.CountAsync();
            var items = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new { items, pageNumber, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _db.ContactMessages.FindAsync(id);
            if (item == null) return NotFound(DataResponse<string>.CreateFailure("Bulunamadı"));
            return Ok(DataResponse<ContactMessage>.CreateSuccess(item));
        }

        public class UpdateContactMessageDto
        {
            public bool? IsResolved { get; set; }
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContactMessageDto dto)
        {
            var item = await _db.ContactMessages.FindAsync(id);
            if (item == null) return NotFound(DataResponse<string>.CreateFailure("Bulunamadı"));
            if (dto.IsResolved.HasValue)
            {
                item.IsResolved = dto.IsResolved.Value;
                item.ResolvedAt = dto.IsResolved.Value ? DateTime.UtcNow : null;
                item.ResolvedBy = dto.IsResolved.Value ? _currentUser.UserId : null;
            }
            await _db.SaveChangesAsync();
            return Ok(DataResponse<ContactMessage>.CreateSuccess(item));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ContactMessages.FindAsync(id);
            if (item == null) return NotFound(DataResponse<string>.CreateFailure("Bulunamadı"));
            _db.ContactMessages.Remove(item);
            await _db.SaveChangesAsync();
            return Ok(DataResponse<string>.CreateSuccess("Silindi"));
        }
    }
}
