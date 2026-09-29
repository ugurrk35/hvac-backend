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
    public class BlogTagService : Service<BlogTag>, IBlogTagService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogTagService(IBlogTagRepository repository, IUnitOfWork unitOfWork) : base(repository, unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BlogTag> CreateTagAsync(BlogTagCreateDto dto)
        {
            var tag = new BlogTag
            {
                Name = dto.Name,
                Slug = dto.Slug.ToLower()
            };

            var addedTag = await _repository.AddAsync(tag);
            await _unitOfWork.CompleteAsync();
            return addedTag;
        }

        public async Task<BlogTag> UpdateTagAsync(int id, BlogTagCreateDto dto)
        {
            var tag = await _repository.GetByIdAsync(id);
            if (tag == null) throw new Exception("Tag not found");

            tag.Name = dto.Name;
            tag.Slug = dto.Slug.ToLower();

            await _repository.UpdateAsync(tag);
            await _unitOfWork.CompleteAsync();
            return tag;
        }

        public async Task DeleteTagAsync(int id)
        {
            var tag = await _repository.GetByIdAsync(id);
            if (tag == null) throw new Exception("Tag not found");

            await _repository.DeleteAsync(tag);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<BlogTag> GetTagWithPostsAsync(int tagId)
        {
            return await ((IBlogTagRepository)_repository).GetTagWithPostsAsync(tagId);
        }
    }
}
