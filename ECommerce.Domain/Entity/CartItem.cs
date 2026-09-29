using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class CartItem : AuditableEntity
    {
        public int ShoppingCartId { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public int? ProductAttributeCombinationId { get; set; }
        public ProductAttributeCombination ProductAttributeCombination { get; set; }
        public string? PersonalizationText { get; set; }
        public ICollection<CartItemAttributeSelection> AttributeSelections { get; set; } = new List<CartItemAttributeSelection>();
    }
}
