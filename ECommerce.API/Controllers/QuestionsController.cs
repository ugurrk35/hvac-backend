using ECommerce.API.Dtos.Products;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün sorularını yönetir.
    /// - Onaylı soruları listeler
    /// - Giriş yapan kullanıcıların yeni soru oluşturmasını sağlar (moderasyonlu)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Belirtilen ürün için onaylı soruları döndürür.
        /// </summary>
        /// <param name="productId">Ürün kimliği</param>
        [HttpGet("{productId:int}")]
        [HttpGet("/ap/questons/{productId:int}")]
        public async Task<IActionResult> GetApproved(int productId)
        {
            var items = await _context.ProductQuestions
                .Where(q => q.ProductId == productId && q.IsApproved)
                .OrderByDescending(q => q.CreatedDate)
                .Select(q => new
                {
                    id = q.Id,
                    question = q.QuestionText,
                    answer = q.AnswerText,
                    createdAt = q.CreatedDate,
                    authorName = q.AuthorName
                })
                .ToListAsync();

            return Ok(DataResponse<object>.CreateSuccess(items));
        }

        /// <summary>
        /// Giriş yapmış kullanıcıdan ürün için yeni soru alır ve moderasyona gönderir.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQuestionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse.CreateFailure("Geçersiz veri"));

            var exists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted);
            if (!exists) return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));

            var item = new ProductQuestion
            {
                ProductId = dto.ProductId,
                AuthorName = User?.Identity?.Name ?? "Kullanıcı",
                QuestionText = dto.Question,
                IsApproved = false
            };
            _context.ProductQuestions.Add(item);
            await _context.SaveChangesAsync();

            return Ok(DataResponse<int>.CreateSuccess(item.Id, "Soru alındı, onay bekliyor"));
        }
    }
}
