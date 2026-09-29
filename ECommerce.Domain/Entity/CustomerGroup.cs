using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class CustomerGroup : AuditableEntity

    {
        // Kullanıcılar
        public ICollection<ApplicationUser> Users { get; set; }

        public CustomerGroup()
        {
            ProductPrices = new HashSet<ProductPrice>();
            Users = new HashSet<ApplicationUser>();
        }
        public string Name { get; set; } // "Bayi", "Toptancı", "VIP" vb.

        public ICollection<ProductPrice> ProductPrices { get; set; }
    }

}
