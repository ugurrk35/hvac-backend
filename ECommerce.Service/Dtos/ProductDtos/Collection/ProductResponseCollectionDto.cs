using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos.Collection
{
    public class ProductResponseCollectionDto
    {
        public string Id { get; set; }
        public string Handle { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public ImageCollectionDto FeaturedImage { get; set; }
        public PriceRangeCollectionDto PriceRange { get; set; }
    }

   


   

}
