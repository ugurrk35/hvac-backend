namespace ECommerce.Adaptor.Payment.Abstract
{
    public class PaymentRefundRequest { public string OrderId { get; set; } = string.Empty; public decimal Amount { get; set; } public string ReferenceNo { get; set; } = string.Empty; }
    public class PaymentRefundResult { public bool Success { get; set; } public string? ErrorMessage { get; set; } public string? ProviderResponse { get; set; } }
}
