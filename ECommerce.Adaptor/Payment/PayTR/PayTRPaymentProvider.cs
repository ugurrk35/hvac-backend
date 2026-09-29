using ECommerce.Adaptor.Payment.Abstract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Adaptor.Payment.PayTR
{
    public class PayTRPaymentProvider : IPaymentProvider
    {
        private readonly PayTRConfig _cfg;

        public PayTRPaymentProvider(PayTRConfig config)
        {
            _cfg = config;
        }

        public async Task<PaymentInitializeResult> InitializeAsync(PaymentRequest request)
        {
            var paytrRequest = request as PayTRPaymentRequest;
            if (paytrRequest == null)
            {
                return new PaymentInitializeResult
                {
                    Success = false,
                    ErrorMessage = "PayTR ödemesi için PayTRPaymentRequest bekleniyor."
                };
            }

            var basketArray = paytrRequest.Basket.Select(i =>
                new object[] { i.Name, i.Total.ToString("0.00", CultureInfo.InvariantCulture), i.Quantity }
            ).ToArray();

            string basketEncoded = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(basketArray))
            );

            int amount = (int)Math.Round(paytrRequest.Amount * 100, MidpointRounding.AwayFromZero);

            int noInstallment = paytrRequest.NoInstallment != 0 ? paytrRequest.NoInstallment : _cfg.NoInstallment;
            int maxInstallment = paytrRequest.MaxInstallment != 0 ? paytrRequest.MaxInstallment : _cfg.MaxInstallment;
            string currency = !string.IsNullOrWhiteSpace(paytrRequest.Currency) ? paytrRequest.Currency : _cfg.Currency;
            bool testMode = paytrRequest.TestMode ?? _cfg.TestMode;
            int timeoutLimit = paytrRequest.TimeoutLimit != 0 ? paytrRequest.TimeoutLimit : _cfg.TimeoutLimit;
            bool debugOn = paytrRequest.DebugOn || _cfg.DebugOn;
            string lang = !string.IsNullOrWhiteSpace(paytrRequest.Language) ? paytrRequest.Language : _cfg.Language;

            string tokenString = string.Concat(
                _cfg.MerchantId,
                paytrRequest.CustomerIp,
                paytrRequest.OrderId,
                paytrRequest.CustomerEmail,
                amount.ToString(CultureInfo.InvariantCulture),
                basketEncoded,
                noInstallment.ToString(CultureInfo.InvariantCulture),
                maxInstallment.ToString(CultureInfo.InvariantCulture),
                currency,
                testMode ? "1" : "0",
                _cfg.MerchantSalt
            );

            string paytrToken = PayTRHash.Hmac(tokenString, _cfg.MerchantKey);

            var form = new Dictionary<string, string>
            {
                { "merchant_id", _cfg.MerchantId },
                { "user_ip", paytrRequest.CustomerIp },
                { "merchant_oid", paytrRequest.OrderId },
                { "email", paytrRequest.CustomerEmail },
                { "payment_amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "user_name", paytrRequest.CustomerName },
                { "user_address", paytrRequest.CustomerAddress ?? string.Empty },
                { "user_phone", paytrRequest.CustomerPhone ?? string.Empty },
                { "merchant_ok_url", paytrRequest.OkUrl },
                { "merchant_fail_url", paytrRequest.FailUrl },
                { "user_basket", basketEncoded },
                { "no_installment", noInstallment.ToString(CultureInfo.InvariantCulture) },
                { "max_installment", maxInstallment.ToString(CultureInfo.InvariantCulture) },
                { "currency", currency },
                { "test_mode", testMode ? "1" : "0" },
                { "timeout_limit", timeoutLimit.ToString(CultureInfo.InvariantCulture) },
                { "debug_on", debugOn ? "1" : "0" },
                { "lang", lang ?? "tr" },
                { "paytr_token", paytrToken }
            };

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync(_cfg.ApiUrl, new FormUrlEncodedContent(form));

                if (!response.IsSuccessStatusCode)
                {
                    return new PaymentInitializeResult
                    {
                        Success = false,
                        ErrorMessage = $"PayTR servis hatası: {(int)response.StatusCode}"
                    };
                }

                string json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<PayTRTokenResponse>(json);

                if (obj == null || obj.status != "success")
                {
                    return new PaymentInitializeResult
                    {
                        Success = false,
                        ErrorMessage = obj?.reason ?? "PayTR yanıtı çözümlenemedi"
                    };
                }

                return new PaymentInitializeResult
                {
                    Success = true,
                    Token = obj.token,
                    IFrameUrl = _cfg.IFrameBaseUrl + obj.token
                };
            }
            catch (Exception ex)
            {
                return new PaymentInitializeResult
                {
                    Success = false,
                    ErrorMessage = $"PayTR istek hatası: {ex.Message}"
                };
            }
        }

        public async Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderId) || request.Amount <= 0) return new PaymentRefundResult { Success = false, ErrorMessage = "Geçersiz iade isteği." };
            var amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            var token = PayTRHash.Hmac(_cfg.MerchantId + request.OrderId + amount + _cfg.MerchantSalt, _cfg.MerchantKey);
            var form = new Dictionary<string, string> { ["merchant_id"] = _cfg.MerchantId, ["merchant_oid"] = request.OrderId, ["return_amount"] = amount, ["paytr_token"] = token, ["reference_no"] = request.ReferenceNo };
            try { using var client = new HttpClient(); var response = await client.PostAsync(_cfg.RefundApiUrl, new FormUrlEncodedContent(form)); var body = await response.Content.ReadAsStringAsync(); var result = JsonConvert.DeserializeObject<PayTRRefundResponse>(body); return new PaymentRefundResult { Success = response.IsSuccessStatusCode && result?.status == "success", ErrorMessage = result?.err_msg ?? (!response.IsSuccessStatusCode ? $"PayTR servis hatası: {(int)response.StatusCode}" : null), ProviderResponse = body }; }
            catch (Exception ex) { return new PaymentRefundResult { Success = false, ErrorMessage = ex.Message }; }
        }

        private class PayTRRefundResponse { public string? status { get; set; } public string? err_msg { get; set; } }
    }
}
