using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.BlogDtos
{
    public class BlogPostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Excerpt { get; set; }
        public DateTime PublishDate { get; set; }
        public int Views { get; set; }
        public string? ImageUrl { get; set; }

        public BlogCategoryListDto BlogCategory { get; set; }
    }
}
