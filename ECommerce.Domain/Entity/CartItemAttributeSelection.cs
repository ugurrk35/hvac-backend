namespace ECommerce.Domain.Entity
{
    public class CartItemAttributeSelection : AuditableEntity
    {
        public int CartItemId { get; set; }
        public CartItem CartItem { get; set; }

        public int ProductAttributeId { get; set; }
        public ProductAttribute ProductAttribute { get; set; }

        public int? ProductAttributeValueId { get; set; }
        public ProductAttributeValue? ProductAttributeValue { get; set; }

        public int? ProductAttributeCombinationId { get; set; }
        public ProductAttributeCombination? ProductAttributeCombination { get; set; }

        public int? ProductAttributeCombinationValueId { get; set; }
        public ProductAttributeCombinationValue? ProductAttributeCombinationValue { get; set; }

        public string? PersonalizationText { get; set; }
    }
}
