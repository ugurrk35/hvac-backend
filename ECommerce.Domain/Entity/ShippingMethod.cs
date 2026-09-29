using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ShippingMethod:AuditableEntity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        /// <summary>Orders at or above this amount ship free. Null disables the rule.</summary>
        public decimal? FreeShippingThreshold { get; set; }
        public string TrackingUrl { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
