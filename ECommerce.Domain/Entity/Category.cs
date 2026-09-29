using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
 
        public class Category:AuditableEntity
        {
            public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        // SEO Metadata
        public string MetaTitle { get; set; }                        // <title>
        public string MetaDescription { get; set; }                  // <meta name="description">
        public string MetaKeywords { get; set; }                     // <meta name="keywords">
        public string CanonicalUrl { get; set; }                     // <link rel="canonical">
        public string OgTitle { get; set; }                          // Open Graph: Facebook başlığı
        public string OgDescription { get; set; }                    // Open Graph: Açıklama
        public string OgImage { get; set; }                          // Open Graph: Görsel (URL)
        public string TwitterCardType { get; set; } = "summary_large_image"; // Twitter kart tipi
        public ICollection<Product>? products { get; set; }

    }
}
