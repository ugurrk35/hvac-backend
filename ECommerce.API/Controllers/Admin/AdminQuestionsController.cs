using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Repository.Data;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Ürün sorularının yönetimi ve moderasyonu (listeleme, onay, silme).
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminQuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminQuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.ProductQuestions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => !q.IsApproved);
                }
                else if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.IsApproved);
                }
            }

            var items = await query
                .OrderByDescending(q => q.CreatedDate)
                .Select(q => new
                {
                    id = q.Id,
                    productId = q.ProductId,
                    question = q.QuestionText,
                    answer = q.AnswerText,
                    isApproved = q.IsApproved,
                    createdAt = q.CreatedDate,
                    authorName = q.AuthorName
                })
                .ToListAsync();

            return Ok(DataResponse<object>.CreateSuccess(items));
        }

        [HttpPut("approve/{id:int}")]
        public async Task<IActionResult> Approve(int id)
        {
            var item = await _context.ProductQuestions.FindAsync(id);
            if (item == null) return NotFound(BaseResponse.CreateFailure("Soru bulunamadı"));
            item.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Soru onaylandı"));
        }

        [HttpPut("answer/{id:int}")]
        public async Task<IActionResult> Answer(int id, [FromBody] string answer)
        {
            var item = await _context.ProductQuestions.FindAsync(id);
            if (item == null) return NotFound(BaseResponse.CreateFailure("Soru bulunamadı"));
            item.AnswerText = answer;
            item.IsApproved = true;
            await _context.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Cevap kaydedildi"));
        }
    }
}
