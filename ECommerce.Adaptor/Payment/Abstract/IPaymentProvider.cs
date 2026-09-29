using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public interface IPaymentProvider
    {
        Task<PaymentInitializeResult> InitializeAsync(PaymentRequest request);
        Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request);
    }
}
