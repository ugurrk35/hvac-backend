using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public OrderRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<Order>> GetAllOrdersWithDetailsAsync(int pageNumber, int pageSize)
        {
            return await _dbContext.Orders
             .Where(o => !o.IsDeleted)
             .Include(o => o.OrdersItems) // sadece entity olan navigation'lar burada olabilir
             .ThenInclude(o => o.Product)
             .OrderByDescending(o => o.CreatedAt)
             .Skip((pageNumber - 1) * pageSize)
             .Take(pageSize)
             .ToListAsync();
        }


        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _dbContext.Orders
                .Include(o => o.OrdersItems)
                .ThenInclude(o=>o.Product)
                .ThenInclude(por=>por.ProductImages)
                .ThenInclude(por => por.Image)
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.ShippingMethod)
                .Include(o => o.BillingAddress)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _dbContext.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByGuestIdAsync(Guid guestIdentifier)
        {
            return await _dbContext.Orders
                .Where(o => o.GuestIdentifier == guestIdentifier && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _dbContext.Orders
                .Where(o => o.OrderStatusId == (int)status && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersBetweenDatesAsync(DateTime start, DateTime end)
        {
            return await _dbContext.Orders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersAboveAmountAsync(decimal minTotal)
        {
            return await _dbContext.Orders
                .Where(o => o.TotalAmount >= minTotal && !o.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int count = 10)
        {
            return await _dbContext.Orders
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Order?> GetLastOrderForUserAsync(int userId)
        {
            return await _dbContext.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order != null)
            {
                order.OrderStatus = newStatus;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task CancelOrderAsync(int orderId)
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            if (order != null)
            {
                order.OrderStatus = OrderStatus.Canceled;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<string?> GetTrackingInfoAsync(int orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.ShippingMethod)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            return order?.ShippingMethod?.TrackingUrl;
        }

        public async Task<string?> GetInvoiceNumberAsync(int orderId)
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            return order?.Id.ToString("INV00000");
        }

        public async Task<bool> IsOrderReturnableAsync(int orderId)
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);
            return order != null && order.CreatedAt.AddDays(14) >= DateTime.UtcNow && order.OrderStatus == OrderStatus.Completed;
        }

        public async Task<OrderReportDto> GetOrderReportAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await GetOrdersBetweenDatesAsync(startDate, endDate);
            return new OrderReportDto
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                CancelledOrders = orders.Count(o => o.OrderStatus == OrderStatus.Canceled),
                ReturnedOrders = 0 // Geri iade sistemi ayrıca takip ediliyorsa burada eklenmeli
            };
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByPaymentMethodAsync(int paymentMethodId)
        {
            return await _dbContext.Orders.Where(o => o.PaymentMethodId == paymentMethodId && !o.IsDeleted).ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByShippingMethodAsync(int shippingMethodId)
        {
            return await _dbContext.Orders.Where(o => o.ShippingMethodId == shippingMethodId && !o.IsDeleted).ToListAsync();
        }
        /// <summary>
        /// En son siparişi verilen tarihe göre getirir.
        /// Sipariş numarasına göre azalan sırada sıralar ve ilkini alır.
        /// Sipariş numarası, siparişin oluşturulma tarihine göre benzersizdir.
        /// so, bu yöntem belirli bir tarihteki en son siparişi alır.
        /// sipariş numarası oluşturmada kullanılır
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<Order?> GetLastOrderByDateAsync(DateTime date)
        {
            return await _dbContext.Orders
                .Where(o => o.CreatedAt.Date == date.Date && !o.IsDeleted)
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();
        }
    }
}
