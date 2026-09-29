using System;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class CheckoutResultDto
    {
        public OrderDto Order { get; set; }
        public string? JwtToken { get; set; }
        public bool UserCreated { get; set; }
        public bool UserLoggedIn { get; set; }
        public OrderUserInfoDto? AuthenticatedUser { get; set; }
    }

    public class OrderUserInfoDto
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}
