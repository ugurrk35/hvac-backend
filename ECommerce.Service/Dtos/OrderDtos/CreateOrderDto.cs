using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class CreateOrderDto
    {
        public int? UserId { get; set; }
        public Guid? GuestIdentifier { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int ShoppingCartId { get; set; }
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal TotalAmount { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int PaymentMethodId { get; set; }
        public int? ShippingMethodId { get; set; }
        [StringLength(50)]
        public string? CouponCode { get; set; }
        [Required]
        public AddressDto ShippingAddress { get; set; }
        [Required]
        public AddressDto BillingAddress { get; set; }
        [Required]
        [StringLength(50)]
        public string CustomerFirstName { get; set; }
        [Required]
        [StringLength(50)]
        public string CustomerLastName { get; set; }
        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; }
        [Required]
        [Phone]
        public string CustomerPhone { get; set; }

        public string Notes { get; set; }
        // ❌ Bunu kaldır:
        // public List<OrderItemDto> OrdersItems { get; set; }
    }
}
