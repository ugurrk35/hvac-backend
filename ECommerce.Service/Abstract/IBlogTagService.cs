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
    public interface IBlogTagService : IService<BlogTag>
    {
        Task<BlogTag> CreateTagAsync(BlogTagCreateDto dto);
        Task<BlogTag> UpdateTagAsync(int id, BlogTagCreateDto dto);
        Task DeleteTagAsync(int id);
        Task<BlogTag> GetTagWithPostsAsync(int tagId);
    }
}
