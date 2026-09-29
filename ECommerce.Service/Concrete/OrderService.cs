using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Dtos.OrderDtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class OrderService : Service<Order>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;



        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly IPriceQuoteService _priceQuoteService;

        public OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IUserService userService, IPriceQuoteService priceQuoteService)
            : base(orderRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _priceQuoteService = priceQuoteService;
        }

        public async Task<List<Order>> GetAllOrdersWithDetailsAsync(int pageNumber, int pageSize)
        {
            return await _orderRepository.GetAllOrdersWithDetailsAsync(pageNumber, pageSize);
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _orderRepository.GetOrderWithDetailsAsync(orderId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _orderRepository.GetOrdersByUserIdAsync(userId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByGuestIdAsync(Guid guestIdentifier)
        {
            return await _orderRepository.GetOrdersByGuestIdAsync(guestIdentifier);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _orderRepository.GetOrdersByStatusAsync(status);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersBetweenDatesAsync(DateTime start, DateTime end)
        {
            return await _orderRepository.GetOrdersBetweenDatesAsync(start, end);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersAboveAmountAsync(decimal minTotal)
        {
            return await _orderRepository.GetOrdersAboveAmountAsync(minTotal);
        }

        public async Task<IReadOnlyList<Order>> GetRecentOrdersAsync(int count = 10)
        {
            return await _orderRepository.GetRecentOrdersAsync(count);
        }

        public async Task<Order?> GetLastOrderForUserAsync(int userId)
        {
            return await _orderRepository.GetLastOrderForUserAsync(userId);
        }

        public async Task<CheckoutProcessResult> ProcessCheckoutAsync(CheckoutRequestDto request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Order == null)
                throw new ArgumentException("Sipariş bilgileri eksik.");

            switch (request.Mode)
            {
                case CheckoutMode.Guest:
                    request.Order.UserId = null;
                    request.Order.GuestIdentifier = request.GuestIdentifier ?? request.Order.GuestIdentifier;
                    var guestOrder = await CreateGuestOrderFromCartAsync(request.Order);
                    return new CheckoutProcessResult
                    {
                        Order = guestOrder,
                        UserCreated = false,
                        UserLoggedIn = false,
                        UserFirstName = request.Order.CustomerFirstName,
                        UserLastName = request.Order.CustomerLastName,
                        UserEmail = request.Order.CustomerEmail
                    };

                case CheckoutMode.ExistingUser:
                {
                    if (request.Login == null)
                        throw new ArgumentException("Giriş bilgileri sağlanmalıdır.");

                    var loginResult = await AttemptLoginAsync(request.Login);
                    if (loginResult.UserId == null)
                        throw new InvalidOperationException("Kullanıcı doğrulanamadı.");

                    var normalizedCartId = await TransferCartOwnershipAsync(request.Order.ShoppingCartId, request.GuestIdentifier, loginResult.UserId.Value);

                    request.Order.UserId = loginResult.UserId;
                    request.Order.GuestIdentifier = null;
                    request.Order.ShoppingCartId = normalizedCartId;

                    var userOrder = await CreateOrderFromCartAsync(request.Order);
                    return new CheckoutProcessResult
                    {
                        Order = userOrder,
                        JwtToken = loginResult.Token,
                        UserLoggedIn = true,
                        UserCreated = false,
                        UserFirstName = userOrder.CustomerFirstName,
                        UserLastName = userOrder.CustomerLastName,
                        UserEmail = userOrder.CustomerEmail
                    };
                }

                case CheckoutMode.Register:
                {
                    if (request.Registration == null)
                        throw new ArgumentException("Kayıt bilgileri sağlanmalıdır.");

                    var registrationResult = await RegisterUserAsync(request.Registration);
                    var normalizedCartId = await TransferCartOwnershipAsync(request.Order.ShoppingCartId, request.GuestIdentifier, registrationResult.UserId);
                    request.Order.UserId = registrationResult.UserId;
                    request.Order.GuestIdentifier = null;
                    request.Order.ShoppingCartId = normalizedCartId;

                    var order = await CreateOrderFromCartAsync(request.Order);
                    return new CheckoutProcessResult
                    {
                        Order = order,
                        JwtToken = registrationResult.Token,
                        UserCreated = true,
                        UserLoggedIn = true,
                        UserFirstName = order.CustomerFirstName,
                        UserLastName = order.CustomerLastName,
                        UserEmail = order.CustomerEmail
                    };
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Mode), "Geçersiz checkout modu.");


            }


        }

        private async Task<(int? UserId, string Token)> AttemptLoginAsync(CheckoutLoginDto login)
        {
            var identifier = login.Identifier?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("E-posta veya telefon numarası gereklidir.");

            var loginResult = await _userService.LoginWithUserAsync(identifier, login.Password);
            if (loginResult == null)
                throw new InvalidOperationException("Giriş başarısız. Lütfen bilgilerinizi kontrol edin.");

            return (loginResult.Value.User.Id, loginResult.Value.Token);
        }

        private async Task<(int UserId, string Token)> RegisterUserAsync(CheckoutRegisterDto registration)
        {
            var username = registration.Email.Trim().ToLowerInvariant();

            var user = new ApplicationUser
            {
                UserName = username,
                Email = registration.Email,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                PhoneNumber = registration.PhoneNumber,
                IsGuest = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userService.CreateUserAsync(user, registration.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Kullanıcı oluşturulamadı: {errors}");
            }

            var token = await _userService.LoginAsync(username, registration.Password);
            if (token == null)
                throw new InvalidOperationException("Oturum açılamadı. Lütfen daha sonra tekrar deneyin.");

            return (user.Id, token);
        }

        private async Task<int> TransferCartOwnershipAsync(int cartId, Guid? guestIdentifier, int userId)
        {
            if (guestIdentifier.HasValue)
            {
                await _unitOfWork.ShoppingCartRepository.MergeGuestCartToUserAsync(guestIdentifier.Value, userId);
            }

            var cart = await _unitOfWork.ShoppingCartRepository.GetByIdAsync(cartId);
            if (cart == null)
                throw new InvalidOperationException("Sepet bulunamadı.");

            if (cart.UserId.HasValue && cart.UserId != userId)
                throw new InvalidOperationException("Sepet başka bir kullanıcıya ait.");

            if (!cart.UserId.HasValue)
            {
                cart.UserId = userId;
                cart.GuestIdentifier = null;
                cart.LastModifiedAt = DateTime.UtcNow;
                await _unitOfWork.ShoppingCartRepository.UpdateAsync(cart);
            }

            return cart.Id;
        }

        private Task<Order> CreateOrderFromCartAsync(CreateOrderDto orderDto) => CreateGuestOrderFromCartAsync(orderDto);
        //sipariş durumu güncelle
        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string orderTracking = null)
        {
            var order = newStatus == OrderStatus.Canceled
                ? await GetOrderWithDetailsAsync(orderId)
                : await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException("Sipariş bulunamadı.");

            var currentStatus = order.OrderStatus;

            if (currentStatus == newStatus)
            {
                if (orderTracking != null && !string.Equals(order.CargoTracking, orderTracking, StringComparison.Ordinal))
                {
                    order.CargoTracking = orderTracking;
                    await _unitOfWork.CompleteAsync();
                }
                return;
            }

            if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedStatuses)
                || !allowedStatuses.Contains(newStatus))
            {
                throw new InvalidOperationException($"Sipariş durumu '{currentStatus}' durumundan '{newStatus}' durumuna geçiş yapılamaz.");
            }

            if (newStatus == OrderStatus.Canceled && order.PaymentStatus == PaymentStatus.Paid)
                throw new InvalidOperationException("Ödenmiş sipariş doğrudan iptal edilemez; önce iade işlemi yapılmalıdır.");

            if (newStatus == OrderStatus.Shipped && string.IsNullOrWhiteSpace(orderTracking))
                throw new InvalidOperationException("Sipariş kargolanırken takip numarası zorunludur.");

            order.CargoTracking = orderTracking;
            order.OrderStatus = newStatus;
            if (newStatus == OrderStatus.Canceled && order.OrdersItems?.Any() == true)
                await RevertStockAsync(order.OrdersItems);
            // Durum geçmişini kaydet
            await AddStatusHistoryAsync(orderId, currentStatus, newStatus);
            await _unitOfWork.CompleteAsync();
        }

        private static readonly Dictionary<OrderStatus, List<OrderStatus>> AllowedTransitions = new()
{
    { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Canceled } },
    { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Canceled } },
    { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
    { OrderStatus.Delivered, new List<OrderStatus> { OrderStatus.Completed, OrderStatus.Returned } },
    { OrderStatus.Completed, new List<OrderStatus>() },
    { OrderStatus.Canceled, new List<OrderStatus>() },
    { OrderStatus.Returned, new List<OrderStatus>() },
};


        public async Task CancelOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException("Sipariş bulunamadı.");

            if (order.OrderStatus == OrderStatus.Canceled)
                throw new InvalidOperationException("Sipariş zaten iptal edilmiş.");

            // İptal edilebilir durum kontrolü
            var allowedCancelStatuses = new[] { OrderStatus.Pending, OrderStatus.Processing };
            if (!allowedCancelStatuses.Contains(order.OrderStatus))
                throw new InvalidOperationException($"Sipariş durumu '{order.OrderStatus}' iken iptal edilemez.");

            // Ödeme iadesi (varsa)
            //if (order.PaymentStatus == Domain.Entity.PaymentStatus.Paid)
            //{
            //    var refundResult = await _paymentService.RefundPaymentAsync(order.PaymentReference, order.TotalAmount);
            //    if (!refundResult.IsSuccess)
            //    {
            //        // İade başarısızsa logla, alert at veya retry mekanizması kur
            //        throw new InvalidOperationException("Ödeme iadesi başarısız: " + refundResult.ErrorMessage);
            //    }

            //    // Ödeme durumu iade olarak güncellenebilir
            //    await UpdatePaymentStatusAsync(order.Id, Domain.Entity.PaymentStatus.Refunded, null);
            //}

            // Sipariş durumunu iptal yap
            await UpdateOrderStatusAsync(orderId, OrderStatus.Canceled, null);
        }


        public async Task<string?> GetTrackingInfoAsync(int orderId)
        {
            return await _orderRepository.GetTrackingInfoAsync(orderId);
        }

        public async Task<string?> GetInvoiceNumberAsync(int orderId)
        {
            return await _orderRepository.GetInvoiceNumberAsync(orderId);
        }

        public async Task<bool> IsOrderReturnableAsync(int orderId)
        {
            return await _orderRepository.IsOrderReturnableAsync(orderId);
        }

        public async Task<OrderReportDto> GetOrderReportAsync(DateTime startDate, DateTime endDate)
        {
            return await _orderRepository.GetOrderReportAsync(startDate, endDate);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByPaymentMethodAsync(int paymentMethodId)
        {
            return await _orderRepository.GetOrdersByPaymentMethodAsync(paymentMethodId);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByShippingMethodAsync(int shippingMethodId)
        {
            return await _orderRepository.GetOrdersByShippingMethodAsync(shippingMethodId);
        }



        //public async Task<PaymentResult> CreateOrderAndProcessPaymentAsync(CreateOrderDto orderDto)
        //{
        //    Order createdOrder;

        //    // 1. Transaction: Sipariş oluşturma + stok düşme (kısa süreli)
        //    await _unitOfWork.BeginTransactionAsync();
        //    try
        //    {
        //        // Order entity map
        //        var order = _mapper.Map<Order>(orderDto);
        //        order.OrderNumber = await GenerateOrderNumberAsync();
        //        order.OrderStatus = OrderStatus.Pending;
        //        order.PaymentStatus = Domain.Entity.PaymentStatus.Unpaid; // Başlangıçta ödemesiz
        //        order.PaymentMethodId = orderDto.PaymentMethodId;
        //        order.ShippingMethodId = orderDto.ShippingMethodId;

        //        // Kullanıcı bilgisi veya misafir
        //        if (orderDto.UserId.HasValue && orderDto.UserId.Value > 0)
        //        {
        //            order.UserId = orderDto.UserId;
        //            order.GuestIdentifier = null;
        //        }
        //        else
        //        {
        //            order.UserId = null;
        //            order.GuestIdentifier = orderDto.GuestIdentifier ?? Guid.NewGuid();
        //        }

        //        // Sepeti getir
        //        var cart = await _unitOfWork.ShoppingCartRepository.GetCartWithItems(orderDto.ShoppingCartId);
        //        if (cart == null)
        //            throw new InvalidOperationException($"Sepet bulunamadı: ID = {orderDto.ShoppingCartId}");

        //        order.OrdersItems = new List<OrderItem>();

        //        // Her ürün için pessimistic lock al, stok kontrol et ve düş
        //        foreach (var cartItem in cart.CartItems)
        //        {
        //            var product = await _unitOfWork.ProductRepository.GetByIdForUpdateAsync(cartItem.ProductId);
        //            if (product == null)
        //                throw new InvalidOperationException($"Ürün bulunamadı: ID = {cartItem.ProductId}");

        //            if (product.Quantity < cartItem.Quantity)
        //                throw new InvalidOperationException(
        //                    $"Yetersiz stok! Ürün: {product.Name}, Stok: {product.Quantity}, İstenen: {cartItem.Quantity}");

        //            product.Quantity -= cartItem.Quantity;
        //            _unitOfWork.ProductRepository.UpdateAsync(product);

        //            // --- ProductPrice üzerinden fiyatı getir ---
        //            var productPrice = await _unitOfWork.ProductPriceRepository.GetLatestPriceAsync(
        //                cartItem.ProductId,
        //                cartItem.ProductAttributeCombinationId,
        //                cart.User?.CustomerGroupId // müşteri grubu fiyatı varsa
        //            );

        //            if (productPrice == null)
        //                throw new InvalidOperationException($"Ürün fiyatı bulunamadı: ProductId={cartItem.ProductId}");

        //            var finalPrice = productPrice.DiscountPrice ?? productPrice.Price;
        //            order.OrdersItems.Add(new OrderItem
        //            {
        //                ProductId = cartItem.ProductId,
        //                Quantity = cartItem.Quantity,
        //                Product = product,
        //                Price = finalPrice
        //            });
        //        }

        //        // Siparişi kaydet
        //        createdOrder = await _orderRepository.AddAsync(order);

        //        // Transaction commit (SaveChanges + Commit)
        //        await _unitOfWork.CommitAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        throw; // Dışarı at, üst katmanda log vs.
        //    }

        //    // 2. Ödeme işlemi: Transaction dışında (dış sistem bağlantısı)
        //    var paymentRequest = new PaymentRequest
        //    {
        //        OrderId = createdOrder.OrderNumber,
        //        Amount = createdOrder.TotalAmount,
        //        Description = "Sipariş ödemesi",
        //        CustomerName = $"{createdOrder.CustomerFirstName} {createdOrder.CustomerLastName}",
        //        CustomerEmail = createdOrder.CustomerEmail,
        //        CustomerPhone = createdOrder.CustomerPhone,
        //        CustomerAddress = createdOrder.ShippingAddress.AddressLine,
        //        CustomerCity = createdOrder.ShippingAddress.City,
        //        CustomerCountry = createdOrder.ShippingAddress.Country,
        //        SuccessUrl = "https://yourdomain.com/payment-success",
        //        FailUrl = "https://yourdomain.com/payment-fail",
        //        BasketItems = createdOrder.OrdersItems.Select(i => new BasketItem
        //        {
        //            Name = i.Product.Name,
        //            Price = i.Price,
        //            Quantity = i.Quantity
        //        }).ToList()
        //    };

        //    var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest, PaymentProvider.PayTR);

        //    // 3. Ödeme sonucunu DB’ye güncelle
        //    try
        //    {
        //        var mappedStatus = MapFromPayTRStatus(paymentResult.Status);
        //        await UpdatePaymentStatusAsync(createdOrder.Id, mappedStatus, paymentResult.ReferenceCode);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ödeme başarılı ama DB güncellemesi başarısız olabilir
        //        // Logla, alert at, manuel müdahale gerekebilir
        //        throw new InvalidOperationException("Ödeme sonucu DB'ye kaydedilemedi.", ex);
        //    }

        //    // 4. Ödeme başarısızsa özel işlem (örn. stok iadesi veya kullanıcı bilgilendirme)
        //    if (!paymentResult.IsSuccess)
        //    {
        //        // Ödeme başarısızsa stok iadesi yap
        //        await RevertStockAsync(createdOrder.OrdersItems);

        //        // Sipariş durumunu iptal et
        //        await UpdateOrderStatusAsync(createdOrder.Id, OrderStatus.Canceled);

        //        throw new InvalidOperationException("Ödeme başarısız: " + paymentResult.ErrorMessage);
                

        //        throw new InvalidOperationException("Ödeme başarısız: " + paymentResult.ErrorMessage);
        //    }

        //    return paymentResult;
        //}


        public async Task<Order> CreateGuestOrderFromCartAsync(CreateOrderDto orderDto)
        {
            if (orderDto == null)
                throw new ArgumentNullException(nameof(orderDto));

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var quote = await _priceQuoteService.GetCartQuoteAsync(orderDto.ShoppingCartId, orderDto.ShippingMethodId, orderDto.CouponCode, orderDto.ShippingAddress?.City, orderDto.ShippingAddress?.District);
                var cart = await _unitOfWork.ShoppingCartRepository.GetCartWithItems(orderDto.ShoppingCartId);
                if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                    throw new InvalidOperationException("Sepet bulunamadı veya boş.");

                if (cart.GuestIdentifier.HasValue && orderDto.GuestIdentifier != cart.GuestIdentifier)
                    throw new InvalidOperationException("Sepet farklı bir misafir oturumuna ait.");

                if (cart.UserId.HasValue && cart.UserId != orderDto.UserId)
                    throw new InvalidOperationException("Sepet farklı bir kullanıcıya ait.");

                var order = orderDto.ToEntity();
                order.OrderNumber = await GenerateOrderNumberAsync();
                order.OrderStatus = OrderStatus.Pending;
                order.PaymentStatus = Domain.Entity.PaymentStatus.Unpaid;
                order.GuestIdentifier = orderDto.UserId.HasValue ? null : orderDto.GuestIdentifier ?? cart.GuestIdentifier ?? Guid.NewGuid();
                order.UserId = orderDto.UserId ?? cart.UserId;
                order.OrdersItems = new List<OrderItem>();

                if (!order.PaymentMethodId.HasValue)
                    throw new InvalidOperationException("Ödeme yöntemi seçilmelidir.");

                var paymentMethod = await _unitOfWork.PaymentMethodRepository.GetByIdAsync(order.PaymentMethodId.Value);
                if (paymentMethod == null)
                    throw new InvalidOperationException($"Geçersiz ödeme yöntemi: {order.PaymentMethodId.Value}");

                order.PaymentMethod = paymentMethod;

                order.ShippingMethodId = quote.ShippingMethodId;

                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(cartItem.ProductId);
                    if (product == null)
                        throw new InvalidOperationException($"Ürün bulunamadı: {cartItem.ProductId}");

                    var quotedItem = quote.Items.Single(item => item.CartItemId == cartItem.Id);
                    order.OrdersItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Product = product,
                        Quantity = cartItem.Quantity,
                        Price = quotedItem.EffectiveUnitPrice,
                        ListUnitPrice = quotedItem.ListUnitPrice,
                        ProductDiscountTotal = quotedItem.LineDiscountTotal,
                        ProductName = product.Name,
                        ProductImageUrl = cartItem.Product?.ProductImages?.OrderBy(image => image.SortOrder).FirstOrDefault()?.Image?.Url ?? product.OgImage,
                        VariantSnapshot = string.Join("; ", cartItem.AttributeSelections.Select(selection => $"{selection.ProductAttribute?.Name}: {selection.ProductAttributeValue?.Value ?? selection.PersonalizationText}").Where(value => !value.EndsWith(": "))),
                        ProductCampaignPackageId = cartItem.ProductCampaignPackageId,
                        CampaignSnapshotJson = cartItem.CampaignSnapshotJson,
                        ProductAttributeCombinationId = cartItem.ProductAttributeCombinationId
                    });
                }

                foreach (var gift in quote.GiftItems)
                {
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(gift.ProductId);
                    if (product == null) throw new InvalidOperationException($"Hediye ürün bulunamadı: {gift.ProductId}");
                    order.OrdersItems.Add(new OrderItem { ProductId = gift.ProductId, Product = product, Quantity = gift.Quantity, Price = 0m, ListUnitPrice = product.BasePrice, ProductName = product.Name, IsGift = true });
                }

                await ValidateOrderItemsAsync(order.OrdersItems);
                await DeductStockAsync(order.OrdersItems);

                order.TotalAmount = quote.GrandTotal;
                order.ShippingAmount = quote.ShippingTotal;
                order.CampaignDiscountTotal = quote.CampaignDiscountTotal;
                order.AppliedCampaignIds = quote.AppliedCampaigns.Count == 0
                    ? null
                    : string.Join(',', quote.AppliedCampaigns.Select(campaign => campaign.CampaignId));

                var createdOrder = await _orderRepository.AddAsync(order);

                cart.IsOrdered = true;
                cart.TotalAmount = order.TotalAmount;
                cart.LastModifiedAt = DateTime.UtcNow;
                cart.UserId = order.UserId;
                cart.GuestIdentifier = order.GuestIdentifier;
                await _unitOfWork.ShoppingCartRepository.UpdateAsync(cart);

                await _unitOfWork.CommitAsync();

                return createdOrder;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }



        // Misafir kullanıcı siparişi için validasyonlu metot
        //public async Task<Order> CreateOrderForGuestAsync(Order order)
        //{
        //    if (order == null)
        //        throw new ArgumentNullException(nameof(order));

        //    if (order.GuestIdentifier == Guid.Empty)
        //        throw new ArgumentException("Misafir kullanıcı kimliği boş olamaz.");

        //    if (order.TotalAmount <= 0)
        //        throw new ArgumentException("Sipariş tutarı sıfırdan büyük olmalıdır.");

        //    // Stok kontrolü
        //    foreach (var item in order.OrdersItems)
        //    {
        //        var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductId);
        //        if (product == null)
        //            throw new KeyNotFoundException($"Ürün bulunamadı: {item.ProductId}");
        //        if (product.Quantity < item.Quantity)
        //            throw new InvalidOperationException($"Yetersiz stok: {product.Name}");
        //    }
        //    //_unitOfWork
        //    var createdOrder = await _orderRepository.AddAsync(order);
        //    await _unitOfWork.CompleteAsync();

        //    return createdOrder;
        //}
        // Kullanıcı siparişi için validasyonlu metot
        //public async Task<Order> CreateOrderForUserAsync(Order order)
        //{
        //    if (order == null)
        //        throw new ArgumentNullException(nameof(order));

        //    if (order.UserId == 0)
        //        throw new ArgumentException("Kullanıcı ID boş olamaz.");

        //    if (order.TotalAmount <= 0)
        //        throw new ArgumentException("Sipariş tutarı sıfırdan büyük olmalıdır.");

        //    // Stok kontrolü
        //    foreach (var item in order.OrdersItems)
        //    {
        //        var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductId);
        //        if (product == null)
        //            throw new KeyNotFoundException($"Ürün bulunamadı: {item.ProductId}");
        //        if (product.Quantity < item.Quantity)
        //            throw new InvalidOperationException($"Yetersiz stok: {product.Name}");
        //    }

        //    var createdOrder = await _orderRepository.AddAsync(order);
        //    await _unitOfWork.CompleteAsync();

        //    return createdOrder;
        //}
        //#region Sipariş Oluşturma Metodları (Geliştirilmiş)
        public async Task<Order> CreateOrderForGuestAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            await ValidateGuestOrderAsync(order);
            await ValidateOrderItemsAsync(order.OrdersItems);

            // Sipariş numarası oluştur
            order.OrderNumber = await GenerateOrderNumberAsync();
           
            order.OrderStatus = OrderStatus.Pending;

            // Stok düşür
            await DeductStockAsync(order.OrdersItems);

            var createdOrder = await _orderRepository.AddAsync(order);
            //await _unitOfWork.CompleteAsync();

            // Sipariş onay e-postası gönder
            //await SendOrderConfirmationAsync(createdOrder);

            return createdOrder;
        }

        //public async Task<PaymentResult> CreateOrderAndProcessPaymentForUserAsync(CreateOrderDto orderDto)
        //{
        //    await _unitOfWork.BeginTransactionAsync();

        //    try
        //    {
        //        // 1. Order entity map
        //        var order = _mapper.Map<Order>(orderDto);
        //        order.OrderNumber = await GenerateOrderNumberAsync();
        //        order.OrderStatus = OrderStatus.Pending;

        //        // UserId null olmamalı
        //        if (!order.UserId.HasValue)
        //            throw new Exception("Kullanıcı kimliği eksik.");

        //        await ValidateUserOrderAsync(order);
        //        // Fiyatları ProductPrice tablosundan çek
        //        foreach (var item in order.OrdersItems)
        //        {
        //            var productPrice = await _unitOfWork.ProductPriceRepository.GetLatestPriceAsync(
        //                item.ProductId,
        //                item.ProductAttributeCombinationId,
        //                order.User?.CustomerGroupId
        //            );

        //            if (productPrice == null)
        //                throw new InvalidOperationException($"Ürün fiyatı bulunamadı: ProductId={item.ProductId}");

        //            item.Price = productPrice.DiscountPrice ?? productPrice.Price;
        //        }
        //        await ValidateOrderItemsAsync(order.OrdersItems);

        //        // 2. Stok düş
        //        await DeductStockAsync(order.OrdersItems);

        //        // 3. Siparişi kaydet
        //        var createdOrder = await _orderRepository.AddAsync(order);

        //        // 4. PaymentRequest oluştur
        //        var paymentRequest = new PaymentRequest
        //        {
        //            OrderId = createdOrder.OrderNumber,
        //            Amount = createdOrder.TotalAmount,
        //            Description = "Sipariş ödemesi",
        //            CustomerName = createdOrder.CustomerFirstName + " " + createdOrder.CustomerLastName,
        //            CustomerEmail = createdOrder.CustomerEmail,
        //            CustomerPhone = createdOrder.CustomerPhone,
        //            CustomerAddress = createdOrder.ShippingAddress.AddressLine,
        //            CustomerCity = createdOrder.ShippingAddress.City,
        //            CustomerCountry = createdOrder.ShippingAddress.Country,
        //            SuccessUrl = "https://yourdomain.com/payment-success",
        //            FailUrl = "https://yourdomain.com/payment-fail",
        //            BasketItems = createdOrder.OrdersItems.Select(item => new BasketItem
        //            {
        //                Name = item.Product.Name,
        //                Price = item.Product.BasePrice,
        //                Quantity = item.Quantity
        //            }).ToList()
        //        };

        //        // 5. Ödeme işlemi
        //        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest, PaymentProvider.PayTR);

        //        if (!paymentResult.IsSuccess)
        //        {
        //            await RevertStockAsync(createdOrder.OrdersItems);

        //            // Sipariş durumunu iptal et
        //            await UpdateOrderStatusAsync(createdOrder.Id, OrderStatus.Canceled);

        //            throw new InvalidOperationException("Ödeme başarısız: " + paymentResult.ErrorMessage);


        //            throw new InvalidOperationException("Ödeme başarısız: " + paymentResult.ErrorMessage);
           
        //        }

        //        await _unitOfWork.CommitAsync();
        //        return paymentResult;
        //    }
        //    catch (Exception)
        //    {
        //        await _unitOfWork.RollbackAsync();
        //        throw;
        //    }
        //}

        //#endregion

        //#region Yeni Eklenen Metodlar

        //// Sipariş filtreleme ve sayfalama
        //public async Task<PagedResult<Order>> GetOrdersPagedAsync(OrderFilterDto filter, int page = 1, int pageSize = 10)
        //{
        //    return await _orderRepository.GetOrdersPagedAsync(filter, page, pageSize);
        //}

        //// Sipariş arama
        //public async Task<IReadOnlyList<Order>> SearchOrdersAsync(string searchTerm)
        //{
        //    return await _orderRepository.SearchOrdersAsync(searchTerm);
        //}

        //// Sipariş durumu geçmişi
        //public async Task<IReadOnlyList<OrderStatusHistory>> GetOrderStatusHistoryAsync(int orderId)
        //{
        //    return await _orderRepository.GetOrderStatusHistoryAsync(orderId);
        //}

        //// Sipariş iade işlemi
        //public async Task<OrderReturn> InitiateReturnAsync(int orderId, ReturnRequestDto returnRequest)
        //{
        //    var order = await GetOrderWithDetailsAsync(orderId);
        //    if (order == null)
        //        throw new KeyNotFoundException("Sipariş bulunamadı.");

        //    if (!await IsOrderReturnableAsync(orderId))
        //        throw new InvalidOperationException("Bu sipariş iade edilemez.");

        //    var orderReturn = new OrderReturn
        //    {
        //        OrderId = orderId,
        //        ReturnReason = returnRequest.Reason,
        //        ReturnDate = DateTime.UtcNow,
        //        ReturnStatus = ReturnStatus.Requested,
        //        ReturnAmount = returnRequest.ReturnAmount,
        //        Items = returnRequest.Items
        //    };

        //    await _unitOfWork.OrderReturnRepository.AddAsync(orderReturn);
        //    await _unitOfWork.CompleteAsync();

        //    return orderReturn;
        //}

        //// Sipariş istatistikleri
        //public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        //{
        //    return await _orderRepository.GetOrderStatisticsAsync(startDate, endDate);
        //}

        //// Müşteri sipariş geçmişi özet
        //public async Task<CustomerOrderSummaryDto> GetCustomerOrderSummaryAsync(int userId)
        //{
        //    return await _orderRepository.GetCustomerOrderSummaryAsync(userId);
        //}

        //// Sipariş kargo takibi güncelleme
        //public async Task UpdateTrackingInfoAsync(int orderId, string trackingNumber, string carrierName)
        //{
        //    var order = await _orderRepository.GetByIdAsync(orderId);
        //    if (order == null)
        //        throw new KeyNotFoundException("Sipariş bulunamadı.");

        //    order.TrackingNumber = trackingNumber;
        //    order.CarrierName = carrierName;
        //    order.UpdatedDate = DateTime.UtcNow;

        //    if (order.OrderStatus == OrderStatus.Processing)
        //    {
        //        order.OrderStatus = OrderStatus.Shipped;
        //        order.ShippedDate = DateTime.UtcNow;
        //    }

        //    await _unitOfWork.CompleteAsync();
        //}

        //// Sipariş teslimat onayı
        //public async Task ConfirmDeliveryAsync(int orderId)
        //{
        //    var order = await _orderRepository.GetByIdAsync(orderId);
        //    if (order == null)
        //        throw new KeyNotFoundException("Sipariş bulunamadı.");

        //    if (order.OrderStatus != OrderStatus.Shipped)
        //        throw new InvalidOperationException("Sadece kargoya verilmiş siparişler teslim edilebilir.");

        //    order.OrderStatus = OrderStatus.Delivered;
        //    order.DeliveredDate = DateTime.UtcNow;
        //    order.UpdatedDate = DateTime.UtcNow;

        //    await _unitOfWork.CompleteAsync();
        //}

        //// Sipariş notları
        //public async Task AddOrderNoteAsync(int orderId, string note, bool isCustomerVisible = false)
        //{
        //    var orderNote = new OrderNote
        //    {
        //        OrderId = orderId,
        //        Note = note,
        //        IsCustomerVisible = isCustomerVisible,
        //        CreatedDate = DateTime.UtcNow
        //    };

        //    await _unitOfWork.OrderNoteRepository.AddAsync(orderNote);
        //    await _unitOfWork.CompleteAsync();
        //}

        //public async Task<IReadOnlyList<OrderNote>> GetOrderNotesAsync(int orderId, bool customerVisibleOnly = false)
        //{
        //    return await _orderRepository.GetOrderNotesAsync(orderId, customerVisibleOnly);
        //}

        //// Toplu sipariş işlemleri
        public async Task BulkUpdateStatusAsync(List<int> orderIds, string newStatus)
        {
            if (!Enum.TryParse(typeof(OrderStatus), newStatus, true, out var statusObj))
                throw new ArgumentException("Geçersiz sipariş durumu");
            var status = (OrderStatus)statusObj;
            foreach (var id in orderIds)
            {
                var order = await GetByIdAsync(id);
                if (order != null)
                {
                    await UpdateOrderStatusAsync(id, status, order.CargoTracking);
                }
            }
        }

        //// Sipariş fatura oluşturma
        //public async Task<Invoice> GenerateInvoiceAsync(int orderId)
        //{
        //    var order = await GetOrderWithDetailsAsync(orderId);
        //    if (order == null)
        //        throw new KeyNotFoundException("Sipariş bulunamadı.");

        //    if (order.OrderStatus == OrderStatus.Canceled)
        //        throw new InvalidOperationException("İptal edilmiş sipariş için fatura oluşturulamaz.");

        //    var invoice = new Invoice
        //    {
        //        OrderId = orderId,
        //        InvoiceNumber = await GenerateInvoiceNumberAsync(),
        //        InvoiceDate = DateTime.UtcNow,
        //        TotalAmount = order.TotalAmount,
        //        TaxAmount = order.TaxAmount
        //    };

        //    await _unitOfWork.InvoiceRepository.AddAsync(invoice);
        //    await _unitOfWork.CompleteAsync();

        //    return invoice;
        //}

        // Ödeme durumu güncelleme
        public async Task<bool> UpdatePaymentStatusAsync(
     int orderId,
     PaymentStatus newPaymentStatus,
     string? paymentReference = null)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException("Sipariş bulunamadı.");

            // ✅ ZATEN PAID İSE BİR DAHA DOKUNMA (ÇİFT CALLBACK KORUMASI)
            if (order.PaymentStatus == PaymentStatus.Paid)
                return false;

            // A failed order releases its reservation and must be recreated for a new payment attempt.
            if (order.PaymentStatus == PaymentStatus.Failed)
                return false;

            // The provider can retry a failed callback as well; never return stock twice.
            if (order.PaymentStatus == newPaymentStatus)
                return false;

            var oldPaymentStatus = order.PaymentStatus;

            order.PaymentStatus = newPaymentStatus;
            order.PaymentReference = paymentReference;
            order.LastModifiedAt = DateTime.UtcNow;

            // ✅ SADECE ÖDEME BAŞARILIYSA SİPARİŞ İLERLESİN
            if (oldPaymentStatus != PaymentStatus.Paid &&
                newPaymentStatus == PaymentStatus.Paid &&
                order.OrderStatus == OrderStatus.Pending)
            {
                order.OrderStatus = OrderStatus.Processing;
            }

            if (oldPaymentStatus != PaymentStatus.Failed && newPaymentStatus == PaymentStatus.Failed)
            {
                var orderWithItems = await GetOrderWithDetailsAsync(orderId);
                if (orderWithItems?.OrdersItems?.Any() == true)
                    await RevertStockAsync(orderWithItems.OrdersItems);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }


        //// Kupon/İndirim uygulama
        //public async Task ApplyCouponAsync(int orderId, string couponCode)
        //{
        //    var order = await GetOrderWithDetailsAsync(orderId);
        //    if (order == null)
        //        throw new KeyNotFoundException("Sipariş bulunamadı.");

        //    var coupon = await _unitOfWork.CouponRepository.GetValidCouponAsync(couponCode);
        //    if (coupon == null)
        //        throw new InvalidOperationException("Geçersiz kupon kodu.");

        //    // Kupon kullanım koşullarını kontrol et
        //    await ValidateCouponUsageAsync(coupon, order);

        //    var discountAmount = CalculateDiscountAmount(coupon, order.TotalAmount);

        //    order.CouponCode = couponCode;
        //    order.DiscountAmount = discountAmount;
        //    order.TotalAmount -= discountAmount;
        //    order.UpdatedDate = DateTime.UtcNow;

        //    await _unitOfWork.CompleteAsync();
        //}
        //#endregion

        //#region Private Helper Methods
        private async Task<List<string>> ValidateGuestOrderAsync(Order order)
        {
            var errors = new List<string>();

            if (order.GuestIdentifier == Guid.Empty)
                errors.Add("Misafir kullanıcı kimliği boş olamaz.");

            // Minimum sipariş tutarı
            const decimal minOrderAmount = 10m;
            if (order.TotalAmount < minOrderAmount)
                errors.Add($"Sipariş tutarı en az {minOrderAmount} olmalıdır.");

            // Adres kontrolü
            if (order.ShippingAddress == null)
            {
                errors.Add("Teslimat adresi zorunludur.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(order.ShippingAddress.AddressLine))
                    errors.Add("Adres satırı zorunludur.");

                if (string.IsNullOrWhiteSpace(order.ShippingAddress.City))
                    errors.Add("Şehir bilgisi zorunludur.");

                if (string.IsNullOrWhiteSpace(order.ShippingAddress.Country))
                    errors.Add("Ülke bilgisi zorunludur.");
            }

            // E-posta kontrolü
            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                errors.Add("İletişim e-postası zorunludur.");
            }
            else
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(order.CustomerEmail);
                    if (addr.Address != order.CustomerEmail)
                        errors.Add("Geçersiz e-posta formatı.");
                }
                catch
                {
                    errors.Add("Geçersiz e-posta formatı.");
                }
            }

            // Telefon kontrolü
            if (string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                errors.Add("Telefon numarası zorunludur.");
            }
            else
            {
                var phoneDigits = new string(order.CustomerPhone.Where(char.IsDigit).ToArray());
                if (phoneDigits.Length < 10 || phoneDigits.Length > 15)
                    errors.Add("Telefon numarası geçerli değil. Lütfen alan kodu ile birlikte giriniz.");
            }

            // Sipariş kalemleri kontrolü
            if (order.OrdersItems == null || !order.OrdersItems.Any())
            {
                errors.Add("Sipariş kalemi bulunmuyor.");
            }
            else if (order.OrdersItems.Any(i => i.Quantity <= 0))
            {
                errors.Add("Sipariş kalemlerindeki adet 0'dan büyük olmalıdır.");
            }

            return await Task.FromResult(errors);
        }

        private async Task ValidateUserOrderAsync(Order order)
        {
            if (order.UserId == null || order.UserId <= 0)
                throw new ArgumentException("Geçerli bir kullanıcı kimliği sağlanmalıdır.");

            var user = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId.Value);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            if (user.IsGuest)
                throw new InvalidOperationException("Misafir kullanıcı ile sipariş oluşturulamaz.");

            if (!user.IsActive)
                throw new InvalidOperationException("Kullanıcı aktif değil.");

            if (order.TotalAmount <= 0)
                throw new ArgumentException("Sipariş tutarı sıfırdan büyük olmalıdır.");
        }


        private async Task ValidateOrderItemsAsync(ICollection<OrderItem> orderItems)
        {
            if (orderItems == null || !orderItems.Any())
                throw new ArgumentException("Sipariş en az bir ürün içermelidir.");

            foreach (var item in orderItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException($"Geçersiz ürün miktarı: ÜrünId={item.ProductId}");
            }

            var productIds = orderItems.Select(i => i.ProductId).Distinct().ToList();

            // Tüm ürünleri tek sorguda çek
            var products = await _unitOfWork.ProductRepository.GetAllByIdsAsync(productIds);

            var productDict = products.ToDictionary(p => p.Id);

            foreach (var item in orderItems)
            {
                if (!productDict.TryGetValue(item.ProductId, out var product))
                    throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");

                if (!product.IsActive)
                    throw new InvalidOperationException($"Pasif ürün sipariş edilemez: {product.Name}");

                if (product.Quantity < item.Quantity)
                    throw new InvalidOperationException($"Yetersiz stok: {product.Name}");
            }
        }




        private async Task DeductStockAsync(ICollection<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                // Pessimistic lock ile satırı alıyoruz
                var product = await _unitOfWork.ProductRepository.GetByIdForUpdateAsync(item.ProductId);

                if (product == null)
                    throw new InvalidOperationException($"Ürün bulunamadı: {item.ProductId}");

                if (product.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Yetersiz stok! ÜrünId: {product.Id}, Stok: {product.Quantity}, İstenen: {item.Quantity}"
                    );

                product.Quantity -= item.Quantity;
                await _unitOfWork.ProductRepository.UpdateAsync(product);
                // Db'ye yazma CommitAsync() sırasında yapılacak
            }
        }






        //private void ValidateStatusChange(OrderStatus currentStatus, OrderStatus newStatus)
        //{
        //    var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
        //    {
        //        { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Canceled } },
        //        { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Canceled } },
        //        { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
        //        { OrderStatus.Delivered, new List<OrderStatus>() }, // Teslim edilmiş siparişin durumu değiştirilemez
        //        { OrderStatus.Canceled, new List<OrderStatus>() } // İptal edilmiş siparişin durumu değiştirilemez
        //    };

        //    if (!validTransitions.ContainsKey(currentStatus) ||
        //        !validTransitions[currentStatus].Contains(newStatus))
        //    {
        //        throw new InvalidOperationException($"Sipariş durumu {currentStatus}'dan {newStatus}'a değiştirilemez.");
        //    }
        //}

        private async Task AddStatusHistoryAsync(int orderId, OrderStatus oldStatus, OrderStatus newStatus)
        {
            var history = new OrderStatusHistory
            {
                OrderId = orderId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow
            };

            await _unitOfWork.OrderStatusHistoryRepository.AddAsync(history);
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.UtcNow.Date; // UTC kullanıyoruz
            var prefix = $"ORD{today:yyyyMMdd}";
            var lastOrder = await _orderRepository.GetLastOrderByDateAsync(today);

            var sequence = 1;
            if (lastOrder != null)
            {
                var lastSequence = lastOrder.OrderNumber.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out var parsed))
                    sequence = parsed + 1;
            }

            return $"{prefix}{sequence:D4}";
        }


        //private async Task<string> GenerateInvoiceNumberAsync()
        //{
        //    var today = DateTime.Now;
        //    var prefix = $"INV{today:yyyyMM}";
        //    var lastInvoice = await _unitOfWork.InvoiceRepository.GetLastInvoiceByMonthAsync(today.Year, today.Month);

        //    var sequence = 1;
        //    if (lastInvoice != null)
        //    {
        //        var lastSequence = lastInvoice.InvoiceNumber.Substring(prefix.Length);
        //        if (int.TryParse(lastSequence, out var parsed))
        //            sequence = parsed + 1;
        //    }

        //    return $"{prefix}{sequence:D6}";
        //}

        //private async Task SendOrderConfirmationAsync(Order order)
        //{
        //    // E-posta gönderme servisi ile sipariş onay e-postası gönder
        //    // Bu kısım email service ile implement edilecek
        //    await Task.CompletedTask;
        //}

        //private async Task ValidateCouponUsageAsync(Coupon coupon, Order order)
        //{
        //    if (coupon.ExpiryDate < DateTime.UtcNow)
        //        throw new InvalidOperationException("Kupon süresi dolmuş.");

        //    if (coupon.MinOrderAmount > order.TotalAmount)
        //        throw new InvalidOperationException($"Minimum sipariş tutarı {coupon.MinOrderAmount:C} olmalıdır.");

        //    if (coupon.UsageLimit.HasValue)
        //    {
        //        var usageCount = await _unitOfWork.OrderRepository.GetCouponUsageCountAsync(coupon.Code);
        //        if (usageCount >= coupon.UsageLimit.Value)
        //            throw new InvalidOperationException("Kupon kullanım limiti aşılmış.");
        //    }
        //}

        //private decimal CalculateDiscountAmount(Coupon coupon, decimal orderAmount)
        //{
        //    if (coupon.DiscountType == DiscountType.Percentage)
        //    {
        //        return orderAmount * (coupon.DiscountValue / 100);
        //    }
        //    else
        //    {
        //        return Math.Min(coupon.DiscountValue, orderAmount);
        //    }
        //}
        //#endregion

        private async Task RevertStockAsync(ICollection<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                var product = await _unitOfWork.ProductRepository.GetByIdForUpdateAsync(item.ProductId);

                if (product == null)
                    continue; // Veya logla, kritik değil

                product.Quantity += item.Quantity;
                await _unitOfWork.ProductRepository.UpdateAsync(product);
            }
        }

    }
}
