using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Sipariş oluşturma (checkout) akışını yürütür; kullanıcı oluşturma/giriş ve ödeme bilgilerinin işlenmesini kapsar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : BaseApiController
    {
        private readonly IOrderService _orderService;

        private readonly ApplicationDbContext _db;

        public OrderController(IOrderService orderService, ApplicationDbContext db)
        {
            _orderService = orderService;
            _db = db;
        }

        private static bool IsPersonalizedOrderItem(OrderItem item)
        {
            var productName = item.ProductName ?? string.Empty;
            var variant = item.VariantSnapshot ?? string.Empty;

            return productName.Contains("İsimli", StringComparison.OrdinalIgnoreCase)
                || productName.Contains("isimli", StringComparison.OrdinalIgnoreCase)
                || variant.Contains("İsim:", StringComparison.OrdinalIgnoreCase)
                || variant.Contains("isim:", StringComparison.OrdinalIgnoreCase)
                || variant.Contains("Kişiselleştirme:", StringComparison.OrdinalIgnoreCase)
                || variant.Contains("kişiselleştirme:", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
                return Unauthorized(DataResponse<object>.CreateFailure("Geçerli kullanıcı oturumu bulunamadı."));

            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            var result = orders.OrderByDescending(order => order.CreatedAt).Select(order => new CustomerOrderSummary(
                order.Id,
                order.OrderNumber ?? order.Id.ToString(),
                order.CreatedAt,
                order.TotalAmount,
                order.OrderStatus.ToString(),
                order.PaymentStatus?.ToString(),
                order.CargoTracking)).ToList();

            return Ok(DataResponse<List<CustomerOrderSummary>>.CreateSuccess(result));
        }

        [HttpGet("mine/{orderId:int}")]
        [Authorize]
        public async Task<IActionResult> GetMyOrder(int orderId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
                return Unauthorized(DataResponse<object>.CreateFailure("Geçerli kullanıcı oturumu bulunamadı."));

            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null) return NotFound(DataResponse<object>.CreateFailure("Sipariş bulunamadı."));
            if (order.UserId != userId) return Forbid();

            var customerItems = order.OrdersItems?.Select(item => new CustomerOrderItem(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.ProductImageUrl ?? item.Product?.ProductImages
                    ?.OrderBy(image => image.SortOrder)
                    .Select(image => image.Image?.Url)
                    .FirstOrDefault(),
                item.Quantity,
                item.Price,
                item.ListUnitPrice,
                item.ProductDiscountTotal,
                item.VariantSnapshot,
                item.IsGift,
                !item.IsGift && !IsPersonalizedOrderItem(item),
                IsPersonalizedOrderItem(item) ? "İsimli ve kişiselleştirilmiş ürünlerde iade veya değişim yapılmaz." : null)).ToList() ?? [];

            var result = new CustomerOrderDetail(
                order.Id,
                order.OrderNumber ?? order.Id.ToString(),
                order.CreatedAt,
                order.TotalAmount,
                order.ShippingAmount,
                order.CampaignDiscountTotal,
                order.OrderStatus.ToString(),
                order.PaymentStatus?.ToString(),
                order.CargoTracking,
                order.CanBeReturned && customerItems.Any(item => item.IsReturnable),
                new CustomerOrderAddressDto(order.ShippingAddress?.Country, order.ShippingAddress?.City, order.ShippingAddress?.District, order.ShippingAddress?.Neighborhood, order.ShippingAddress?.AddressLine, order.ShippingAddress?.PostalCode),
                customerItems);

            return Ok(DataResponse<CustomerOrderDetail>.CreateSuccess(result));
        }

        [HttpPost("mine/{orderId:int}/return-request")]
        [Authorize]
        public async Task<IActionResult> CreateReturnRequest(int orderId, [FromBody] CreateReturnRequest request)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId))
                return Unauthorized(DataResponse<object>.CreateFailure("Geçerli kullanıcı oturumu bulunamadı."));
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 300)
                return BadRequest(BaseResponse.CreateFailure("İade nedeni zorunludur ve en fazla 300 karakter olabilir."));
            if (request.Note?.Length > 2000) return BadRequest(BaseResponse.CreateFailure("İade notu en fazla 2000 karakter olabilir."));

            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            if (order.UserId != userId) return Forbid();
            if (!order.CanBeReturned) return BadRequest(BaseResponse.CreateFailure("Bu sipariş iade süresi veya teslimat koşulunu sağlamıyor."));
            if (await _db.OrderReturnRequests.AnyAsync(item => item.OrderId == orderId && (item.Status == ReturnRequestStatus.Pending || item.Status == ReturnRequestStatus.Approved || item.Status == ReturnRequestStatus.Received)))
                return BadRequest(BaseResponse.CreateFailure("Bu sipariş için açık bir iade talebi zaten var."));

            var requestedItems = request.Items?.Where(item => item.Quantity > 0).GroupBy(item => item.OrderItemId).Select(group => new ReturnRequestItemInput(group.Key, group.Sum(item => item.Quantity))).ToList() ?? [];
            if (requestedItems.Count == 0) return BadRequest(BaseResponse.CreateFailure("İade edilecek en az bir ürün seçin."));
            var orderItems = order.OrdersItems?.Where(item => !item.IsGift).ToDictionary(item => item.Id) ?? [];
            if (requestedItems.Any(item => !orderItems.TryGetValue(item.OrderItemId, out var orderItem) || item.Quantity > orderItem.Quantity)) return BadRequest(BaseResponse.CreateFailure("Seçilen iade kalemlerinden biri geçersiz."));
            if (requestedItems.Any(item => IsPersonalizedOrderItem(orderItems[item.OrderItemId])))
                return BadRequest(BaseResponse.CreateFailure("İsimli ve kişiselleştirilmiş ürünlerde iade veya değişim yapılamaz. Ayıplı ya da siparişten farklı teslim edilen ürünler için müşteri hizmetleriyle iletişime geçin."));
            var returnRequest = new OrderReturnRequest { OrderId = orderId, Reason = request.Reason.Trim(), CustomerNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(), Items = requestedItems.Select(item => new OrderReturnRequestItem { OrderItemId = item.OrderItemId, Quantity = item.Quantity }).ToList() };
            _db.OrderReturnRequests.Add(returnRequest);
            await _db.SaveChangesAsync();
            return Ok(DataResponse<CustomerReturnRequestResponse>.CreateSuccess(new(returnRequest.Id, returnRequest.Status.ToString(), returnRequest.CreatedAt), "İade talebiniz incelenmek üzere alındı."));
        }

        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Geçersiz değer." : e.ErrorMessage)
                    .ToList();

                return BadRequest(DataResponse<CheckoutResultDto>.CreateFailure("Validasyon hatası.", errors));
            }

            try
            {
                var result = await _orderService.ProcessCheckoutAsync(request);
                var response = new CheckoutResultDto
                {
                    Order = result.Order.ToDto(),
                    JwtToken = result.JwtToken,
                    UserCreated = result.UserCreated,
                    UserLoggedIn = result.UserLoggedIn,
                    AuthenticatedUser = (result.UserCreated || result.UserLoggedIn) && result.Order.UserId.HasValue
                        ? new OrderUserInfoDto
                        {
                            UserId = result.Order.UserId.Value,
                            FirstName = result.UserFirstName,
                            LastName = result.UserLastName,
                            Email = result.UserEmail
                        }
                        : null
                };

                return Ok(DataResponse<CheckoutResultDto>.CreateSuccess(response, "Sipariş başarıyla oluşturuldu."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(DataResponse<CheckoutResultDto>.CreateFailure(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(DataResponse<CheckoutResultDto>.CreateFailure(ex.Message));
            }
        }

        [HttpGet("{orderId:int}/payment-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null) return NotFound(DataResponse<object>.CreateFailure("Sipariş bulunamadı."));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isOwner = order.UserId.HasValue
                ? int.TryParse(currentUserId, out var userId) && userId == order.UserId.Value
                : Guid.TryParse(Request.Headers["X-Guest-Identifier"].FirstOrDefault(), out var guestId) && order.GuestIdentifier == guestId;
            if (!isOwner) return Forbid();

            return Ok(DataResponse<OrderPaymentStatusResponse>.CreateSuccess(new OrderPaymentStatusResponse(
                order.Id, order.OrderNumber ?? order.Id.ToString(), order.TotalAmount, order.PaymentStatus ?? PaymentStatus.Unpaid, order.OrderStatus)));
        }

        [HttpPost("{orderId:int}/attribution")]
        [AllowAnonymous]
        public async Task<IActionResult> SaveAttribution(int orderId, [FromBody] SaveOrderAttributionRequest request)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isOwner = order.UserId.HasValue ? int.TryParse(currentUserId, out var userId) && userId == order.UserId.Value : Guid.TryParse(Request.Headers["X-Guest-Identifier"].FirstOrDefault(), out var guestId) && order.GuestIdentifier == guestId;
            if (!isOwner) return Forbid();
            var attribution = await _db.OrderAttributions.FirstOrDefaultAsync(item => item.OrderId == orderId);
            if (attribution == null) { attribution = new OrderAttribution { OrderId = orderId }; _db.OrderAttributions.Add(attribution); }
            attribution.VisitorId = Trim(request.VisitorId, 120); attribution.Source = Trim(request.Source, 120); attribution.Medium = Trim(request.Medium, 120); attribution.Campaign = Trim(request.Campaign, 250); attribution.Content = Trim(request.Content, 250); attribution.Term = Trim(request.Term, 250); attribution.LandingPath = Trim(request.LandingPath, 500);
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Attribution kaydedildi."));
        }

        private static string? Trim(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
        //[HttpGet("{orderId}")]
        //public async Task<IActionResult> GetOrderWithDetails(int orderId)
        //{
        //    try
        //    {
        //        var order = await _orderService.GetOrderWithDetailsAsync(orderId);
        //        if (order == null)
        //        {
        //            return NotFound(new { Message = "Sipariş bulunamadı." });
        //        }


        //        var orderDto = new OrderDto
        //        {
        //            Id = order.Id,
        //            UserId = order.UserId,
        //            GuestIdentifier = order.GuestIdentifier,
        //            OrderNumber = order.OrderNumber,
        //            CustomerFirstName=order.CustomerFirstName,
        //            CustomerLastName=order.CustomerLastName,
        //            CustomerEmail=order.CustomerEmail,
        //            CustomerPhone=order.CustomerPhone,
        //            ShoppingCartId = order.ShoppingCartId,
        //            TotalAmount = order.TotalAmount,
        //            PaymentMethodId = order.PaymentMethodId ?? 0,
        //            ShippingMethodId = order.ShippingMethodId ?? 0,
        //           OrderStatusId=order.OrderStatusId,
        //    CargoTracking =order.CargoTracking,

        //ShippingAddress = new AddressDto
        //            {
        //                Country = order.ShippingAddress.Country,
        //                City = order.ShippingAddress.City,
        //                District = order.ShippingAddress.District,
        //                AddressLine = order.ShippingAddress.AddressLine,
        //                PostalCode = order.ShippingAddress.PostalCode
        //            },

        //            BillingAddress = new AddressDto
        //            {
        //                Country = order.BillingAddress.Country,
        //                City = order.BillingAddress.City,
        //                District = order.BillingAddress.District,
        //                AddressLine = order.BillingAddress.AddressLine,
        //                PostalCode = order.BillingAddress.PostalCode
        //            },

        //            OrdersItems = order.OrdersItems.Select(item => new OrderItemDto
        //            {
        //                ProductName = item.Product?.Name,
        //                Quantity = item.Quantity,
        //                UnitPrice = item.Price,
        //                ProductImageUrl = item.Product.ProductImages
        //        .Where(a => a.SortOrder == 0)
        //        .Select(a => a.Image.Url)
        //        .FirstOrDefault()
        //            }).ToList()

        //        };

        //        return Ok(new DataResponse<OrderDto>
        //        {
        //            Success = true,
        //            Message = "Sipariş detayları başarıyla getirildi.",
        //            Data = orderDto
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new DataResponse<OrderDto>
        //        {
        //            Success = false,
        //            Message = $"Bir hata oluştu: {ex.Message}",
        //            Data = null
        //        });
        //    }
        //}

        //[HttpGet("paged-orders")]
        //public async Task<ActionResult<PagedResponse<OrderAll>>> GetOrders([FromQuery] OrderFilterDto filter)
        //{
        //    try
        //    {
        //        var orders = await _orderService.GetAllOrdersWithDetailsAsync(filter.PageNumber, filter.PageSize);
        //        var orderDtos = orders.Select(o => new OrderAll
        //        {
        //            Id = o.Id,
        //            OrderNumber = o.OrderNumber,
        //            TotalAmount = o.TotalAmount,
        //            CreateDate = o.CreatedAt,
        //            OrderStatusName = o.OrderStatus.ToString(),
        //            FullName=o.CustomerFirstName+" "+o.CustomerLastName,

        //        }).ToList();

        //        var totalCount = await _orderService.CountAsync();

        //        var response = new PagedResponse<OrderAll>
        //        {
        //            Success = true,
        //            Message = "Siparişler başarıyla getirildi",
        //            Items = orderDtos,
        //            PageNumber = filter.PageNumber,
        //            PageSize = filter.PageSize,
        //            TotalCount = totalCount,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        //        };

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        // İstersen loglama ekleyebilirsin burada
        //        return StatusCode(500, new PagedResponse<OrderAll>
        //        {
        //            Success = false,
        //            Message = "Bir hata oluştu: " + ex.Message,
        //            Items = null,
        //            PageNumber = filter.PageNumber,
        //            PageSize = filter.PageSize,
        //            TotalCount = 0,
        //            TotalPages = 0
        //        });
        //    }
        //}




        //[HttpPost("create-and-pay")]
        //public async Task<IActionResult> CreateAndPay([FromBody] CreateOrderDto orderDto)
        //{
        //    try
        //    {
        //        PaymentResult paymentResult;

        //        if (User.Identity?.IsAuthenticated == true)
        //        {
        //            // Kullanıcı bilgilerini DTO'ya ekle
        //            var userIdClaim = User.FindFirst("sub")?.Value;
        //            if (int.TryParse(userIdClaim, out var userId))
        //            {
        //                orderDto.UserId = userId;
        //            }

        //            paymentResult = await _orderService.CreateOrderAndProcessPaymentForUserAsync(orderDto);
        //        }
        //        else
        //        {
        //            paymentResult = await _orderService.CreateOrderAndProcessPaymentAsync(orderDto);
        //        }

        //        return Ok(new { orderId = paymentResult.OrderId, paymentStatus = "success" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}

        //[HttpPut("update-status")]
        //public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto updateDto)
        //{
        //    try
        //    {
        //        await _orderService.UpdateOrderStatusAsync(updateDto.OrderId, (OrderStatus)updateDto.NewStatus, updateDto.OrderTracking);
        //        return Ok(new
        //        {
        //            Success = true,
        //            Message = "Sipariş durumu başarıyla güncellendi."
        //        });
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        return NotFound(new
        //        {
        //            Success = false,
        //            Message = ex.Message
        //        });
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return BadRequest(new
        //        {
        //            Success = false,
        //            Message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Success = false,
        //            Message = $"Bir hata oluştu: {ex.Message}"
        //        });
        //    }
        //}


    }

    public record OrderPaymentStatusResponse(int OrderId, string OrderNumber, decimal TotalAmount, PaymentStatus PaymentStatus, OrderStatus OrderStatus);
    public record CustomerOrderSummary(int Id, string OrderNumber, DateTime CreatedAt, decimal TotalAmount, string Status, string? PaymentStatus, string? CargoTracking);
    public record CustomerOrderAddressDto(string? Country, string? City, string? District, string? Neighborhood, string? AddressLine, string? PostalCode);
    public record CustomerOrderItem(int OrderItemId, int ProductId, string ProductName, string? ProductImageUrl, int Quantity, decimal UnitPrice, decimal ListUnitPrice, decimal ProductDiscountTotal, string? VariantSnapshot, bool IsGift, bool IsReturnable, string? ReturnRestrictionReason);
    public record CustomerOrderDetail(int Id, string OrderNumber, DateTime CreatedAt, decimal TotalAmount, decimal ShippingAmount, decimal CampaignDiscountTotal, string Status, string? PaymentStatus, string? CargoTracking, bool CanBeReturned, CustomerOrderAddressDto ShippingAddress, List<CustomerOrderItem> Items);
    public record CreateReturnRequest(string Reason, string? Note, List<ReturnRequestItemInput>? Items);
    public record ReturnRequestItemInput(int OrderItemId, int Quantity);
    public record CustomerReturnRequestResponse(int Id, string Status, DateTime CreatedAt);
    public record SaveOrderAttributionRequest(string? VisitorId, string? Source, string? Medium, string? Campaign, string? Content, string? Term, string? LandingPath);

    public class OrderFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IsArchived { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public int OrderId { get; set; }
        public int NewStatus { get; set; }
        public string OrderTracking { get; set; }
    }


}
