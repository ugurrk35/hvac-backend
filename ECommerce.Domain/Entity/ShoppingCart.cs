using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ShoppingCart:AuditableEntity
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public Guid? GuestIdentifier { get; set; }
        public int? SessionId { get; set; }
        public bool IsOrdered { get; set; }
        public Session? Session { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<Order> Orders { get; set; }
        public ICollection<CartItem> CartItems { get; set; }



        
    }
}
