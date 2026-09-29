using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class OrderItem:AuditableEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } 
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ListUnitPrice { get; set; }
        public decimal ProductDiscountTotal { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public bool IsGift { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? VariantSnapshot { get; set; }
        public int? ProductAttributeCombinationId { get; set; }
        public ProductAttributeCombination ProductAttributeCombination { get; set; }
    }
}
