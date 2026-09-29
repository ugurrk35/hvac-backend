using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductProductTag:AuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int ProductTagId { get; set; }
        public ProductTag ProductTag { get; set; }

    }
}
