using ECommerce.Domain.Entity;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using ECommerce.Service.Dtos.UserDtos;
using ECommerce.Service.Dtos.ImageDtos;

namespace ECommerce.Service.Mapping.Manual;

public static class OrderAttributeMappings
{
    public static UserDto ToDto(this ApplicationUser source) => new() { Id = source.Id, Email = source.Email ?? string.Empty, FirstName = source.FirstName, LastName = source.LastName, IsActive = source.IsActive, CreatedAt = source.CreatedAt };
    public static ImageDto ToDto(this Image source) => new() { Id = source.Id, Title = source.Title, AltText = source.AltText ?? string.Empty, Caption = source.Caption ?? string.Empty, Url = source.Url, Width = source.Width, Height = source.Height, FileExtension = source.FileExtension, SizeInBytes = source.SizeInBytes };
    public static ProductAttributeDto ToDto(this ProductAttribute source) => new()
    {
        Id = source.Id, Name = source.Name, IsPersonalizationText = source.IsPersonalizationText,
        TextPrompt = source.TextPrompt, MaxLength = source.MaxLength,
        ProductAttributeValues = source.ProductAttributeValues?.Select(value => value.ToDto()).ToList() ?? new()
    };

    public static ProductAttributeValueDto ToDto(this ProductAttributeValue source) => new()
    {
        Id = source.Id, Value = source.Value, PriceModifier = source.PriceModifier
    };

    public static ProductAttribute ToEntity(this ProductAttributeCreateDto source) => new()
    {
        Name = source.Name, IsPersonalizationText = source.IsPersonalizationText,
        TextPrompt = source.TextPrompt, MaxLength = source.MaxLength,
        ProductAttributeValues = source.ProductAttributeValues?.Select(value => value.ToEntity()).ToList() ?? new List<ProductAttributeValue>()
    };

    public static ProductAttributeValue ToEntity(this ProductAttributeValueCreateDto source) => new()
    {
        Value = source.Value, PriceModifier = source.PriceModifier
    };

    public static ProductAttribute ToEntity(this ProductAttributeDto source) => new()
    {
        Id = source.Id, Name = source.Name, IsPersonalizationText = source.IsPersonalizationText,
        TextPrompt = source.TextPrompt, MaxLength = source.MaxLength,
        ProductAttributeValues = source.ProductAttributeValues?.Select(value => new ProductAttributeValue { Id = value.Id, Value = value.Value, PriceModifier = value.PriceModifier }).ToList() ?? new List<ProductAttributeValue>()
    };

    public static void ApplyTo(this ProductAttributeUpdateDto source, ProductAttribute target)
    {
        target.Name = source.Name; target.IsPersonalizationText = source.IsPersonalizationText;
        target.TextPrompt = source.TextPrompt; target.MaxLength = source.MaxLength;
        target.ProductAttributeValues = source.ProductAttributeValues?.Select(value => new ProductAttributeValue { Id = value.Id, Value = value.Value, PriceModifier = value.PriceModifier, ProductAttributeId = target.Id }).ToList() ?? new List<ProductAttributeValue>();
    }

    public static AddressDto ToDto(this Address? source) => new()
    {
        Country = source?.Country, City = source?.City, District = source?.District,
        Neighborhood = source?.Neighborhood, AddressLine = source?.AddressLine, PostalCode = source?.PostalCode
    };

    public static Address ToEntity(this AddressDto source) => new()
    {
        Country = source.Country, City = source.City, District = source.District,
        Neighborhood = source.Neighborhood, AddressLine = source.AddressLine, PostalCode = source.PostalCode
    };

    public static Order ToEntity(this CreateOrderDto source) => new()
    {
        UserId = source.UserId, GuestIdentifier = source.GuestIdentifier, ShoppingCartId = source.ShoppingCartId,
        TotalAmount = source.TotalAmount, PaymentMethodId = source.PaymentMethodId, ShippingMethodId = source.ShippingMethodId,
        CustomerFirstName = source.CustomerFirstName, CustomerLastName = source.CustomerLastName,
        CustomerEmail = source.CustomerEmail, CustomerPhone = source.CustomerPhone, Notes = source.Notes,
        ShippingAddress = source.ShippingAddress.ToEntity(), BillingAddress = source.BillingAddress.ToEntity()
    };

    public static OrderDto ToDto(this Order source) => new()
    {
        Id = source.Id, UserId = source.UserId, GuestIdentifier = source.GuestIdentifier, OrderNumber = source.OrderNumber ?? string.Empty,
        CustomerFirstName = source.CustomerFirstName ?? string.Empty, CustomerLastName = source.CustomerLastName ?? string.Empty,
        CustomerEmail = source.CustomerEmail ?? string.Empty, CustomerPhone = source.CustomerPhone ?? string.Empty,
        Notes = source.Notes,
        ShoppingCartId = source.ShoppingCartId, CargoTracking = source.CargoTracking ?? string.Empty,
        TotalAmount = source.TotalAmount, ShippingAmount = source.ShippingAmount, CampaignDiscountTotal = source.CampaignDiscountTotal,
        PaymentMethodId = source.PaymentMethodId ?? 0, PaymentMethodName = source.PaymentMethod?.Name,
        PaymentStatusId = source.PaymentStatusId, PaymentReference = source.PaymentReference,
        ShippingMethodId = source.ShippingMethodId ?? 0, ShippingMethodName = source.ShippingMethod?.Name,
        OrderStatusId = source.OrderStatusId, ShippingAddress = source.ShippingAddress.ToDto(), BillingAddress = source.BillingAddress.ToDto(),
        OrdersItems = source.OrdersItems?.Select(item => new OrderItemDto { ProductId = item.ProductId, ProductName = item.ProductName, ProductImageUrl = item.ProductImageUrl ?? string.Empty, Quantity = item.Quantity, UnitPrice = item.Price, ListUnitPrice = item.ListUnitPrice, ProductDiscountTotal = item.ProductDiscountTotal, VariantSnapshot = item.VariantSnapshot }).ToList() ?? new()
    };
}
