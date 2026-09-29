using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.BlogDtos
{
    public class BlogPostCommentCreateDto
    {
        public int BlogPostId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public string Content { get; set; }
    }
}
