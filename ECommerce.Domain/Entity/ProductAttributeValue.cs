using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductAttributeValue : AuditableEntity
    {
        public int ProductAttributeId { get; set; }  // Bağlı olduğu özellik ID
        public ProductAttribute ProductAttribute { get; set; }  // İlgili özellik nesnesi
        public string Value { get; set; }  // Değer (Örn: Kırmızı)
        public decimal PriceModifier { get; set; }  // Fiyat etkisi, eğer varsa
    }
}

