using ECommerce.API.Dtos.Products;
using ECommerce.API.Helper;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün yorumlarını yönetir.
    /// - Yayınlanmış (onaylı) yorumların listelenmesi (cache ile)
    /// - Giriş yapmış kullanıcıdan yorum oluşturma ve moderasyon kuyruğuna alma
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Belirtilen ürüne ait onaylı yorumları (fotoğraflarıyla birlikte) döndürür.
        /// </summary>
        /// <param name="productId">Ürün kimliği</param>
        [HttpGet("{productId:int}")]
        [HttpGet("/reviews/{productId:int}")]
        //[CacheResponse(72000)] // 20 saat cache
        public async Task<IActionResult> GetApprovedReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 1;
                if (pageSize > 50) pageSize = 50;

                var query = _context.ProductReviews
                    .Include(r => r.Photos)
                        .ThenInclude(p => p.Image)
                    .Where(r => r.ProductId == productId && r.IsApproved);

                var totalCount = await query.CountAsync();

                var items = await query
      .OrderByDescending(r => r.ReviewDate)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(r => new
      {
          title = r.Title,
          content = r.Content,
          rating = r.Rating,
          createdAt = r.ReviewDate,
          authorName = r.ReviewerName,
          images = r.Photos
              .Where(p => p.ImageId != null)             // FK üzerinden filtre
              .Select(p => new
              {
                  url = p.Image.Url,
                  alt = p.Image.AltText,
                  caption = p.Image.Caption
              })
              .ToList()
      })
      .ToListAsync();


                return Ok(DataResponse<object>.CreateSuccess(new
                {
                    items,
                    totalCount,
                    pageNumber = page,
                    pageSize
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, DataResponse<string>.CreateFailure("Yorumlar çekilirken hata oluştu", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Giriş yapmış kullanıcıdan yeni bir ürün yorumu alır ve moderasyona gönderir.
        /// </summary>
        [Authorize]
        [HttpPost]
        //[HttpPost("/ap/revews")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BaseResponse.CreateFailure("Geçersiz veri"));

            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted);
            if (!productExists)
                return NotFound(BaseResponse.CreateFailure("Kullanıcı Bulunamadı"));

            var reviewerName = User.FindFirst("sub")?.Value ?? "Kullanıcı";
            var reviewerEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? $"{reviewerName}@example.com";


            var review = new ProductReview
            {
                ProductId = dto.ProductId,
                ReviewerName = reviewerName,
                ReviewerEmail = reviewerEmail,

                Title = dto.Title ?? string.Empty,
                Content = dto.Content,
                Rating = dto.Rating,
                IsApproved = false
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

                        if (dto.ImageIds != null && dto.ImageIds.Count > 0)
            {
                foreach (var imgId in dto.ImageIds)
                {
                    _context.ProductReviewPhotos.Add(new ProductReviewPhoto
                    {
                        ProductReviewId = review.Id,
                        ImageId = imgId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(DataResponse<int>.CreateSuccess(review.Id, "Yorum alındı, onay bekliyor"));
        }
    }
}




