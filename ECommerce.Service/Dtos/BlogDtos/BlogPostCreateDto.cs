using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.BlogDtos
{
    public class BlogPostCreateDto
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public int BlogCategoryId { get; set; }
        public DateTime PublishDate { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public ICollection<int> TagIds { get; set; } = new List<int>();
        public ICollection<int> ImageIds { get; set; } = new List<int>();

        // SEO
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImageUrl { get; set; }
        public string CanonicalUrl { get; set; }
        public string SchemaJson { get; set; } = "{}"; // boş JSON varsayılanı
    }
}
