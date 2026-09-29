using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class SeoFriendlyImage:AuditableEntity
    {
        public int ImageId { get; set; }
        public Image Image { get; set; }
        public string SeoFilename { get; set; }
        public string SeoAltText { get; set; }
        public string SeoTitle { get; set; }
        public string SeoCaption { get; set; }
    }
}

