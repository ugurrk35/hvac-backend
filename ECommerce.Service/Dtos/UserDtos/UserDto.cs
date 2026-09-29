using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.UserDtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Dilersen rol bilgisi gibi ek alanlar da olabilir.
        public List<string> Roles { get; set; } = new List<string>();
    }

}
