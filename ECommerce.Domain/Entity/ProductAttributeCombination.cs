using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductAttributeCombination : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string Sku { get; set; }  // Benzersiz stok kodu
        public decimal Price { get; set; }  // Kombinasyon fiyatı eklenecek fiyat +
        public int Quantity { get; set; }  // Stok miktarı
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<ProductAttributeCombinationValue> ProductAttributeCombinationValues { get; set; }  // Özellik-değer kombinasyonları
    }
}

