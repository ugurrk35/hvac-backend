using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class BlogPostImage : AuditableEntity
    {
        public int BlogPostId { get; set; }
        [JsonIgnore]
        public BlogPost BlogPost { get; set; }

        public int ImageId { get; set; }
        public Image Image { get; set; }

        public int SortOrder { get; set; }
    }

}
