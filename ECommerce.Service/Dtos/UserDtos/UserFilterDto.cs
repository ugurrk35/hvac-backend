using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.UserDtos
{
    public class UserFilterDto
    {
        public string? SearchTerm { get; set; } // Email, FirstName, LastName, Username için arama
        public bool? IsActive { get; set; }
        public string? Role { get; set; } // Belirli bir role sahip kullanıcıları filtrele
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? IsBlocked { get; set; }
        
        // Sıralama
        public string SortBy { get; set; } = "createdAt"; // createdAt, email, firstName, lastName
        public string SortOrder { get; set; } = "desc"; // asc, desc
        
        // Sayfalama
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}