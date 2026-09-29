using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<List<Order>> GetAllOrdersWithDetailsAsync(int pageNumber, int pageSize);
        Task<Order?> GetLastOrderByDateAsync(DateTime date);
        // Detaylarla birlikte tek sipariş
        Task<Order?> GetOrderWithDetailsAsync(int orderId);

        // Kullanıcıya göre sipariş listesi
        Task<IReadOnlyList<Order>> GetOrdersByUserIdAsync(int userId);

        // Misafir kullanıcıya göre sipariş listesi
        Task<IReadOnlyList<Order>> GetOrdersByGuestIdAsync(Guid guestIdentifier);

        // Sipariş durumu filtreli
        Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status);

        // Tarih aralığına göre siparişler (raporlama için)
        Task<IReadOnlyList<Order>> GetOrdersBetweenDatesAsync(DateTime start, DateTime end);

        // Toplam tutara göre filtreli siparişler
        Task<IReadOnlyList<Order>> GetOrdersAboveAmountAsync(decimal minTotal);

        // Son X sipariş (admin panelde en son siparişler gibi)
        Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int count = 10);

        // Kullanıcının en son siparişi
        Task<Order?> GetLastOrderForUserAsync(int userId);

        // Siparişi güncelle (adres, ödeme durumu vs)
        Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);

        // İptal et
        Task CancelOrderAsync(int orderId);

        // Kargo takibi için veri (ShippingMethod, durum, tarih vs)
        Task<string?> GetTrackingInfoAsync(int orderId);

        // Fatura bilgileri
        Task<string?> GetInvoiceNumberAsync(int orderId);

        // Geri iade sorguları (iade edilebilir mi, edilmiş mi)
        Task<bool> IsOrderReturnableAsync(int orderId);

        // Kapsamlı sipariş raporu
        Task<OrderReportDto> GetOrderReportAsync(DateTime startDate, DateTime endDate);

        // Kullanıcının ödeme yöntemine göre siparişleri
        Task<IReadOnlyList<Order>> GetOrdersByPaymentMethodAsync(int paymentMethodId);

        // Kargo yöntemi filtreli
        Task<IReadOnlyList<Order>> GetOrdersByShippingMethodAsync(int shippingMethodId);
    }

}
