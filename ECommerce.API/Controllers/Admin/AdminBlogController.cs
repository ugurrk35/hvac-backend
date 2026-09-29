using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.BlogDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using ECommerce.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Blog yönetimi: yazılar, kategoriler, etiketler ve yorum moderasyonu.
    /// Sayfalama ile listeleme, oluşturma/güncelleme ve detay uç noktalarını içerir.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminBlogController : ControllerBase
    {
        private readonly IBlogPostService _blogPostService;
        private readonly IBlogCategoryService _blogCategoryService;
        private readonly IBlogTagService _blogTagService;
        private readonly IBlogPostCommentService _blogPostCommentService;
        private readonly ApplicationDbContext _db;


        public AdminBlogController(
            IBlogPostService blogPostService,
            IBlogCategoryService blogCategoryService,
            IBlogTagService blogTagService,
            IBlogPostCommentService blogPostCommentService,
            ApplicationDbContext db)
        {
            _blogPostService = blogPostService;
            _blogCategoryService = blogCategoryService;
            _blogTagService = blogTagService;
            _blogPostCommentService = blogPostCommentService;
            _db = db;
        }
        /// <summary>
        /// Tüm aktif blog kategorilerini listeler.
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _blogCategoryService.GetAllActiveCategoriesAsync();
            return Ok(DataResponse<IReadOnlyList<BlogCategory>>.CreateSuccess(categories));
        }
        /// <summary>
        /// Blog yazılarını sayfalı şekilde listeler (kategori/etikete göre filtrelenebilir).
        /// </summary>
        [HttpGet("posts/paged")]
        public async Task<IActionResult> GetPagedPosts(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int? categoryId = null,
    [FromQuery] int? tagId = null)
        {
            var response = await _blogPostService.GetPagedPostsAsync(pageNumber, pageSize, categoryId, tagId);
            return Ok(response);
        }

        /// <summary>
        /// Blog yazısı detayını (görseller, etiketler, yorumlar) getirir.
        /// </summary>
        [HttpGet("blogposts/{id}")]
        public async Task<IActionResult> GetPostDetail(int id)
        {
            var post = await _blogPostService.GetPostDetailAsync(id);
            if (post == null)
                return NotFound(DataResponse<BlogPostDetailDto>.CreateFailure("Blog yazısı bulunamadı"));

            return Ok(DataResponse<BlogPostDetailDto>.CreateSuccess(post));
        }

        [HttpGet("posts/{id:int}/products")]
        public async Task<IActionResult> GetRelatedProducts(int id) => Ok(await _db.BlogPostProducts.AsNoTracking().Where(x => x.BlogPostId == id).OrderBy(x => x.SortOrder).Select(x => new { x.ProductId, x.SortOrder, x.Product.Name, x.Product.Slug }).ToListAsync());

        [HttpPut("posts/{id:int}/products")]
        public async Task<IActionResult> SetRelatedProducts(int id, [FromBody] List<int> productIds)
        {
            if (!await _db.BlogPosts.AnyAsync(x => x.Id == id && !x.IsDeleted)) return NotFound();
            var ids = productIds.Distinct().Take(12).ToList();
            var valid = await _db.Products.Where(x => ids.Contains(x.Id) && x.IsPublished && !x.IsDeleted).Select(x => x.Id).ToListAsync();
            var current = await _db.BlogPostProducts.Where(x => x.BlogPostId == id).ToListAsync(); _db.BlogPostProducts.RemoveRange(current);
            _db.BlogPostProducts.AddRange(valid.Select((productId, index) => new BlogPostProduct { BlogPostId = id, ProductId = productId, SortOrder = index }));
            await _db.SaveChangesAsync(); return NoContent();
        }

        /// <summary>
        /// Blog yazısını günceller.
        /// </summary>
        [HttpPut("posts/{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] BlogPostCreateDto dto)
        {
            var post = await _blogPostService.UpdatePostAsync(id, dto);
            var postDto = post.ToEditDto();

            return Ok(DataResponse<BlogPostEditDto>.CreateSuccess(postDto, "blog başarıyla güncellendi"));
        }

        /// <summary>
        /// Yeni blog yazısı oluşturur.
        /// </summary>
        [HttpPost("posts")]
        public async Task<IActionResult> CreatePost([FromBody] BlogPostCreateDto dto)
        {
            var post = await _blogPostService.CreatePostAsync(dto);
            return Ok(DataResponse<BlogPost>.CreateSuccess(post));
        }
        /// <summary>
        /// Yeni blog kategorisi oluşturur.
        /// </summary>
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] BlogCategoryCreateDto dto)
        {
            var category = await _blogCategoryService.CreateCategoryAsync(dto);
            return Ok(DataResponse<BlogCategory>.CreateSuccess(category));
        }

        /// <summary>
        /// Blog kategorisini günceller.
        /// </summary>
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] BlogCategoryCreateDto dto)
        {
            var category = await _blogCategoryService.UpdateCategoryAsync(id, dto);
            return Ok(DataResponse<BlogCategory>.CreateSuccess(category));
        }
        /// <summary>
        /// Kategori detayını id ile getirir.
        /// </summary>
        [HttpGet("categories/{id:int}")]
        public async Task<IActionResult> GetCategoryDetailById(int id)
        {
            var category = await _blogCategoryService.GetCategoryByIdDetailAsync(id);

            if (category == null)
                return NotFound(DataResponse<BlogCategoryDto>.CreateFailure("Kategori bulunamadı"));

            //var dto = _mapper.Map<BlogCategoryDto>(category);
            return Ok(DataResponse<BlogCategoryDto>.CreateSuccess(category));
        }
    }
}
