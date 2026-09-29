using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos.Collection
{

    public class PriceRangeCollectionDto
    {
        public VariantPriceCollectionDto MaxVariantPrice { get; set; }
        public VariantPriceCollectionDto MinVariantPrice { get; set; } // opsiyonel
    }
}
