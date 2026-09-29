# Manuel Mapping Geliştirme Rehberi

Bu projede entity ve DTO dönüşümleri AutoMapper yerine açık C# extension metotlarıyla yapılır.

## Konum ve adlandırma

- Yeni mapper, ait olduğu iş alanına göre `ECommerce.Service/Mapping/Manual/` altına eklenir.
- Dönüşüm adları yönü belirtir: `ToDto`, `ToListDto`, `ToEntity` ve `ApplyTo`.
- Birden çok DTO varsa dönüşüm adı DTO’nun amacını belirtir: örneğin `ToEditDto`.

## Uygulama kuralları

- Entity'den DTO'ya dönüşüm extension metodu olarak yazılır: `public static ProductDto ToDto(this Product source)`.
- Create DTO'ları için yeni entity üretilir: `dto.ToEntity()`.
- Update DTO'ları mevcut entity üzerine uygulanır: `dto.ApplyTo(entity)`.
- Audit alanları (`CreatedAt`, `CreatedBy`) ve ilişkiler, açıkça istenmedikçe update işleminde değiştirilmez.
- Navigation property'ler null güvenli ele alınır. Koleksiyonlar boş liste, tekil ilişkiler null döner.
- İlişkili veri gerekiyorsa sorguda ilgili navigation'ların yüklendiğinden emin olunur.

## Controller ve service kullanımı

```csharp
var entity = request.ToEntity();
var created = await service.AddAsync(entity);
return Ok(created.ToDto());

request.ApplyTo(existingEntity);
await service.UpdateAsync(existingEntity);
```

Controller veya service içinde dönüşüm için tekrar nesne başlatmak yerine ortak mapper extension'ı kullanılır.

## Test standardı

Her yeni mapper için `ECommerce.Tests/Mapping` altında en az şu senaryolar test edilir:

1. Normal dönüşümde tüm sözleşme alanları doğru gelir.
2. Navigation property yüklenmemişken dönüşüm hata vermez.
3. Update dönüşümü yalnızca izin verilen alanları değiştirir.

Testlerden sonra `dotnet test ECommerce.Tests/ECommerce.Tests.csproj --no-restore` çalıştırılır.
