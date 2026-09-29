using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    // Blog Kategori
    public class BlogCategory : AuditableEntity
    {
        public string Name { get; set; }
        public string Slug { get; set; } // SEO URL
        public string Description { get; set; }

        // SEO Alanları
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string OgTitle { get; set; } // Sosyal paylaşım
        public string OgDescription { get; set; }
        public string OgImageUrl { get; set; }
        public string CanonicalUrl { get; set; }

        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    }
}
