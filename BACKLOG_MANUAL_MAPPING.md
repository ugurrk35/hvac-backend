# AutoMapper'dan Manuel Maplemeye Geçiş Backlog'u

## Amaç

`AutoMapper` bağımlılığını ve lisans gereksinimini kaldırmak; entity/DTO dönüşümlerini açık, test edilebilir C# extension metotlarıyla gerçekleştirmek.

Mevcut `ECommerce.Service/Mapping/MappingProfile.cs` dosyasında yaklaşık **50** `CreateMap` tanımı bulunuyor. Geçiş, uygulama davranışını değiştirmeden ve alanların eksik eşlenmesini önleyecek testlerle kademeli yapılmalıdır.

## Kabul Kriterleri

- API, Service ve test projelerinde `AutoMapper` paket veya `using AutoMapper` referansı kalmaz.
- `builder.Services.AddAutoMapper(...)` kaydı kaldırılır.
- Tüm endpoint'ler aynı DTO sözleşmelerini üretmeye devam eder.
- Özellikle ilişkili alanlar (ürün görseli, etiketler, sipariş kalemleri, adresler) korunur.
- Her mapping grubu için en az birim testleri bulunur.
- `dotnet test` ve `dotnet build` başarıyla tamamlanır.
- `dotnet list package --vulnerable` AutoMapper uyarısını göstermez.

## Uygulama İlkeleri

- Her iş alanı için `ECommerce.Service/Mapping/Manual/<Alan>Mappings.cs` altında extension metotları yazılır.
- Dönüşüm yönü adla açık edilir: `ToDto`, `ToEntity`, `ApplyUpdate`.
- Güncelleme DTO'ları yeni entity üretmek yerine mevcut entity üzerine uygulanır: `request.ApplyTo(entity)`.
- Navigation property boş olabileceğinden null güvenliği her mapper'da açıkça sağlanır.
- Controller/service içinde doğrudan nesne başlatmak yerine ortak mapper extension'ı kullanılır.

## Backlog

| ID | Durum | Öncelik | İş paketi | Tahmin | Bağımlılık | Bitti sayılma ölçütü |
|---|---|---|---|---:|---|---|
| MAP-01 | Yapıldı | P0 | Manuel mapping altyapısını oluştur | 0.5 gün | — | `Manual` klasörü ve `ProductCategoryMappings` extension yapısı hazırdır. |
| MAP-02 | Yapıldı | P0 | Mapping envanteri ve çağrı noktalarını çıkar | 0.5 gün | MAP-01 | 50 `CreateMap` tanımı envantere alındı; `IMapper` çağrılarının bulunduğu katmanlar belirlendi. |
| MAP-03 | Yapıldı | P0 | Ürün, kategori ve fiyat maplemelerini taşı | 2 gün | MAP-01, MAP-02 | Ürün create/update, fiyat, görsel/kombinasyon, mağaza/admin ürün listeleme-detay ve kategori akışları manuel maplemeye geçirildi. |
| MAP-04 | Yapıldı | P0 | Ürün öznitelik ve kombinasyon maplemelerini taşı | 2 gün | MAP-03 | Attribute, value, combination ve personalization alanları null güvenli manuel mapper'lara taşındı. |
| MAP-05 | Yapıldı | P0 | Sipariş ve adres maplemelerini taşı | 1.5 gün | MAP-01 | Sipariş kalemleri, ödeme/kargo kimlikleri, fatura ve teslimat adresleri manuel mapper'lara taşındı. |
| MAP-06 | Yapıldı | P1 | Blog ve kullanıcı maplemelerini taşı | 1 gün | MAP-01 | Blog kategori/yazı/etiket/yorum ve `ApplicationUser → UserDto` dönüşümleri manuel mapper'larla tamamlandı. |
| MAP-07 | Yapıldı | P1 | Görsel ve ürün etiketi maplemelerini taşı | 1 gün | MAP-03 | Image, ProductImage ve ProductTag dönüşümleri manuel mapper'lara taşındı. |
| MAP-08 | Yapıldı | P1 | Servis ve controller bağımlılıklarını kaldır | 1.5 gün | MAP-03..MAP-07 | Tüm `IMapper` constructor parametreleri ve `_mapper.Map` çağrıları kaldırıldı. |
| MAP-09 | Yapıldı | P0 | Mapping regresyon testlerini yaz | 2 gün | MAP-03..MAP-07 | Normal, null navigation ve update senaryolarını kapsayan manuel mapping testleri çalışıyor. |
| MAP-10 | Yapıldı | P0 | AutoMapper paketi ve DI kaydını kaldır | 0.5 gün | MAP-08, MAP-09 | API, Service ve Tests projelerinden paket/referansları, DI kaydı ve MappingProfile silindi. |
| MAP-11 | Yapıldı | P1 | Uçtan uca API smoke testi | 1 gün | MAP-10 | Yerel PostgreSQL ile sağlık, kategori ve ürün özniteliği endpoint'leri beklenen JSON sözleşmesiyle doğrulandı. |
| MAP-12 | Yapıldı | P2 | Mapping standartlarını dokümante et | 0.5 gün | MAP-10 | Yeni DTO'lar için kısa geliştirme rehberi eklendi. |

Toplam tahmin: **12–14 iş günü**. MAP-03–MAP-07, farklı dosyalara ayrıldığı için test standardı belirlendikten sonra paralel yürütülebilir.

## Önerilen Sıra

1. MAP-01, MAP-02 ve test standardı
2. MAP-03, MAP-04, MAP-05 (kritik satış ve katalog akışları)
3. MAP-06, MAP-07
4. MAP-08, MAP-09
5. MAP-10, MAP-11, MAP-12

## Riskler ve Önlemler

| Risk | Önlem |
|---|---|
| AutoMapper'ın varsayılan olarak eşlediği bir alanın unutulması | Her mapper için beklenen alanları doğrulayan birim testi yazılır. |
| Navigation property yüklenmemişken null reference hatası | Mapper'larda null-safe erişim ve ilgili endpoint sorgularında `Include` kontrolü yapılır. |
| Update isteğinin audit/ilişki alanlarını yanlışlıkla ezmesi | `ApplyUpdate` metotları yalnızca izinli alanları günceller. |
| Büyük bir PR'ın incelemesinin zorlaşması | Her iş alanı bağımsız PR/commit olarak teslim edilir. |

## İlk Teslim Dilimi

İlk dilim tamamlandı: MAP-01, MAP-02 ve MAP-03 yapıldı. Ürün/kategori/fiyat dönüşümleri için kullanılan manuel mapping deseni, sonraki iş alanlarının referans şablonudur.
