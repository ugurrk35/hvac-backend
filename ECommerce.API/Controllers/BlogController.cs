using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.BlogDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Repository.Data;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Blog içerikleriyle ilgili yayın arayüzünü sağlar.
    /// Kategorilere göre listeleme, etiketler, sayfalama ve yazı detayını döndürür.
    /// Yazı detayında onaylı/aktif yorumlar, görseller ve etiketler yer alır.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly IBlogPostService _blogPostService;
        private readonly IBlogCategoryService _blogCategoryService;
        private readonly IBlogTagService _blogTagService;
        private readonly IBlogPostCommentService _blogPostCommentService;
        private readonly ApplicationDbContext _db;


        public BlogController(
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
     

        //[HttpGet("categories/{slug}/posts")]
        //public async Task<IActionResult> GetPostsByCategorySlug(string slug)
        //{
        //    var category = await _blogCategoryService.GetCategoryBySlugAsync(slug);
        //    if (category == null)
        //        return NotFound(DataResponse<BlogCategory>.CreateFailure("Kategori bulunamadı"));

        //    var posts = await _blogPostService.Query()
        //        .Where(p => p.BlogCategoryId == category.Id && p.IsPublished && p.IsActive)
        //        .Include(p => p.BlogPostTags).ThenInclude(t => t.BlogTag)
        //        .ToListAsync();

        //    return Ok(DataResponse<IReadOnlyList<BlogPost>>.CreateSuccess(posts));
        //}

        //[HttpGet("tags/{slug}/posts")]
        //public async Task<IActionResult> GetPostsByTagSlug(string slug)
        //{
        //    var tag = await _blogTagService.Query().FirstOrDefaultAsync(t => t.Slug == slug);
        //    if (tag == null)
        //        return NotFound(DataResponse<BlogTag>.CreateFailure("Tag bulunamadı"));

        //    var posts = await _blogPostService.Query()
        //        .Where(p => p.BlogPostTags.Any(t => t.BlogTagId == tag.Id) && p.IsPublished && p.IsActive)
        //        .Include(p => p.BlogCategory)
        //        .ToListAsync();

        //    return Ok(DataResponse<IReadOnlyList<BlogPost>>.CreateSuccess(posts));
        //}

        //// -------------------- BlogPost --------------------

        [HttpGet("posts/paged")]
        public async Task<IActionResult> GetPagedPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? categoryId = null, [FromQuery] int? tagId = null)
        {
            var response = await _blogPostService.GetPagedPostsAsync(pageNumber, pageSize, categoryId, tagId);
            return Ok(response);
        }

        [HttpGet("posts/{slug}")]
        public async Task<IActionResult> GetPostBySlug(string slug)
        {
            var post = await _blogPostService.GetPostBySlugAsync(slug);
            if (post == null)
                return NotFound(DataResponse<BlogPostDetailDto>.CreateFailure("Post bulunamadı"));

            var dto = new BlogPostDetailDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = post.Content,
                PublishDate = post.PublishDate,
                UpdatedAt = post.LastModifiedAt,
                IsPublished = post.IsPublished,
                IsFeatured = post.IsFeatured,
                Views = post.Views,
                BlogCategoryId = post.BlogCategoryId,
                BlogCategoryName = post.BlogCategory?.Name,
                Images = post.BlogPostImages?.Select(i => i.Image.Url).ToList() ?? new List<string>(),
                Tags = post.BlogPostTags?.Select(t => t.BlogTag.Name).ToList() ?? new List<string>(),
                Comments = post.Comments?.Where(c => c.IsApproved && c.IsActive).Select(c => new BlogCommentDto
                {
                    Id = c.Id,
                    Name = c.AuthorName,
                    Content = c.Content,
                    CreatedDate = c.CreatedAt
                }).ToList() ?? new List<BlogCommentDto>(),
                MetaTitle = post.MetaTitle,
                MetaDescription = post.MetaDescription,
                MetaKeywords = post.MetaKeywords,
                OgTitle = post.OgTitle,
                OgDescription = post.OgDescription,
                OgImageUrl = post.OgImageUrl,
                CanonicalUrl = post.CanonicalUrl,
                SchemaJson = post.SchemaJson
            };

            return Ok(DataResponse<BlogPostDetailDto>.CreateSuccess(dto));
        }

        [HttpGet("posts/{slug}/products")]
        public async Task<IActionResult> GetLinkedProducts(string slug)
        {
            var products = await _db.BlogPostProducts.AsNoTracking().Where(x => x.BlogPost.Slug == slug && x.BlogPost.IsPublished && x.Product.IsPublished && !x.Product.IsDeleted).OrderBy(x => x.SortOrder).Select(x => new { id = x.Product.Id, name = x.Product.Name, slug = x.Product.Slug, price = x.Product.DiscountPrice ?? x.Product.BasePrice }).ToListAsync();
            return Ok(DataResponse<object>.CreateSuccess(products));
        }

        //[HttpGet("posts/{postId}/comments/paged")]
        //public async Task<IActionResult> GetPagedApprovedComments(int postId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        //{
        //    var query = _blogPostCommentService.Query()
        //        .Where(c => c.BlogPostId == postId && c.IsApproved && c.IsActive);

        //    var totalCount = await query.CountAsync();
        //    var comments = await query
        //        .OrderBy(c => c.CommentDate)
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    var response = new PagedResponse<BlogPostComment>
        //    {
        //        Items = comments,
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        TotalCount = totalCount,
        //        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        //        Success = true,
        //        Message = "Yorumlar başarıyla getirildi"
        //    };

        //    return Ok(response);
        //}

       


        //[HttpGet("posts/{slug}")]
        //public async Task<IActionResult> GetPostBySlug(string slug)
        //{
        //    var post = await _blogPostService.GetPostBySlugAsync(slug);
        //    if (post == null)
        //        return NotFound(DataResponse<BlogPost>.CreateFailure("Post bulunamadı"));
        //    return Ok(DataResponse<BlogPost>.CreateSuccess(post));
        //}

        //[HttpGet("posts")]
        //public async Task<IActionResult> GetPublishedPosts()
        //{
        //    var posts = await _blogPostService.GetPublishedPostsAsync();
        //    return Ok(DataResponse<IReadOnlyList<BlogPost>>.CreateSuccess(posts));
        //}

        //[HttpPost("posts/{id}/publish")]
        //public async Task<IActionResult> PublishPost(int id)
        //{
        //    await _blogPostService.PublishPostAsync(id);
        //    return Ok(BaseResponse.CreateSuccess("Post yayınlandı"));
        //}

        //[HttpPost("posts/{id}/unpublish")]
        //public async Task<IActionResult> UnpublishPost(int id)
        //{
        //    await _blogPostService.UnpublishPostAsync(id);
        //    return Ok(BaseResponse.CreateSuccess("Post yayından kaldırıldı"));
        //}

        //[HttpGet("posts/search")]
        //public async Task<IActionResult> SearchPosts([FromQuery] string query)
        //{
        //    var posts = await _blogPostService.SearchPostsAsync(query);
        //    return Ok(DataResponse<IReadOnlyList<BlogPost>>.CreateSuccess(posts));
        //}

        //// -------------------- BlogCategory --------------------

       

        

        //[HttpGet("categories/{slug}")]
        //public async Task<IActionResult> GetCategoryBySlug(string slug)
        //{
        //    var category = await _blogCategoryService.GetCategoryBySlugAsync(slug);
        //    if (category == null)
        //        return NotFound(DataResponse<BlogCategory>.CreateFailure("Kategori bulunamadı"));
        //    return Ok(DataResponse<BlogCategory>.CreateSuccess(category));
        //}

        //// -------------------- BlogTag --------------------

        //[HttpPost("tags")]
        //public async Task<IActionResult> CreateTag([FromBody] BlogTagCreateDto dto)
        //{
        //    var tag = await _blogTagService.CreateTagAsync(dto);
        //    return Ok(DataResponse<BlogTag>.CreateSuccess(tag));
        //}

        //[HttpPut("tags/{id}")]
        //public async Task<IActionResult> UpdateTag(int id, [FromBody] BlogTagCreateDto dto)
        //{
        //    var tag = await _blogTagService.UpdateTagAsync(id, dto);
        //    return Ok(DataResponse<BlogTag>.CreateSuccess(tag));
        //}

        //[HttpDelete("tags/{id}")]
        //public async Task<IActionResult> DeleteTag(int id)
        //{
        //    await _blogTagService.DeleteTagAsync(id);
        //    return Ok(BaseResponse.CreateSuccess("Tag silindi"));
        //}

        //[HttpGet("tags/{id}/posts")]
        //public async Task<IActionResult> GetTagWithPosts(int id)
        //{
        //    var tag = await _blogTagService.GetTagWithPostsAsync(id);
        //    if (tag == null)
        //        return NotFound(DataResponse<BlogTag>.CreateFailure("Tag bulunamadı"));
        //    return Ok(DataResponse<BlogTag>.CreateSuccess(tag));
        //}

        //// -------------------- BlogPostComment --------------------

        //[HttpPost("comments")]
        //public async Task<IActionResult> AddComment([FromBody] BlogPostCommentCreateDto dto)
        //{
        //    var comment = await _blogPostCommentService.AddCommentAsync(dto);
        //    return Ok(DataResponse<BlogPostComment>.CreateSuccess(comment, "Yorum eklendi"));
        //}

        //[HttpPost("comments/{id}/approve")]
        //public async Task<IActionResult> ApproveComment(int id)
        //{
        //    await _blogPostCommentService.ApproveCommentAsync(id);
        //    return Ok(BaseResponse.CreateSuccess("Yorum onaylandı"));
        //}

        //[HttpPost("comments/{id}/reject")]
        //public async Task<IActionResult> RejectComment(int id)
        //{
        //    await _blogPostCommentService.RejectCommentAsync(id);
        //    return Ok(BaseResponse.CreateSuccess("Yorum reddedildi"));
        //}

        //[HttpGet("posts/{postId}/comments")]
        //public async Task<IActionResult> GetApprovedComments(int postId)
        //{
        //    var comments = await _blogPostCommentService.GetApprovedCommentsAsync(postId);
        //    return Ok(DataResponse<IReadOnlyList<BlogPostComment>>.CreateSuccess(comments));
        //}
      
    }
}
