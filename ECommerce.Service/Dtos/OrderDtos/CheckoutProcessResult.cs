using ECommerce.Domain.Entity;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class CheckoutProcessResult
    {
        public Order Order { get; set; }
        public string? JwtToken { get; set; }
        public bool UserCreated { get; set; }
        public bool UserLoggedIn { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserEmail { get; set; }
    }
}
