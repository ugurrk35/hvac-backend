using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Dtos
{
    public class DiscountResult
    {
        public int ProductId { get; set; }        // Ürün id'si için eklendi
        public string CampaignName { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Description { get; set; }
    }
}
