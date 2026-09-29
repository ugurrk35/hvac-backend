namespace ECommerce.API.Dtos.Products
{
    public class AttributeCombinationDetailDto
    {
        public int Id { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public List<AttributeValueDetailDto> AttributeValues { get; set; }
    }
}