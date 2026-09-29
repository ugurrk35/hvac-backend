using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class CheckoutRequestDto
    {
        [Required]
        public CheckoutMode Mode { get; set; }

        [Required]
        public CreateOrderDto Order { get; set; }

        public Guid? GuestIdentifier { get; set; }

        public CheckoutLoginDto? Login { get; set; }

        public CheckoutRegisterDto? Registration { get; set; }
    }

    public enum CheckoutMode
    {
        Guest = 0,
        ExistingUser = 1,
        Register = 2
    }

    public class CheckoutLoginDto
    {
        [Required]
        [StringLength(256)]
        public string Identifier { get; set; }

        [Required]
        [StringLength(256, MinimumLength = 6)]
        public string Password { get; set; }
    }

    public class CheckoutRegisterDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
