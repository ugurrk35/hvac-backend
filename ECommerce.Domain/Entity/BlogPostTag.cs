using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class BlogPostTag : AuditableEntity
    {
        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }
        public int BlogTagId { get; set; }
        public BlogTag BlogTag { get; set; }
    }

}
