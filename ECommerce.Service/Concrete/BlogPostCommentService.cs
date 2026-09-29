using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos.BlogDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class BlogPostCommentService : Service<BlogPostComment>, IBlogPostCommentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogPostCommentService(IBlogPostCommentRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Yeni bir blog yorumu oluşturur ve veritabanına ekler.
        /// Not: Yorumlar başlangıçta onaylı değildir; moderasyon gerektirir.
        /// </summary>
        public async Task<BlogPostComment> AddCommentAsync(BlogPostCommentCreateDto dto)
        {
            var comment = new BlogPostComment
            {
                BlogPostId = dto.BlogPostId,
                AuthorName = dto.AuthorName,
                AuthorEmail = dto.AuthorEmail,
                Content = dto.Content,
                IsApproved = false,
                CommentDate = DateTime.UtcNow
            };

            var addedComment = await _repository.AddAsync(comment);
            await _unitOfWork.CompleteAsync();
            return addedComment;
        }

        /// <summary>
        /// Yorumu onaylar ve aktif hale getirir (yayınlanır).
        /// </summary>
        public async Task ApproveCommentAsync(int commentId)
        {
            var comment = await _repository.GetByIdAsync(commentId);
            if (comment == null) throw new Exception("Comment not found");
            comment.IsApproved = true;
            comment.IsActive = true; // Onaylanan yorumları yayınla
            await _repository.UpdateAsync(comment);
            await _unitOfWork.CompleteAsync();
        }

        /// <summary>
        /// Yorumu pasif hale getirir (yayından kaldırır).
        /// </summary>
        public async Task RejectCommentAsync(int commentId)
        {
            var comment = await _repository.GetByIdAsync(commentId);
            if (comment == null) throw new Exception("Comment not found");
            comment.IsActive = false;
            await _repository.UpdateAsync(comment);
            await _unitOfWork.CompleteAsync();
        }

        /// <summary>
        /// Belirtilen yazıya ait onaylı ve aktif yorumları döndürür.
        /// </summary>
        public async Task<IReadOnlyList<BlogPostComment>> GetApprovedCommentsAsync(int postId)
        {
            return await ((IBlogPostCommentRepository)_repository).GetApprovedCommentsByPostIdAsync(postId);
        }
    }
}
