using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductImage : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } 
        public int ImageId { get; set; }
        public Image Image { get; set; }
        public int SortOrder { get; set; }
    }
}
