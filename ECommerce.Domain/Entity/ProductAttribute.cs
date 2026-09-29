using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductAttribute : AuditableEntity
    {
        public string Name { get; set; }  // Özellik adı (Örn: Renk)
        public bool IsPersonalizationText { get; set; }  // Kişiselleştirilebilir metin mi?
        public string TextPrompt { get; set; }  // Kişiselleştirme metni için açıklama
        public int? MaxLength { get; set; }  // Kişiselleştirme metni uzunluk sınırı
        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }  // Özelliğin olası değerleri
    }

}