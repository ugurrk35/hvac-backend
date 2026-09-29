namespace ECommerce.Domain.Entity
{
    public class OrderReturnRequestItem : AuditableEntity
    {
        public int OrderReturnRequestId { get; set; }
        public OrderReturnRequest? OrderReturnRequest { get; set; }
        public int OrderItemId { get; set; }
        public OrderItem? OrderItem { get; set; }
        public int Quantity { get; set; }
    }
}
