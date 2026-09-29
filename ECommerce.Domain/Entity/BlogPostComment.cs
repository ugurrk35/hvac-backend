using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class BlogPostComment : AuditableEntity
    {
        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }

        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; } // Opsiyonel
        public string Content { get; set; } // Yorum metni
        public bool IsApproved { get; set; } = false; // Moderasyon
        public DateTime CommentDate { get; set; } = DateTime.UtcNow;
    }
}
