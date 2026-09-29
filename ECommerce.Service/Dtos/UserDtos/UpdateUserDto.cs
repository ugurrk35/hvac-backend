using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.UserDtos
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(256)]
        public string? UserName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }

        // Roller (opsiyonel - ayrı endpoint'te de yönetilebilir)
        public List<string>? Roles { get; set; }
    }
}