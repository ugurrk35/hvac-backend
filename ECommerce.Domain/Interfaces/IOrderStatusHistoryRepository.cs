using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IOrderStatusHistoryRepository : IRepository<OrderStatusHistory>
    {
        Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId);
        // Gerekirse başka metotlar ekleyebilirsin
    }
}
