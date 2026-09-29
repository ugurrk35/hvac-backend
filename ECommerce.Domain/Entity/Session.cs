using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class Session : AuditableEntity
    {
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsRevoked { get; set; } = false;
        public ICollection<ShoppingCart> ShoppingCarts { get; set; }
    }
}
