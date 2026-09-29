using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Repository.Data;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Ürün yorumlarının yönetimi ve moderasyonu (listeleme, onay, silme).
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.ProductReviews.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLowerInvariant())
                {
                    case "pending":
                        query = query.Where(r => !r.IsApproved);
                        break;
                    case "approved":
                        query = query.Where(r => r.IsApproved);
                        break;
                }
            }

            var items = await query
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => new
                {
                    id = r.Id,
                    productId = r.ProductId,
                    title = r.Title,
                    content = r.Content,
                    rating = r.Rating,
                    isApproved = r.IsApproved,
                    createdAt = r.ReviewDate,
                    authorName = r.ReviewerName
                })
                .ToListAsync();

            return Ok(DataResponse<object>.CreateSuccess(items));
        }

        [HttpPut("approve/{id:int}")]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null) return NotFound(BaseResponse.CreateFailure("Yorum bulunamadı"));
            review.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Yorum onaylandı"));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null) return NotFound(BaseResponse.CreateFailure("Yorum bulunamadı"));
            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Yorum silindi"));
        }
    }
}
