using ECommerce.API.Models;
using ECommerce.Adaptor.Payment.Abstract;
using ECommerce.Adaptor.Payment.PayTR;
using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract;
using ECommerce.Service.Response;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Security.Claims;
using ECommerce.API.Services;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ödeme işlemleri (sağlayıcı callbackleri dahil) ve sipariş ödeme durumlarını yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IProductService _productService;
        private readonly IPaymentCallbackValidator _callbackValidator;
        private readonly IPaymentProvider _paymentProvider;
        private readonly PayTRConfig _cfg;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IServerSideConversionService _serverSideConversionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IEmailService _emailService;

 
        public PaymentController(
            IOrderService orderService,
            ILogger<PaymentController> logger,
            IProductService productService,
            IPaymentCallbackValidator callbackValidator,
            PayTRConfig cfg,
            IPaymentProvider paymentProvider,
            ApplicationDbContext db,
            IConfiguration configuration,
            IServerSideConversionService serverSideConversionService,
            IShoppingCartService shoppingCartService,
            IEmailService emailService)
        {
            _orderService = orderService;
            _logger = logger;
            _cfg = cfg;

            _productService = productService;
            _callbackValidator = callbackValidator;
            _paymentProvider = paymentProvider;
            _db = db;
            _configuration = configuration;
            _serverSideConversionService = serverSideConversionService;
            _shoppingCartService = shoppingCartService;
            _emailService = emailService;
        }

        [HttpPost("paytr/notify")]
        [AllowAnonymous]
        public async Task<IActionResult> PaytrNotify([FromForm] IFormCollection form)
        {
            if (form == null || form.Count == 0)
                return BadRequest();

            var merchantOid = form["merchant_oid"].ToString();
            var status = form["status"].ToString();
            var totalAmountRaw = form["total_amount"].ToString();
            var hash = form["hash"].ToString();
            var paymentId = form["payment_id"].ToString();

            var hashInput = merchantOid + _cfg.MerchantSalt + status + totalAmountRaw;
            var expected = PayTRHash.Hmac(hashInput, _cfg.MerchantKey);

            if (hash != expected)
            {
                _logger.LogWarning("PayTR HASH FAIL: {OrderNo}", merchantOid);
                return BadRequest();
            }

            // New payment attempts use a provider-unique oid. Keep numeric parsing as a
            // fallback so callbacks belonging to legacy orders continue to be accepted.
            var order = await _db.Orders.FirstOrDefaultAsync(item => item.PaytrMerchantOid == merchantOid && !item.IsDeleted);
            if (order == null && TryGetOrderIdFromMerchantOid(merchantOid, out var legacyOrderId))
                order = await _orderService.GetByIdAsync(legacyOrderId);
            if (order == null)
            {
                _logger.LogError("PayTR callback için sipariş bulunamadı: {Oid}", merchantOid);
                return BadRequest();
            }

            // ÇİFT BİLDİRİM KORUMASI
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await TrySendOrderPaidEmailAsync(order.Id, HttpContext.RequestAborted);
                return Content("OK");
            }

            if (!decimal.TryParse(totalAmountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var paidAmountInCents))
            {
                _logger.LogWarning("PayTR invalid total amount for order {OrderNo}: {TotalAmount}", merchantOid, totalAmountRaw);
                return BadRequest();
            }

            var paidAmount = paidAmountInCents / 100m;
            if (Math.Abs(order.TotalAmount - paidAmount) > 0.01m)
            {
                _logger.LogError(
                    "PayTR amount mismatch for order {OrderNo}. Expected {Expected}, received {Received}",
                    merchantOid,
                    order.TotalAmount,
                    paidAmount);
                return BadRequest();
            }

            if (status == "success")
            {
                var paymentStatusChanged = await _orderService.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Paid, paymentId);
                var campaignIds = (order.AppliedCampaignIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => int.TryParse(value, out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToArray();
                if (paymentStatusChanged && campaignIds.Length > 0)
                {
                    await _db.Campaigns
                        .Where(campaign => campaignIds.Contains(campaign.Id))
                        .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.UsageCount, campaign => campaign.UsageCount + 1));
                }
                if (paymentStatusChanged)
                {
                    var trackedOrder = await _db.Orders.FirstOrDefaultAsync(item => item.Id == order.Id, HttpContext.RequestAborted);
                    if (trackedOrder != null)
                    {
                        trackedOrder.PaytrMerchantOid = merchantOid;
                        await _db.SaveChangesAsync(HttpContext.RequestAborted);
                    }
                    await _shoppingCartService.ClearCartAsync(order.ShoppingCartId);
                    await _serverSideConversionService.TrackPurchaseAsync(order.Id, HttpContext.RequestAborted);
                }
                await TrySendOrderPaidEmailAsync(order.Id, HttpContext.RequestAborted);
            }
            else
            {
                var failCode = form["failed_reason_code"].ToString();
                var failMsg = form["failed_reason_msg"].ToString();

                await _orderService.UpdatePaymentStatusAsync(
                    order.Id,
                    PaymentStatus.Failed,
                    paymentId
                );
            }

            return Content("OK"); //PAYTR ZORUNLU CEVAP
        }

        private async Task TrySendOrderPaidEmailAsync(int orderId, CancellationToken cancellationToken)
        {
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == orderId && !item.IsDeleted, cancellationToken);
            if (order == null || order.PaymentStatus != PaymentStatus.Paid || order.PaymentEmailSentAt.HasValue) return;

            var detailedOrder = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (detailedOrder == null) return;
            var sent = await _emailService.SendOrderPaidAsync(detailedOrder, cancellationToken);
            if (!sent)
            {
                _logger.LogWarning("Ödeme e-postası gönderilemedi; sonraki PayTR bildirimi veya admin yeniden gönderimi ile tekrar denenecek. Sipariş: {OrderId}", orderId);
                return;
            }

            var trackedOrder = await _db.Orders.FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);
            if (trackedOrder == null) return;
            trackedOrder.PaymentEmailSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }


        [HttpPost("paytr/init")]
        [AllowAnonymous]
        [EnableRateLimiting("payment")]
        public async Task<IActionResult> InitializePaytr([FromBody] PaytrInitRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                return BadRequest(DataResponse<object>.CreateFailure("Geçersiz sipariş numarası"));
            }

            var order = await _orderService.GetOrderWithDetailsAsync(request.OrderId);
            if (order == null)
            {
                return NotFound(DataResponse<object>.CreateFailure("Sipariş bulunamadı"));
            }

            if (!CurrentRequestOwnsOrder(order, request.GuestIdentifier))
                return Forbid();

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return BadRequest(DataResponse<object>.CreateFailure("Bu siparişin ödemesi zaten alınmış."));
            }

            if (order.PaymentStatus == PaymentStatus.Failed)
            {
                return BadRequest(DataResponse<object>.CreateFailure("Başarısız ödeme için yeni bir sipariş oluşturun."));
            }

            if (order.TotalAmount <= 0 || order.OrdersItems == null || !order.OrdersItems.Any())
            {
                _logger.LogWarning("Payment initialization rejected for invalid order {OrderId}", request.OrderId);
                return BadRequest(DataResponse<object>.CreateFailure("Sipariş ödeme için geçerli değil."));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var customerName = string.Join(" ", new[] { order.CustomerFirstName, order.CustomerLastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            var chargeableItems = order.OrdersItems.Where(item => !item.IsGift && item.Price * item.Quantity > 0m).ToList();
            var productTotal = chargeableItems.Sum(i => i.Price * i.Quantity);
            var discountRatio = productTotal > 0m
                ? Math.Min(1m, order.CampaignDiscountTotal / productTotal)
                : 0m;
            var basket = chargeableItems.Select(i => new PaymentBasketItem
            {
                Name = string.IsNullOrWhiteSpace(i.ProductName) ? i.Product?.Name ?? $"Ürün {i.ProductId}" : i.ProductName,
                Total = Math.Round(i.Price * i.Quantity * (1m - discountRatio), 2, MidpointRounding.AwayFromZero),
                Quantity = i.Quantity
            }).ToList();

            // Rounding the line discounts can leave a few kuruş; keep the provider basket exact.
            var expectedProductTotal = Math.Max(0m, order.TotalAmount - order.ShippingAmount);
            var roundingDifference = expectedProductTotal - basket.Sum(item => item.Total);
            if (basket.Count > 0 && roundingDifference != 0m)
                basket[0].Total += roundingDifference;

            if (order.ShippingAmount > 0)
            {
                basket.Add(new PaymentBasketItem
                {
                    Name = order.ShippingMethod?.Name ?? "Kargo",
                    Total = order.ShippingAmount,
                    Quantity = 1
                });
            }

            var okUrl = string.IsNullOrWhiteSpace(request.SuccessUrl)
                ? $"{Request.Scheme}://{Request.Host}/payment/success"
                : request.SuccessUrl;

            var failUrl = string.IsNullOrWhiteSpace(request.FailUrl)
                ? $"{Request.Scheme}://{Request.Host}/payment/fail"
                : request.FailUrl;

            if (!IsAllowedReturnUrl(okUrl) || !IsAllowedReturnUrl(failUrl))
                return BadRequest(DataResponse<object>.CreateFailure("Ödeme dönüş adresi izinli değil."));

            var paytrRequest = new PayTRPaymentRequest
            {
                OrderId = await CreateUniquePaytrMerchantOidAsync(order.Id, HttpContext.RequestAborted),
                Amount = order.TotalAmount,
                CustomerEmail = order.CustomerEmail ?? string.Empty,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? order.CustomerEmail ?? "Müşteri" : customerName,
                CustomerIp = ipAddress,
                CustomerAddress = order.ShippingAddress?.AddressLine ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                OkUrl = okUrl,
                FailUrl = failUrl,
                Basket = basket
            };

            var initResult = await _paymentProvider.InitializeAsync(paytrRequest);
            if (!initResult.Success)
            {
                return BadRequest(DataResponse<object>.CreateFailure(initResult.ErrorMessage ?? "PayTR başlatma hatası"));
            }

            var response = new
            {
                token = initResult.Token,
                iframeUrl = initResult.IFrameUrl
            };

            return Ok(DataResponse<object>.CreateSuccess(response, "PayTR iframe token oluşturuldu."));
        }

        private async Task<string> CreateUniquePaytrMerchantOidAsync(int orderId, CancellationToken cancellationToken)
        {
            string merchantOid;
            do
            {
                merchantOid = $"MB{orderId}{Guid.NewGuid():N}";
            }
            while (await _db.Orders.AnyAsync(item => item.PaytrMerchantOid == merchantOid, cancellationToken));

            var trackedOrder = await _db.Orders.FirstAsync(item => item.Id == orderId, cancellationToken);
            trackedOrder.PaytrMerchantOid = merchantOid;
            await _db.SaveChangesAsync(cancellationToken);
            return merchantOid;
        }

        private static bool TryGetOrderIdFromMerchantOid(string merchantOid, out int orderId)
        {
            if (int.TryParse(merchantOid, out orderId)) return true;
            var separator = merchantOid.IndexOf('-');
            return separator > 2
                && merchantOid.StartsWith("MB", StringComparison.Ordinal)
                && int.TryParse(merchantOid.AsSpan(2, separator - 2), out orderId);
        }

        private bool CurrentRequestOwnsOrder(Order order, Guid? guestIdentifier)
        {
            if (order.UserId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                return int.TryParse(userId, out var currentUserId) && currentUserId == order.UserId.Value;
            }
            return order.GuestIdentifier.HasValue && guestIdentifier == order.GuestIdentifier;
        }

        private bool IsAllowedReturnUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) return false;
            var defaultAllowedOrigins = new[]
            {
                "https://kombiklimaburada.com",
                "https://www.kombiklimaburada.com",
                "https://admin.kombiklimaburada.com"
            };
            var configuredAllowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            var allowedOrigins = defaultAllowedOrigins.Concat(configuredAllowedOrigins);
            var currentOrigin = $"{Request.Scheme}://{Request.Host}";
            return allowedOrigins.Any(origin => string.Equals(origin.TrimEnd('/'), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) ||
                   string.Equals(currentOrigin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("installments/{productId:int}")]
        public async Task<IActionResult> GetInstallments(int productId)
        {
            try
            {
                var product = await _productService.GetByIdAsync(productId);
                if (product == null)
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));

                var price = product.DiscountPrice ?? product.BasePrice;
                var plans = new[] { 3, 6, 9, 12 };
                var result = plans.Select(m => new
                {
                    months = m,
                    monthly = Math.Round(price / m, 2),
                    total = Math.Round(price, 2),
                    rate = 0
                });

                return Ok(DataResponse<object>.CreateSuccess(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Taksit planı oluşturulurken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Taksit planı getirilemedi"));
            }
        }

        // Önceki taslak uç noktalar (gerektiğinde uyarlayabilirsiniz)
        // [HttpGet("success")]
        // public IActionResult PaymentSuccess([FromQuery] string orderId)
        // {
        //     return Ok(new { success = true, message = "Ödeme başarılı" });
        // }
        //
        // [HttpGet("fail")]
        // public IActionResult PaymentFail([FromQuery] string orderId, [FromQuery] string error)
        // {
        //     return View("PaymentFail", new PaymentResultViewModel
        //     {
        //         IsSuccess = false,
        //         OrderId = orderId,
        //         Message = "Ödeme işlemi başarısız oldu. Lütfen tekrar deneyiniz."
        //     });
        // }
        //
        // [HttpPost("refund")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> RefundPayment([FromBody] RefundPaymentRequest request)
        // {
        //     // Refund flow burada ele alınabilir.
        // }
    }

    public class PaytrInitRequest
    {
        public int OrderId { get; set; }
        public Guid? GuestIdentifier { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailUrl { get; set; }
    }
}
