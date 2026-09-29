using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class BlogPost : AuditableEntity
    {
        public string Title { get; set; }
        public string Slug { get; set; } // SEO URL
        public string Content { get; set; }
        public string Excerpt { get; set; } // Kısa özet

        public int BlogCategoryId { get; set; }
        public BlogCategory BlogCategory { get; set; }

        public DateTime PublishDate { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int Views { get; set; }

        public ICollection<BlogPostImage> BlogPostImages { get; set; } = new List<BlogPostImage>();
        public ICollection<BlogPostTag>? BlogPostTags { get; set; } = new List<BlogPostTag>();
        public ICollection<BlogPostComment>? Comments { get; set; } = new List<BlogPostComment>();
        public ICollection<BlogPostProduct> RelatedProducts { get; set; } = new List<BlogPostProduct>();

        // SEO Alanları
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImageUrl { get; set; }
        public string CanonicalUrl { get; set; }

        // Schema / Structured Data
        public string SchemaJson { get; set; }
    }
}
