using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.UserDtos
{
    public class LoginDto
    {
        public string? Identifier { get; set; }
        public string? Username { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Password { get; set; }
    }

}
