using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class Image:AuditableEntity
    {
        public string Title { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileExtension { get; set; }
        public int SizeInBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public ICollection<BlogPostImage>? BlogPostImage { get; set; }
        public ICollection<SeoFriendlyImage>? SeoFriendlyImage { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; }


    }
}
