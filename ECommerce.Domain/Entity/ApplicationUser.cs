
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ApplicationUser : IdentityUser<int>
    {
        public ApplicationUser()
        {
            Orders = new HashSet<Order>();
            Sessions = new HashSet<Session>();
            CustomerAddresses = new HashSet<CustomerAddress>();
            Favorites = new HashSet<CustomerFavorite>();
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsGuest { get; set; } = false;
        
        // Admin panel için ek alanlar
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? LastModifiedBy { get; set; }
        // Müşteri grubu ilişkisi
        public int? CustomerGroupId { get; set; }   // opsiyonel: her kullanıcı gruba ait olmayabilir
        public CustomerGroup? CustomerGroup { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Session> Sessions { get; set; }
        public ICollection<CustomerAddress> CustomerAddresses { get; set; }
        public ICollection<CustomerFavorite> Favorites { get; set; }
        public CustomerMarketingConsent? MarketingConsent { get; set; }
    }
}
