using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.OrderDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IOrderService : IService<Order>
    {
    
        Task<List<Order>> GetAllOrdersWithDetailsAsync(int pageNumber, int pageSize);
        Task<bool> UpdatePaymentStatusAsync(int orderId, ECommerce.Domain.Entity.PaymentStatus paymentStatus, string? paymentReference = null);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<IReadOnlyList<Order>> GetOrdersByUserIdAsync(int userId);
        Task<IReadOnlyList<Order>> GetOrdersByGuestIdAsync(Guid guestIdentifier);
        Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<IReadOnlyList<Order>> GetOrdersBetweenDatesAsync(DateTime start, DateTime end);
        Task<IReadOnlyList<Order>> GetOrdersAboveAmountAsync(decimal minTotal);
        Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int count = 10);
        Task<Order?> GetLastOrderForUserAsync(int userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string orderTracking);
        Task CancelOrderAsync(int orderId);
        Task<string?> GetTrackingInfoAsync(int orderId);
        Task<string?> GetInvoiceNumberAsync(int orderId);
        Task<bool> IsOrderReturnableAsync(int orderId);
        Task<OrderReportDto> GetOrderReportAsync(DateTime startDate, DateTime endDate);
        Task<IReadOnlyList<Order>> GetOrdersByPaymentMethodAsync(int paymentMethodId);
        Task<IReadOnlyList<Order>> GetOrdersByShippingMethodAsync(int shippingMethodId);
        Task<Order> CreateOrderForGuestAsync(Order order);
        Task<Order> CreateGuestOrderFromCartAsync(CreateOrderDto orderDto);
        Task<CheckoutProcessResult> ProcessCheckoutAsync(CheckoutRequestDto request);
        Task BulkUpdateStatusAsync(List<int> orderIds, string newStatus);

    }
}
