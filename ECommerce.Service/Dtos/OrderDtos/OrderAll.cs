using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.OrderDtos
{
    public class OrderAll
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string? FullName { get; set; }

        public DateTime CreateDate { get; set; }
        public string OrderStatusName { get; set; }
        public decimal TotalAmount { get; set; }
        //public List<OrderItemDto> OrdersItems { get; set; }
    }
}
