using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.BlogDtos
{
    public class BlogPostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Excerpt { get; set; }
        public string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int Views { get; set; }

        // Kategori
        public int BlogCategoryId { get; set; }
        public string BlogCategoryName { get; set; }

        // Görseller
        public List<string> Images { get; set; } = new();

        // Etiketler
        public List<string> Tags { get; set; } = new();

        // Yorumlar (sadece onaylılar)
        public List<BlogCommentDto> Comments { get; set; } = new();

        // SEO
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImageUrl { get; set; }
        public string CanonicalUrl { get; set; }

        // Schema
        public string SchemaJson { get; set; }
    }

 
}
