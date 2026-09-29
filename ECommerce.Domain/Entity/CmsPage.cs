using System;

namespace ECommerce.Domain.Entity
{
    public class CmsPage : AuditableEntity
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ContentHtml { get; set; }
        public bool IsPublished { get; set; }

        // Optional basic SEO
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? CanonicalUrl { get; set; }
    }
}

