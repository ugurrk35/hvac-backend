using ECommerce.Adaptor.Payment.Abstract;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;

namespace ECommerce.Adaptor.Payment.PayTR
{
    public class PayTRCallbackValidator : IPaymentCallbackValidator
    {
        private readonly PayTRConfig _cfg;

        public PayTRCallbackValidator(PayTRConfig cfg)
        {
            _cfg = cfg;
        }

        public PaymentCallbackResult ValidateCallback(IFormCollection form)
        {
            if (form == null)
            {
                return Fail("Callback form boş geldi");
            }

            string merchantOid = form["merchant_oid"];
            string status = form["status"];
            string totalAmountRaw = form["total_amount"];
            string hash = form["hash"];

            if (string.IsNullOrWhiteSpace(merchantOid) ||
                string.IsNullOrWhiteSpace(status) ||
                string.IsNullOrWhiteSpace(totalAmountRaw) ||
                string.IsNullOrWhiteSpace(hash))
            {
                return Fail("Eksik callback alanları");
            }

            // PayTR hash: HMACSHA256(merchant_oid + merchant_salt + status + total_amount, merchant_key)
            string hashInput = string.Concat(merchantOid, _cfg.MerchantSalt, status, totalAmountRaw);
            string expected = PayTRHash.Hmac(hashInput, _cfg.MerchantKey);

            if (!string.Equals(hash, expected, StringComparison.Ordinal))
            {
                return Fail("Geçersiz imza");
            }

            decimal paidAmount = 0;
            if (decimal.TryParse(totalAmountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var cents))
            {
                paidAmount = cents / 100m;
            }

            bool success = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
            string failReason = form.ContainsKey("failed_reason_msg") ? form["failed_reason_msg"].ToString() : null;

            return new PaymentCallbackResult
            {
                Success = success,
                OrderId = merchantOid,
                PaidAmount = paidAmount,
                ErrorMessage = success ? null : failReason
            };
        }

        private static PaymentCallbackResult Fail(string message) =>
            new PaymentCallbackResult
            {
                Success = false,
                ErrorMessage = message
            };
    }
}
