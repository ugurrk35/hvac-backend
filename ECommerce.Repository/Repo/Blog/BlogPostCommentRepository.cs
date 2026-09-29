using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo.Blog
{
    public class BlogPostCommentRepository : GenericRepository<BlogPostComment>, IBlogPostCommentRepository
    {
        public BlogPostCommentRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        /// <summary>
        /// Belirtilen yazıya ait, onaylı (IsApproved) ve aktif (IsActive) yorumları getirir.
        /// Silinmiş kayıtlar (IsDeleted) hariç tutulur; tarih sırasına göre (eskiden yeniye) döner.
        /// </summary>
        /// <param name="postId">Blog yazısı kimliği</param>
        /// <returns>Onaylı yorum listesi</returns>
        public async Task<IReadOnlyList<BlogPostComment>> GetApprovedCommentsByPostIdAsync(int postId)
        {
            return await _dbContext.BlogPostComments
                .Where(c => c.BlogPostId == postId && c.IsApproved && c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.CommentDate)
                .ToListAsync();
        }
    }
}
