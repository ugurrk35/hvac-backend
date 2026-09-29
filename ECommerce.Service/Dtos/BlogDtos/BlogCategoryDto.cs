using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.BlogDtos
{
    public class BlogCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }

        // SEO Alanları
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImageUrl { get; set; }
        public string CanonicalUrl { get; set; }

        // Bağlantılı bilgiler
        public int PostCount { get; set; }   // kaç tane post var
        public List<BlogPostShortDto> Posts { get; set; } = new();
    }
}
