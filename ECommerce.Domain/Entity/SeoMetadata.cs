using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class SeoMetadata:AuditableEntity
    {
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }
        public bool NoIndex { get; set; }
        public bool NoFollow { get; set; }
        public string OpenGraphImageUrl { get; set; }
        public string TwitterCardImageUrl { get; set; }
    }
}
