using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IBlogPostRepository : IRepository<BlogPost>
    {
        Task<BlogPost> GetPostWithDetailsAsync(int postId);
        Task<IReadOnlyList<BlogPost>> GetPublishedPostsAsync();
        Task<(IReadOnlyList<BlogPost> Items, int TotalCount)> GetPagedPostsAsync(
   int pageNumber, int pageSize, int? categoryId = null, int? tagId = null);
    }
}
