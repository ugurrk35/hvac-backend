using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Dtos
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public int? ProductAttributeCombinationId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public decimal TotalPrice { get; set; }
        public string? PersonalizationText { get; set; }
        public List<CartItemAttributeDto> Attributes { get; set; } = new();
    }

    public class CartItemAttributeDto
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public int? AttributeValueId { get; set; }
        public string? AttributeValue { get; set; }
        public string? PersonalizationText { get; set; }
    }
}
