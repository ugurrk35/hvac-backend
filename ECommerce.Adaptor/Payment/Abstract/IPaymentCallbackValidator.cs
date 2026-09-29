using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.Abstract
{
    public interface IPaymentCallbackValidator
    {
        PaymentCallbackResult ValidateCallback(IFormCollection form);
    }
}
