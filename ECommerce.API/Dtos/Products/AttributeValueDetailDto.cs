namespace ECommerce.API.Dtos.Products
{
    public class AttributeValueDetailDto
    {
        public int AttributeId { get; set; }
        public int? AttributeValueId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
        public string PersonalizationText { get; set; }
        public bool IsPersonalization { get; set; }

    }
}