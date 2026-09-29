using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.BlogDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IBlogPostCommentService : IService<BlogPostComment>
    {
        Task<BlogPostComment> AddCommentAsync(BlogPostCommentCreateDto dto);
        Task ApproveCommentAsync(int commentId);
        Task RejectCommentAsync(int commentId);
        Task<IReadOnlyList<BlogPostComment>> GetApprovedCommentsAsync(int postId);
    }
}
