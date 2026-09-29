using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public Guid? GuestIdentifier { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string? Notes { get; set; }
        public int ShoppingCartId { get; set; }
        public string CargoTracking { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal CampaignDiscountTotal { get; set; }

        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public int? PaymentStatusId { get; set; }
        public string? PaymentReference { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal RefundableAmount { get; set; }
        public int ShippingMethodId { get; set; }
        public string? ShippingMethodName { get; set; }
        public int OrderStatusId { get; set; }
        public AddressDto ShippingAddress { get; set; }
        public AddressDto BillingAddress { get; set; }

        public List<OrderItemDto> OrdersItems { get; set; }
    }
}
