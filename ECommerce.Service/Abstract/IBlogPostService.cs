using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.BlogDtos;
using ECommerce.Service.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IBlogPostService : IService<BlogPost>
    {
        Task<BlogPostDetailDto> GetPostDetailAsync(int postId);
        Task<BlogPostEditDto> GetPostForEditAsync(int postId);
        Task<BlogPost> CreatePostAsync(BlogPostCreateDto dto);
        Task<BlogPost> UpdatePostAsync(int id, BlogPostCreateDto dto);
        Task<BlogPost> GetPostBySlugAsync(string slug);
        Task<IReadOnlyList<BlogPost>> GetPublishedPostsAsync();
        Task PublishPostAsync(int id, DateTime? publishDate = null);
        Task UnpublishPostAsync(int id);
        Task<IReadOnlyList<BlogPost>> SearchPostsAsync(string query);
        Task<PagedResponse<BlogPostListDto>> GetPagedPostsAsync(int pageNumber, int pageSize, int? categoryId = null, int? tagId = null);
    }
}
