using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class BulkPriceUpdateDto
    {
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public int? ProductAttributeCombinationId { get; set; }
        public int? CustomerGroupId { get; set; }

        public decimal? SetPrice { get; set; }
        public decimal? SetDiscountPrice { get; set; }
        public decimal? IncreaseByPercent { get; set; }
        public decimal? DecreaseByPercent { get; set; }

        public bool ApplyToActivePricesOnly { get; set; } = true;
        public DateTime? EffectiveDate { get; set; }
    }
}
