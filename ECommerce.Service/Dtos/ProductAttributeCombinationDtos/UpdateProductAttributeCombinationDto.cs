using ECommerce.Service.Dtos.ProductDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductAttributeCombinationDtos
{
    public class UpdateProductAttributeCombinationDto
    {
        public int Id { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int ProductId { get; set; }
        //public ProductPriceDto ProductPrice { get; set; }
        public List<UpdateProductAttributeCombinationValueDto> Values { get; set; } = new();
    }

}
