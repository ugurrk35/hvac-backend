using ECommerce.Service.Abstract;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Domain.Entity;
using ECommerce.Repository.Data;
using ECommerce.Adaptor.Payment.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using ECommerce.API.Services;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Sipariş yönetimi: listeleme, arşivleme, durum ve kargo takip güncellemeleri,
    /// tekil sipariş detayını getirme işlemleri.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _db;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminOrderController> _logger;
        public AdminOrderController(IOrderService orderService, ApplicationDbContext db, IPaymentProvider paymentProvider, IEmailService emailService, ILogger<AdminOrderController> logger)
        {
            _orderService = orderService;
            _db = db;
            _paymentProvider = paymentProvider;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("{orderId:int}/resend-payment-email")]
        public async Task<IActionResult> ResendPaymentEmail(int orderId)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null || order.IsDeleted) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            if (order.PaymentStatus != PaymentStatus.Paid) return BadRequest(BaseResponse.CreateFailure("Ödeme e-postası yalnızca ödenmiş siparişler için gönderilebilir."));
            if (string.IsNullOrWhiteSpace(order.CustomerEmail)) return BadRequest(BaseResponse.CreateFailure("Siparişe ait e-posta adresi bulunamadı."));
            if (await _db.EmailTemplates.AsNoTracking().AnyAsync(item => item.TemplateKey == "order-paid" && !item.IsActive, HttpContext.RequestAborted))
                return BadRequest(BaseResponse.CreateFailure("Ödeme e-posta şablonu pasif. Admin > E-posta Şablonları alanından 'Sipariş ödendi' şablonunu etkinleştirin."));

            _logger.LogInformation("Yönetici ödeme e-postasını yeniden gönderme isteği başlattı. Sipariş: {OrderId}, Alıcı: {Recipient}", orderId, order.CustomerEmail);
            if (!await _emailService.SendOrderPaidAsync(order, HttpContext.RequestAborted))
                return StatusCode(502, BaseResponse.CreateFailure("E-posta gönderilemedi. SMTP günlüklerini kontrol edin."));

            var trackedOrder = await _db.Orders.FirstAsync(item => item.Id == orderId);
            trackedOrder.PaymentEmailSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Ödeme e-postası yeniden gönderildi."));
        }

        [HttpPost("archive")]
        public async Task<ActionResult> Archive([FromBody] ArchiveOrdersRequest request)
        {
            if (request.OrderIds == null || request.OrderIds.Count == 0)
                return BadRequest(BaseResponse.CreateFailure("Arşivlenecek en az bir sipariş seçin."));
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
                return BadRequest(BaseResponse.CreateFailure("Arşiv nedeni zorunludur ve en fazla 1000 karakter olabilir."));

            var orders = await _db.Orders.Where(item => request.OrderIds.Distinct().Contains(item.Id) && !item.IsDeleted).ToListAsync();
            if (orders.Count != request.OrderIds.Distinct().Count())
                return NotFound(BaseResponse.CreateFailure("Siparişlerden biri bulunamadı veya zaten arşivlenmiş."));

            var archivableStatuses = new[] { OrderStatus.Canceled, OrderStatus.Returned };
            if (orders.Any(item => !archivableStatuses.Contains(item.OrderStatus)))
                return BadRequest(BaseResponse.CreateFailure("Yalnızca iptal edilmiş veya iadesi tamamlanmış siparişler arşivlenebilir."));

            var archivedByUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) ? userId : (int?)null;
            var archivedAt = DateTime.UtcNow;
            foreach (var order in orders)
            {
                order.IsDeleted = true;
                order.IsActive = false;
                order.ArchiveReason = request.Reason.Trim();
                order.ArchivedAt = archivedAt;
                order.ArchivedByUserId = archivedByUserId;
                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    OldStatus = order.OrderStatus,
                    NewStatus = order.OrderStatus,
                    ChangedAt = archivedAt,
                    EventType = "archive"
                });
            }
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Siparişler kalıcı olarak silinmeden arşive alındı."));
        }

        [HttpPost("{orderId:int}/restore")]
        public async Task<IActionResult> Restore(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(item => item.Id == orderId && item.IsDeleted);
            if (order == null) return NotFound(BaseResponse.CreateFailure("Arşivlenmiş sipariş bulunamadı."));
            var restoredAt = DateTime.UtcNow;
            order.IsDeleted = false;
            order.IsActive = true;
            order.ArchiveReason = null;
            order.ArchivedAt = null;
            order.ArchivedByUserId = null;
            _db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, OldStatus = order.OrderStatus, NewStatus = order.OrderStatus, ChangedAt = restoredAt, EventType = "restore" });
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess("Sipariş arşivden geri yüklendi."));
        }

        [HttpPost("bulk-update-status")]
        public async Task<ActionResult> BulkUpdateStatus([FromBody] BulkUpdateOrderStatusDto dto)
        {
            try
            {
                await _orderService.BulkUpdateStatusAsync(dto.OrderIds, dto.NewStatus);
                return Ok(BaseResponse.CreateSuccess("Seçili siparişlerin durumu güncellendi"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse.CreateFailure($"Toplu durum güncelleme sırasında hata: {ex.Message}"));
            }
        }

        [HttpPost("bulk-update-tracking")]
        public async Task<ActionResult> BulkUpdateTracking([FromBody] BulkUpdateTrackingDto dto)
        {
            // TODO: Implement bulk update tracking logic
            return Ok();
        }

        [HttpPost("{orderId:int}/refund")]
        public async Task<IActionResult> Refund(int orderId, [FromBody] RefundOrderRequest request)
        {
            if (!request.Confirmed) return BadRequest(BaseResponse.CreateFailure("İade işlemi açıkça onaylanmalıdır."));
            if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(BaseResponse.CreateFailure("İade tutarı ve nedeni zorunludur."));
            if (request.Reason.Trim().Length > 1000) return BadRequest(BaseResponse.CreateFailure("İade nedeni en fazla 1000 karakter olabilir."));
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 100) return BadRequest(BaseResponse.CreateFailure("İade işlem anahtarı geçersiz."));
            var idempotencyKey = request.IdempotencyKey.Trim();
            var previousRequest = await _db.OrderRefunds.FirstOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey);
            if (previousRequest != null)
            {
                if (previousRequest.OrderId != orderId || previousRequest.Amount != request.Amount) return Conflict(BaseResponse.CreateFailure("Bu işlem anahtarı farklı bir iade için kullanılmış."));
                return previousRequest.Status == RefundStatus.Succeeded
                    ? Ok(DataResponse<RefundResponse>.CreateSuccess(new(previousRequest.Id, previousRequest.Amount, previousRequest.Status, previousRequest.ReferenceNo)))
                    : BadRequest(BaseResponse.CreateFailure(previousRequest.ProviderResponse ?? "Bu iade isteği daha önce işlenmiş."));
            }
            var order = await _db.Orders.FirstOrDefaultAsync(item => item.Id == orderId && !item.IsDeleted);
            if (order == null) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            if (order.PaymentStatus != PaymentStatus.Paid) return BadRequest(BaseResponse.CreateFailure("Yalnızca ödenmiş sipariş iade edilebilir."));
            var refundedTotal = await _db.OrderRefunds.Where(item => item.OrderId == orderId && item.Status == RefundStatus.Succeeded).SumAsync(item => (decimal?)item.Amount) ?? 0m;
            if (request.Amount > order.TotalAmount - refundedTotal) return BadRequest(BaseResponse.CreateFailure("İade tutarı kalan tahsilatı aşamaz."));
            var refund = new OrderRefund { OrderId = orderId, Amount = request.Amount, Reason = request.Reason.Trim(), ReferenceNo = $"RF{orderId}{DateTime.UtcNow:yyyyMMddHHmmssfff}", IdempotencyKey = idempotencyKey, Status = RefundStatus.Pending };
            _db.OrderRefunds.Add(refund); await _db.SaveChangesAsync();
            var providerResult = await _paymentProvider.RefundAsync(new PaymentRefundRequest { OrderId = order.PaytrMerchantOid ?? order.Id.ToString(), Amount = refund.Amount, ReferenceNo = refund.ReferenceNo });
            refund.Status = providerResult.Success ? RefundStatus.Succeeded : RefundStatus.Failed; refund.ProviderResponse = providerResult.ProviderResponse ?? providerResult.ErrorMessage; refund.ProcessedAt = DateTime.UtcNow;
            if (providerResult.Success && refundedTotal + refund.Amount >= order.TotalAmount) order.PaymentStatus = PaymentStatus.Refunded;
            await _db.SaveChangesAsync();
            return providerResult.Success ? Ok(DataResponse<RefundResponse>.CreateSuccess(new(refund.Id, refund.Amount, refund.Status, refund.ReferenceNo))) : BadRequest(BaseResponse.CreateFailure(providerResult.ErrorMessage ?? "İade sağlayıcı tarafından reddedildi."));
        }

        [HttpGet("{orderId:int}/timeline")]
        public async Task<IActionResult> GetTimeline(int orderId)
        {
            var exists = await _db.Orders.AnyAsync(item => item.Id == orderId && !item.IsDeleted);
            if (!exists) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));

            var statusItems = await _db.OrderStatusHistories.AsNoTracking()
                .Where(item => item.OrderId == orderId)
                .Select(item => new OrderTimelineItem(item.ChangedAt, item.EventType ?? (item.OldStatus == item.NewStatus ? "archive" : "status"), item.EventType == "restore" ? "Sipariş arşivden geri yüklendi" : item.EventType == "archive" || item.OldStatus == item.NewStatus ? "Sipariş arşivlendi" : "Sipariş durumu güncellendi", item.EventType == "restore" ? "Sipariş tekrar aktif listeye alındı." : item.EventType == "archive" || item.OldStatus == item.NewStatus ? "Sipariş kalıcı olarak silinmeden arşive alındı." : $"{item.OldStatus} → {item.NewStatus}", item.CreatedBy))
                .ToListAsync();
            var refundItems = await _db.OrderRefunds.AsNoTracking()
                .Where(item => item.OrderId == orderId)
                .Select(item => new OrderTimelineItem(item.ProcessedAt ?? item.CreatedAt, "refund", item.Status == RefundStatus.Succeeded ? "İade tamamlandı" : item.Status == RefundStatus.Failed ? "İade başarısız" : "İade bekliyor", $"₺{item.Amount:N2} · {item.Reason} · {item.ReferenceNo}", item.CreatedBy))
                .ToListAsync();
            return Ok(DataResponse<List<OrderTimelineItem>>.CreateSuccess(statusItems.Concat(refundItems).OrderByDescending(item => item.OccurredAt).ToList()));
        }

        [HttpGet("{orderId:int}/notes")]
        public async Task<IActionResult> GetNotes(int orderId)
        {
            var exists = await _db.Orders.AnyAsync(item => item.Id == orderId && !item.IsDeleted);
            if (!exists) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            var notes = await _db.OrderNotes.AsNoTracking().Where(item => item.OrderId == orderId && !item.IsDeleted)
                .OrderByDescending(item => item.CreatedAt).Select(item => new OrderNoteResponse(item.Id, item.Content, item.CreatedAt, item.CreatedBy)).ToListAsync();
            return Ok(DataResponse<List<OrderNoteResponse>>.CreateSuccess(notes));
        }

        [HttpPost("{orderId:int}/notes")]
        public async Task<IActionResult> AddNote(int orderId, [FromBody] AddOrderNoteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Trim().Length > 2000) return BadRequest(BaseResponse.CreateFailure("Sipariş notu 1–2000 karakter olmalıdır."));
            var exists = await _db.Orders.AnyAsync(item => item.Id == orderId && !item.IsDeleted);
            if (!exists) return NotFound(BaseResponse.CreateFailure("Sipariş bulunamadı."));
            var note = new OrderNote { OrderId = orderId, Content = dto.Note.Trim() };
            _db.OrderNotes.Add(note);
            await _db.SaveChangesAsync();
            return Ok(DataResponse<OrderNoteResponse>.CreateSuccess(new(note.Id, note.Content, note.CreatedAt, note.CreatedBy)));
        }

        [HttpGet("return-requests")]
        public async Task<IActionResult> GetReturnRequests([FromQuery] ReturnRequestStatus? status = null)
        {
            var query = _db.OrderReturnRequests.AsNoTracking().Include(item => item.Order).Where(item => !item.IsDeleted);
            if (status.HasValue) query = query.Where(item => item.Status == status.Value);
            var items = await query.Include(item => item.Items).ThenInclude(item => item.OrderItem).OrderByDescending(item => item.CreatedAt).Select(item => new AdminReturnRequestResponse(
                item.Id, item.OrderId, item.Order!.OrderNumber ?? item.OrderId.ToString(), item.Order!.CustomerFirstName, item.Order!.CustomerLastName,
                item.Reason, item.CustomerNote, item.Status, item.AdminNote, item.CreatedAt, item.ReviewedAt, item.ReturnLabelUrl, item.ReturnTrackingNumber, item.Items.Select(x => new ReturnItemDto(x.OrderItemId, x.OrderItem!.ProductName, x.Quantity, x.OrderItem!.VariantSnapshot)).ToList())).ToListAsync();
            return Ok(DataResponse<List<AdminReturnRequestResponse>>.CreateSuccess(items));
        }

        [HttpPut("return-requests/{requestId:int}")]
        public async Task<IActionResult> UpdateReturnRequest(int requestId, [FromBody] UpdateReturnRequestRequest request)
        {
            if (!Enum.IsDefined(request.Status) || request.Status == ReturnRequestStatus.Pending)
                return BadRequest(BaseResponse.CreateFailure("Geçerli bir sonuç durumu seçin."));
            if (request.AdminNote?.Length > 2000) return BadRequest(BaseResponse.CreateFailure("Yönetici notu en fazla 2000 karakter olabilir."));
            if (!IsSafeExternalUrl(request.ReturnLabelUrl)) return BadRequest(BaseResponse.CreateFailure("İade etiketi için yalnızca geçerli HTTP/HTTPS URL kullanılabilir."));
            var item = await _db.OrderReturnRequests.FirstOrDefaultAsync(value => value.Id == requestId && !value.IsDeleted);
            if (item == null) return NotFound(BaseResponse.CreateFailure("İade talebi bulunamadı."));
            item.Status = request.Status;
            item.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
            item.ReturnLabelUrl = string.IsNullOrWhiteSpace(request.ReturnLabelUrl) ? null : request.ReturnLabelUrl.Trim();
            item.ReturnTrackingNumber = string.IsNullOrWhiteSpace(request.ReturnTrackingNumber) ? null : request.ReturnTrackingNumber.Trim();
            item.ReviewedAt = DateTime.UtcNow;
            item.ReviewedByUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var userId) ? userId : null;
            await _db.SaveChangesAsync();
            return Ok(DataResponse<AdminReturnRequestResponse>.CreateSuccess(new(item.Id, item.OrderId, string.Empty, null, null, item.Reason, item.CustomerNote, item.Status, item.AdminNote, item.CreatedAt, item.ReviewedAt, item.ReturnLabelUrl, item.ReturnTrackingNumber, [])));
        }

        private static bool IsSafeExternalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return value.Length <= 2048
                && Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                && string.IsNullOrEmpty(uri.UserInfo);
        }
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderWithDetails(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { Message = "Sipariş bulunamadı." });
                }


                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    GuestIdentifier = order.GuestIdentifier,
                    OrderNumber = order.OrderNumber,
                    CustomerFirstName = order.CustomerFirstName,
                    CustomerLastName = order.CustomerLastName,
                    CustomerEmail = order.CustomerEmail,
                    CustomerPhone = order.CustomerPhone,
                    Notes = order.Notes,
                    ShoppingCartId = order.ShoppingCartId,
                    TotalAmount = order.TotalAmount,
                    ShippingAmount = order.ShippingAmount,
                    CampaignDiscountTotal = order.CampaignDiscountTotal,
                    PaymentMethodId = order.PaymentMethodId ?? 0,
                    PaymentMethodName = order.PaymentMethod?.Name,
                    PaymentStatusId = order.PaymentStatusId,
                    PaymentReference = order.PaymentReference,
                    RefundedAmount = await _db.OrderRefunds.Where(item => item.OrderId == order.Id && item.Status == RefundStatus.Succeeded).SumAsync(item => (decimal?)item.Amount) ?? 0m,
                    ShippingMethodId = order.ShippingMethodId ?? 0,
                    ShippingMethodName = order.ShippingMethod?.Name,
                    OrderStatusId = order.OrderStatusId,
                    CargoTracking = order.CargoTracking,

                    ShippingAddress = new AddressDto
                    {
                        Country = order.ShippingAddress?.Country ?? string.Empty,
                        City = order.ShippingAddress?.City ?? string.Empty,
                        District = order.ShippingAddress?.District ?? string.Empty,
                        AddressLine = order.ShippingAddress?.AddressLine ?? string.Empty,
                        PostalCode = order.ShippingAddress?.PostalCode ?? string.Empty
                    },

                    BillingAddress = new AddressDto
                    {
                        Country = order.BillingAddress?.Country ?? string.Empty,
                        City = order.BillingAddress?.City ?? string.Empty,
                        District = order.BillingAddress?.District ?? string.Empty,
                        AddressLine = order.BillingAddress?.AddressLine ?? string.Empty,
                        PostalCode = order.BillingAddress?.PostalCode ?? string.Empty
                    },

                    OrdersItems = order.OrdersItems.Select(item => new OrderItemDto
                    {
                        ProductName = item.ProductName ?? item.Product?.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        ListUnitPrice = item.ListUnitPrice,
                        ProductDiscountTotal = item.ProductDiscountTotal,
                        VariantSnapshot = item.VariantSnapshot,
                        ProductCampaignPackageId = item.ProductCampaignPackageId,
                        CampaignSnapshotJson = item.CampaignSnapshotJson,
                        ProductImageUrl = item.ProductImageUrl ?? item.Product?.ProductImages
                            ?.OrderBy(image => image.SortOrder)
                            .Select(image => image.Image?.Url)
                            .FirstOrDefault()
                    }).ToList()

                };
                orderDto.RefundableAmount = Math.Max(0m, orderDto.TotalAmount - orderDto.RefundedAmount);

                return Ok(new DataResponse<OrderDto>
                {
                    Success = true,
                    Message = "Sipariş detayları başarıyla getirildi.",
                    Data = orderDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new DataResponse<OrderDto>
                {
                    Success = false,
                    Message = $"Bir hata oluştu: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpGet("paged-orders")]
        public async Task<ActionResult<PagedResponse<OrderAll>>> GetOrders([FromQuery] OrderFilterDto filter)
        {
            try
            {
                var pageNumber = Math.Max(1, filter.PageNumber);
                var pageSize = Math.Clamp(filter.PageSize, 1, 100);
                var query = BuildFilteredOrdersQuery(filter);

                var totalCount = await query.CountAsync();
                var orderDtos = await query.OrderByDescending(o => o.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(o => new OrderAll
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalAmount = o.TotalAmount,
                    CreateDate = o.CreatedAt,
                    OrderStatusName = o.OrderStatus.ToString(),
                    FullName = o.CustomerFirstName + " " + o.CustomerLastName,
                }).ToListAsync();

                var response = new PagedResponse<OrderAll>
                {
                    Success = true,
                    Message = "Siparişler başarıyla getirildi",
                    Items = orderDtos,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // İstersen loglama ekleyebilirsin burada
                return StatusCode(500, new PagedResponse<OrderAll>
                {
                    Success = false,
                    Message = "Bir hata oluştu: " + ex.Message,
                    Items = null,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalCount = 0,
                    TotalPages = 0
                });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportOrders([FromQuery] OrderFilterDto filter)
        {
            var query = BuildFilteredOrdersQuery(filter);

            var rows = await query.OrderByDescending(order => order.CreatedAt).Select(order => new { order.OrderNumber, order.CustomerFirstName, order.CustomerLastName, order.CustomerEmail, order.TotalAmount, order.ShippingAmount, order.CampaignDiscountTotal, order.OrderStatusId, order.CreatedAt }).ToListAsync();
            var csv = new StringBuilder("Sipariş No,Müşteri,E-posta,Tutar,Kargo,Kampanya İndirimi,Durum,Tarih\r\n");
            foreach (var row in rows)
                csv.AppendJoin(',', CsvCell(row.OrderNumber), CsvCell($"{row.CustomerFirstName} {row.CustomerLastName}".Trim()), CsvCell(row.CustomerEmail), CsvCell(row.TotalAmount.ToString("0.00")), CsvCell(row.ShippingAmount.ToString("0.00")), CsvCell(row.CampaignDiscountTotal.ToString("0.00")), CsvCell(((OrderStatus)row.OrderStatusId).ToString()), CsvCell(row.CreatedAt.ToString("yyyy-MM-dd HH:mm"))).Append("\r\n");
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", $"siparisler-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetOrderSummary([FromQuery] OrderFilterDto filter)
        {
            var query = BuildFilteredOrdersQuery(filter);
            var summary = new OrderListSummary(
                await query.CountAsync(),
                await query.SumAsync(order => (decimal?)order.TotalAmount) ?? 0m,
                await query.CountAsync(order => order.OrderStatus == OrderStatus.Pending || order.OrderStatus == OrderStatus.Processing),
                await query.CountAsync(order => order.OrderStatus == OrderStatus.Shipped));
            return Ok(DataResponse<OrderListSummary>.CreateSuccess(summary));
        }

        private IQueryable<Order> BuildFilteredOrdersQuery(OrderFilterDto filter)
        {
            var query = _db.Orders.AsNoTracking().Where(order => order.IsDeleted == filter.IsArchived);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(order => (order.OrderNumber ?? string.Empty).ToLower().Contains(search)
                    || (order.CustomerFirstName ?? string.Empty).ToLower().Contains(search)
                    || (order.CustomerLastName ?? string.Empty).ToLower().Contains(search)
                    || (order.CustomerEmail ?? string.Empty).ToLower().Contains(search));
            }
            if (filter.Status.HasValue && Enum.IsDefined(typeof(OrderStatus), filter.Status.Value)) query = query.Where(order => order.OrderStatusId == filter.Status.Value);
            if (filter.FromDate.HasValue) query = query.Where(order => order.CreatedAt >= filter.FromDate.Value.Date);
            if (filter.ToDate.HasValue) query = query.Where(order => order.CreatedAt < filter.ToDate.Value.Date.AddDays(1));
            return query;
        }

        private static string CsvCell(string? value)
        {
            var safe = value ?? string.Empty;
            if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }




       

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto updateDto)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(updateDto.OrderId, (OrderStatus)updateDto.NewStatus, updateDto.OrderTracking);
                return Ok(new
                {
                    Success = true,
                    Message = "Sipariş durumu başarıyla güncellendi."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Bir hata oluştu: {ex.Message}"
                });
            }
        }
        //[HttpPost("export")]
        //public async Task<ActionResult> Export([FromBody] OrderExportFilterDto filter)
        //{
        //    // TODO: Implement export logic
        //    return Ok();
        //}

        //[HttpPost("import")]
        //public async Task<ActionResult> Import([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
        //{
        //    // TODO: Implement import logic
        //    return Ok();
        //}

        //[HttpGet("search")]
        //public async Task<ActionResult> Search([FromQuery] string searchTerm)
        //{
        //    // TODO: Implement search logic
        //    return Ok();
        //}

        //[HttpPost("{id}/add-note")]
        //public async Task<ActionResult> AddNote(int id, [FromBody] AddOrderNoteDto dto)
        //{
        //    // TODO: Implement add note logic
        //    return Ok();
        //}
    }

    // Placeholder DTOs
    public class BulkUpdateOrderStatusDto { public List<int> OrderIds { get; set; } public string NewStatus { get; set; } }
    public class BulkUpdateTrackingDto { public List<int> OrderIds { get; set; } public string TrackingNumber { get; set; } }
    public record ArchiveOrdersRequest(List<int> OrderIds, string Reason);
    public class OrderExportFilterDto { public int? Status { get; set; } public string? Customer { get; set; } }
    public class AddOrderNoteDto { public string Note { get; set; } }
    public record OrderNoteResponse(int Id, string Content, DateTime CreatedAt, string? CreatedBy);
    public record RefundOrderRequest(decimal Amount, string Reason, bool Confirmed, string IdempotencyKey);
    public record RefundResponse(int Id, decimal Amount, RefundStatus Status, string ReferenceNo);
    public record OrderTimelineItem(DateTime OccurredAt, string Type, string Title, string Description, string? Actor);
    public record OrderListSummary(int TotalCount, decimal TotalAmount, int NeedsAttentionCount, int ShippedCount);
    public record UpdateReturnRequestRequest(ReturnRequestStatus Status, string? AdminNote, string? ReturnLabelUrl, string? ReturnTrackingNumber);
    public record ReturnItemDto(int OrderItemId, string ProductName, int Quantity, string? VariantSnapshot);
    public record AdminReturnRequestResponse(int Id, int OrderId, string OrderNumber, string? CustomerFirstName, string? CustomerLastName, string Reason, string? CustomerNote, ReturnRequestStatus Status, string? AdminNote, DateTime CreatedAt, DateTime? ReviewedAt, string? ReturnLabelUrl, string? ReturnTrackingNumber, List<ReturnItemDto> Items);
}
