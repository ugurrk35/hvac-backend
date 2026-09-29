using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos.ImageDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Dtos.ProductDtos.Collection;
using ECommerce.Repository.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{

    public class ProductService : Service<Product>, IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork, ApplicationDbContext dbContext)
            : base(productRepository, unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;

        }
        public async Task<List<ProductResponseCollectionDto>> GetCollectionProductsAsync(string collection, string currency)
        {
            var products = await _unitOfWork.ProductRepository
                .GetPublishedProductsByCollectionAsync(collection);

            return products.Select(p => new ProductResponseCollectionDto
            {
                Id = p.Id.ToString(),
                Handle = p.Slug,
                Title = p.Name,
                Description = p.Description,
                FeaturedImage = new ImageCollectionDto
                {
                    Url = p.ProductImages
         .Select(pi => pi.Image?.Url)
         .FirstOrDefault(u => u != null)
      ?? string.Empty
                },
                PriceRange = new PriceRangeCollectionDto
                {
                    MaxVariantPrice = new VariantPriceCollectionDto
                    {
                        Amount = (p.DiscountPrice ?? p.BasePrice).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                        CurrencyCode = currency
                    },
                    MinVariantPrice = null // opsiyonel
                }
            }).ToList();
        }
        /// <summary>
        /// Ürün adı ile arama yapar (kısmi veya tam eşleşme).
        /// </summary>
        /// <param name="searchTerm">Aranacak kelime veya ifade.</param>
        /// <returns>Eşleşen ürünlerin listesi.</returns>
        public async Task<IReadOnlyList<Product>> SearchByNameAsync(string searchTerm)
        {
            return await _productRepository.SearchByNameAsync(searchTerm);
        }

        /// <summary>
        /// Belirtilen kategori kimliğine göre ürünleri getirir.
        /// </summary>
        /// <param name="categoryId">Kategori Id'si.</param>
        /// <returns>İlgili kategoriye ait ürün listesi.</returns>
        public async Task<IReadOnlyList<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _productRepository.GetByCategoryIdAsync(categoryId);
        }

        /// <summary>
        /// Belirtilen fiyat aralığındaki ürünleri getirir.
        /// </summary>
        /// <param name="minPrice">Minimum fiyat.</param>
        /// <param name="maxPrice">Maksimum fiyat.</param>
        /// <returns>Fiyat aralığındaki ürünler.</returns>
        public async Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _productRepository.GetByPriceRangeAsync(minPrice, maxPrice);
        }

        /// <summary>
        /// Stokta bulunan ürünlerin listesini döner.
        /// </summary>
        /// <returns>Stokta bulunan ürünler.</returns>
        public async Task<IReadOnlyList<Product>> GetInStockProductsAsync()
        {
            return await _productRepository.GetInStockProductsAsync();
        }

        /// <summary>
        /// En çok sipariş edilen veya popüler ürünlerin listesini döner.
        /// </summary>
        /// <param name="topCount">Kaç ürün getirileceği.</param>
        /// <returns>Popüler ürünler listesi.</returns>
        public async Task<IReadOnlyList<Product>> GetPopularProductsAsync(int topCount)
        {
            return await _productRepository.GetPopularProductsAsync(topCount);
        }

        /// <summary>
        /// Ürün detaylarını, ilişkili tüm verilerle birlikte getirir.
        /// </summary>
        /// <param name="productId">Ürün Id'si.</param>
        /// <returns>Detaylı ürün nesnesi.</returns>
        public async Task<Product> GetProductDetailsAsync(int productId)
        {
            //var cacheKey = $"Product_{productId}";

            //if (!_cache.TryGetValue(cacheKey, out Product cachedProduct))
            //{
                var product = await _productRepository.GetProductDetailsAsync(productId);
                if (product == null) return null;

                //var cacheEntryOptions = new MemoryCacheEntryOptions()
                //    .SetSlidingExpiration(TimeSpan.FromDays(CacheDurationInDays));

                //_cache.Set(cacheKey, product, cacheEntryOptions);

                return product;
            //}

            //return cachedProduct;
        }

        /// <summary>
        /// Ürün stok miktarını günceller.
        /// </summary>
        /// <param name="productId">Stok güncellenecek ürün Id'si.</param>
        /// <param name="quantityChange">Stok değişim miktarı (pozitif veya negatif).</param>
        /// <returns>Görev tamamlanınca döner.</returns>
        public async Task UpdateStockAsync(int productId, int quantityChange)
        {
            await _productRepository.UpdateStockAsync(productId, quantityChange);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Çoklu filtreleme seçenekleri ile ürün listesi döner.
        /// </summary>
        /// <param name="categoryId">Kategori Id (opsiyonel).</param>
        /// <param name="minPrice">Minimum fiyat (opsiyonel).</param>
        /// <param name="maxPrice">Maksimum fiyat (opsiyonel).</param>
        /// <param name="inStock">Stok durumu (opsiyonel).</param>
        /// <param name="searchTerm">Arama terimi (opsiyonel).</param>
        /// <returns>Filtrelenmiş ürün listesi.</returns>
        public async Task<IReadOnlyList<Product>> GetFilteredProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, bool? inStock, string searchTerm)
        {
            return await _productRepository.GetFilteredProductsAsync(categoryId, minPrice, maxPrice, inStock, searchTerm);
        }

        /// <summary>
        /// Ürünü slug (URL dostu isim) değerine göre getirir.
        /// </summary>
        /// <param name="slug">Ürünün slug değeri.</param>
        /// <returns>Slug'a göre bulunan ürün.</returns>
        public async Task<Product> GetBySlugAsync(string slug)
        {
            return await _productRepository.GetBySlugAsync(slug);
        }

        /// <summary>
        /// Verilen slug'ın başka bir üründe kullanılıp kullanılmadığını kontrol eder.
        /// </summary>
        /// <param name="slug">Kontrol edilecek slug.</param>
        /// <param name="productIdToExclude">Kontrol sırasında hariç tutulacak ürün Id (opsiyonel).</param>
        /// <returns>Slug benzersiz ise true döner.</returns>
        public async Task<bool> IsSlugUniqueAsync(string slug, int? productIdToExclude = null)
        {
            return await _productRepository.IsSlugUniqueAsync(slug, productIdToExclude);
        }




        /// <summary>
        /// Sayfalama ve filtreleme ile ürünleri getirir
        /// </summary>
        public async Task<(IReadOnlyList<Product> Products, int TotalCount)> GetPagedProductsAsync(ProductFilterDto filter)
        {
            // This would typically use specifications or a more complex query builder
            // For now, using the existing filtered method and implementing pagination logic
            var allFilteredProducts = await GetFilteredProductsAsync(
                filter.CategoryId,
                filter.MinPrice,
                filter.MaxPrice,
                filter.InStock,
                filter.SearchTerm);

            // Apply additional filters
            var query = allFilteredProducts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Size))
            {
                query = query.Where(product => product.ProductAttributeCombination.Any(combination =>
                    !combination.IsDeleted && combination.ProductAttributeCombinationValues.Any(value =>
                        !value.IsDeleted && value.ProductAttributeValue != null && value.ProductAttributeValue.Value == filter.Size)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Color))
            {
                query = query.Where(product => HasAttributeValue(product, "Renk", filter.Color));
            }

            if (!string.IsNullOrWhiteSpace(filter.Material))
            {
                query = query.Where(product => HasAttributeValue(product, "Malzeme", filter.Material));
            }

            if (filter.Personalizable == true)
            {
                query = query.Where(product => product.ProductAttributeCombination.Any(combination =>
                    !combination.IsDeleted && combination.ProductAttributeCombinationValues.Any(value =>
                        !value.IsDeleted && value.ProductAttribute != null && value.ProductAttribute.IsPersonalizationText)));
            }

            if (filter.TagIds?.Any() == true)
            {
                query = query.Where(p => p.ProductProductTags.Any(ppt => filter.TagIds.Contains(ppt.ProductTagId)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                query = query.Where(p => p.Brand.Contains(filter.Brand, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.IsPublished.HasValue)
            {
                query = query.Where(p => p.IsPublished == filter.IsPublished.Value);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "price" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.DiscountPrice ?? p.BasePrice)
                    : query.OrderBy(p => p.DiscountPrice ?? p.BasePrice),
                "date" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                "name" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            var totalCount = query.Count();

            // Apply pagination
            var products = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return (products, totalCount);
        }

        private static bool HasAttributeValue(Product product, string attributeName, string expectedValue) =>
            product.ProductAttributeCombination.Any(combination => !combination.IsDeleted &&
                combination.ProductAttributeCombinationValues.Any(value => !value.IsDeleted &&
                    value.ProductAttribute != null && value.ProductAttributeValue != null &&
                    value.ProductAttribute.Name.Contains(attributeName, StringComparison.OrdinalIgnoreCase) &&
                    value.ProductAttributeValue.Value.Equals(expectedValue, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// İlgili ürünleri getirir (aynı kategori veya benzer özellikler)
        /// </summary>
        public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int count = 5, bool? forCart = null)
        {
            // Öncelik explicit tanımlı ilişkilerdedir. İlişki varsa ama o ekran için
            // gizlendiyse kategori fallback'i göstermeyiz.
            var hasExplicitRelations = (await _unitOfWork.ProductRelatedRepository.GetRelatedProductsAsync(productId, 1)).Any();
            var explicitRelated = await _unitOfWork.ProductRelatedRepository.GetRelatedProductsAsync(productId, count, forCart);
            if (explicitRelated != null && explicitRelated.Any())
                return explicitRelated.ToList();
            if (hasExplicitRelations)
                return new List<Product>();

            // Fallback: aynı kategori ürünlerinden doldur
            var product = await GetByIdAsync(productId);
            if (product == null) return new List<Product>();
            var sameCategory = await GetByCategoryIdAsync(product.CategoryId);
            return sameCategory
                .Where(p => p.Id != productId && p.IsActive && p.IsPublished && !p.IsDeleted && p.Quantity > 0)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Etiketlere göre ürünleri getirir
        /// </summary>
        public async Task<IReadOnlyList<Product>> GetProductsByTagsAsync(List<int> tagIds)
        {
            if (!tagIds?.Any() == true) return new List<Product>();

            var allProducts = await GetAllAsync();
            return allProducts
                .Where(p => p.ProductProductTags.Any(ppt => tagIds.Contains(ppt.ProductTagId))
                           && p.IsPublished && !p.IsDeleted)
                .ToList();
        }

        /// <summary>
        /// İlişkili verilerle birlikte ürün oluşturur
        /// </summary>
        public async Task<Product> CreateProductWithRelationsAsync(CreateProductDto createDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Ana ürünü oluştur
                  var product = createDto.ToEntity();
                  product.CreatedAt = DateTime.UtcNow;
                  product.CreatedBy = "System";
                  product.RowVersion = Guid.NewGuid().ToByteArray();
                  var createdProduct = await _unitOfWork.ProductRepository.AddAsync(product);
                await _unitOfWork.CompleteAsync();
                // Ana ürün fiyatı
                //if (createDto.ProductPrice != null)
                //{
                //    var mainPrice = _mapper.Map<ProductPrice>(createDto.ProductPrice);
                //    mainPrice.ProductId = createdProduct.Id;
                //    mainPrice.ProductAttributeCombinationId = null; // Ana ürün olduğu için null
                //    mainPrice.CreatedAt = DateTime.UtcNow;
                //    mainPrice.CreatedBy = "System";
                //    mainPrice.Price = createdProduct.BasePrice;
                //    await _unitOfWork.ProductPriceRepository.AddAsync(mainPrice);
                //}
                var mainPrice = new ProductPrice
                {
                    ProductId = createdProduct.Id,
                    ProductAttributeCombinationId = null, // ana ürün
                    Price = createdProduct.BasePrice,     // BasePrice üzerinden otomatik
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsActive = true
                };
                await _unitOfWork.ProductPriceRepository.AddAsync(mainPrice);
                // Ürün resimleri
                if (createDto.ProductImages?.Any() == true)
                {
                    foreach (var imageDto in createDto.ProductImages)
                    {
                        var productImage = imageDto.ToEntity();
                        productImage.ProductId = createdProduct.Id;
                        productImage.CreatedAt = DateTime.UtcNow;
                        productImage.CreatedBy = "System";

                        await _unitOfWork.ProductImageRepository.AddAsync(productImage);
                    }
                }

                // Ürün etiketleri
                if (createDto.ProductTagIds?.Any() == true)
                {
                    foreach (var tagId in createDto.ProductTagIds)
                    {
                        var productTag = new ProductProductTag
                        {
                            ProductId = createdProduct.Id,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "System",
                            ProductTagId = tagId
                        };
                        await _unitOfWork.ProductProductTagRepository.AddAsync(productTag);
                    }
                }

                // Varyasyon kombinasyonları ve değerleri
                if (createDto.AttributeCombinations?.Any() == true)
                {
                    foreach (var combinationDto in createDto.AttributeCombinations)
                    {
                        var combination = combinationDto.ToEntity();
                        combination.ProductId = createdProduct.Id;
                        combination.CreatedAt = DateTime.UtcNow;
                        combination.CreatedBy = "System";
                        combination.IsActive = true;
                       
                        var addedCombination = await _unitOfWork.ProductAttributeCombinationRepository.AddAsync(combination);
                        await _unitOfWork.CompleteAsync(); // ⭐ Combination'ı kaydet ki ID'si oluşsun
                                                           // Kombinasyon fiyatı
                        if (combinationDto.Price > 0)
                        {
                            var comboPrice = new ProductPrice
                            {
                                ProductId = createdProduct.Id,
                                ProductAttributeCombinationId = addedCombination.Id,
                                Price = combinationDto.Price,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "System"
                            };
                            await _unitOfWork.ProductPriceRepository.AddAsync(comboPrice);
                        }

                        if (combinationDto.AttributeValues?.Any() == true)
                        {
                            foreach (var valueDto in combinationDto.AttributeValues)
                            {
                                var valueEntity = new ProductAttributeCombinationValue // ⭐ Direct mapping yerine manuel oluştur
                                {
                                    ProductAttributeCombinationId = addedCombination.Id,
                                    ProductAttributeId = valueDto.ProductAttributeId,
                                    ProductAttributeValueId = valueDto.ProductAttributeValueId > 0
    ? valueDto.ProductAttributeValueId
    : (int?)null,
                                    //PersonalizationText = valueDto.PersonalizationText ?? string.Empty, // ⭐ Null kontrolü
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = "System",
                                    IsActive = true
                                };

                                await _unitOfWork.ProductAttributeCombinationValueRepository.AddAsync(valueEntity);
                            }
                        }
                    }
                }

                // İlgili ürünler (opsiyonel, max 4)
                if (createDto.RelatedProductIds?.Any() == true)
                {
                    var ids = createDto.RelatedProductIds
                        .Where(x => x > 0 && x != createdProduct.Id)
                        .Distinct()
                        .Take(4)
                        .ToList();

                    if (ids.Any())
                    {
                        var existing = await _unitOfWork.ProductRepository.GetAllByIdsAsync(ids);
                        var existingIds = existing.Select(p => p.Id).ToList();
                        var order = 0;
                        foreach (var rid in existingIds)
                        {
                            var pr = new ProductRelated
                            {
                                ProductId = createdProduct.Id,
                                RelatedProductId = rid,
                                SortOrder = order++,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "System"
                            };
                            await _unitOfWork.ProductRelatedRepository.AddAsync(pr);
                        }
                    }
                }

                await _unitOfWork.CompleteAsync(); // Son kayıt
                await _unitOfWork.CommitAsync();
                return await GetProductDetailsAsync(createdProduct.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                // Log the exception details
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        /// <summary>
        /// İlişkili verilerle birlikte ürün günceller
        /// </summary>
        public async Task<Product> UpdateProductWithRelationsAsync(UpdateProductDto updateDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingProduct = await GetProductDetailsAsync(updateDto.Id);
                if (existingProduct == null)
                    throw new ArgumentException("Ürün bulunamadı");

                  updateDto.ApplyTo(existingProduct);
                  existingProduct.RowVersion = Guid.NewGuid().ToByteArray();

                  existingProduct.LastModifiedBy = "System"; // Use user context if available
                await _unitOfWork.CompleteAsync();
                // --- Fiyatları Güncelle ---
                await _unitOfWork.ProductPriceRepository.RemoveByProductIdAsync(existingProduct.Id);

                // Ana ürün fiyatı
                await _unitOfWork.ProductPriceRepository.AddAsync(new ProductPrice
                {
                    ProductId = existingProduct.Id,
                    Price = existingProduct.BasePrice,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsActive = true
                });

                // --- Etiketleri Güncelle ---
                await _unitOfWork.ProductProductTagRepository.RemoveByProductIdAsync(existingProduct.Id);
                if (updateDto.ProductTagIds?.Any() == true)
                {
                    foreach (var tagId in updateDto.ProductTagIds)
                    {
                        var productTag = new ProductProductTag
                        {
                            ProductId = existingProduct.Id,
                            ProductTagId = tagId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "System"
                        };
                        await _unitOfWork.ProductProductTagRepository.AddAsync(productTag);
                    }
                }

                // --- Resimleri Güncelle ---
                await _unitOfWork.ProductImageRepository.RemoveByProductIdAsync(existingProduct.Id);

                if (updateDto.ProductImages?.Any() == true)
                {
                    foreach (var imageDto in updateDto.ProductImages)
                    {
                        var productImage = imageDto.ToEntity();
                        productImage.ProductId = existingProduct.Id;
                        productImage.CreatedAt = DateTime.UtcNow;
                        productImage.CreatedBy = "System";
                        await _unitOfWork.ProductImageRepository.AddAsync(productImage);
                    }
                }

                // --- İlgili Ürünleri Güncelle ---
                await _unitOfWork.ProductRelatedRepository.RemoveByProductIdAsync(existingProduct.Id);
                if (updateDto.RelatedProductIds?.Any() == true)
                {
                    var ids = updateDto.RelatedProductIds
                        .Where(x => x > 0 && x != existingProduct.Id)
                        .Distinct()
                        .Take(4)
                        .ToList();

                    if (ids.Any())
                    {
                        var existingRelated = await _unitOfWork.ProductRepository.GetAllByIdsAsync(ids);
                        var existingIds = existingRelated.Select(p => p.Id).ToList();
                        var order = 0;
                        foreach (var rid in existingIds)
                        {
                            var pr = new ProductRelated
                            {
                                ProductId = existingProduct.Id,
                                RelatedProductId = rid,
                                SortOrder = order++,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "System"
                            };
                            await _unitOfWork.ProductRelatedRepository.AddAsync(pr);
                        }
                    }
                }

                // --- Varyasyonları Güncelle ---

                // Geçmiş siparişlerin varyant referanslarını bozmamak için eski
                // kombinasyonları fiziksel olarak silmek yerine pasifleştiriyoruz.
                await _unitOfWork.ProductRepository.DeactivateByProductIdAsync(existingProduct.Id);

                if (updateDto.AttributeCombinations?.Any() == true)
                {
                    foreach (var combinationDto in updateDto.AttributeCombinations)
                    {
                        var combination = combinationDto.ToEntity();
                        combination.ProductId = existingProduct.Id;
                        combination.CreatedAt = DateTime.UtcNow;
                        combination.CreatedBy = "System";
                        combination.IsActive = true;
                        combination.Id = 0;
                      
                        var addedCombination = await _unitOfWork.ProductAttributeCombinationRepository.AddAsync(combination);
                        await _unitOfWork.CompleteAsync();

                        // Kombinasyon fiyatı
                        if (combinationDto.Price > 0)
                        {
                            await _unitOfWork.ProductPriceRepository.AddAsync(new ProductPrice
                            {
                                ProductId = existingProduct.Id,
                                ProductAttributeCombinationId = addedCombination.Id,
                                Price = combinationDto.Price,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "System",
                                IsActive = true
                            });
                        }

                        if (combinationDto.Values?.Any() == true)
                        {
                            foreach (var valueDto in combinationDto.Values)
                            {
                                var valueEntity = new ProductAttributeCombinationValue
                                {
                                    ProductAttributeCombinationId = addedCombination.Id,
                                    ProductAttributeId = valueDto.ProductAttributeId,
                                    ProductAttributeValueId = (valueDto.ProductAttributeValueId == 0) ? null : valueDto.ProductAttributeValueId,

                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = "System",
                                    IsActive = true
                                };
                                await _unitOfWork.ProductAttributeCombinationValueRepository.AddAsync(valueEntity);
                           
                            }
                        }
                    }
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitAsync();

                // Eski varyantlar bu isteğin başında takip ediliyordu. Yeni aktif
                // varyantları dönerken navigation fix-up'ın silinmiş kayıtları
                // yanıta yeniden eklememesi için sorguyu temiz context ile çalıştır.
                _dbContext.ChangeTracker.Clear();
                return await GetProductDetailsAsync(existingProduct.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.Message}");
                throw;
            }
        }


        /// <summary>
        /// Toplu fiyat güncelleme
        /// </summary>
        public async Task BulkUpdatePricesAsync(List<ProductPriceUpdateDto> priceUpdates)
        {
            foreach (var priceUpdate in priceUpdates)
            {
                var product = await GetByIdAsync(priceUpdate.Id);
                if (product != null)
                {
                    if (priceUpdate.BasePrice.HasValue)
                    {
                        var priceDifference = priceUpdate.BasePrice.Value - product.BasePrice;
                        product.BasePrice = priceUpdate.BasePrice.Value;

                        // Varyant fiyatları ürün fiyatını tamamen ezdiği için yalnızca
                        // ana fiyatı değiştirmek vitrinde eski varyant fiyatını gösterir.
                        // Farkı ekleyerek her varyantın kendi fiyat farkını koruyoruz.
                        var combinations = await _dbContext.ProductAttributeCombinations
                            .Where(combination => combination.ProductId == product.Id && combination.IsActive && !combination.IsDeleted && combination.Price > 0)
                            .ToListAsync();
                        foreach (var combination in combinations)
                            combination.Price += priceDifference;
                    }

                    if (priceUpdate.DiscountPrice.HasValue)
                        product.DiscountPrice = priceUpdate.DiscountPrice.Value;

                    product.LastModifiedAt = DateTime.UtcNow;
                    product.LastModifiedBy = "System";

                    await UpdateAsync(product);
                }
            }

            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// SKU benzersizliğini kontrol eder
        /// </summary>
        public async Task<bool> IsSKUUniqueAsync(string sku, int? productIdToExclude = null)
        {
            var allProducts = await GetAllAsync();
            return !allProducts.Any(p => p.SKU == sku &&
                                        (productIdToExclude == null || p.Id != productIdToExclude) &&
                                        !p.IsDeleted);
        }

        /// <summary>
        /// Öne çıkan ürünleri getirir
        /// </summary>
        public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count = 10)
        {
            // This assumes you have a IsFeatured property or similar mechanism
            // For now, returning newest published products
            var allProducts = await GetAllAsync();
            return allProducts
                .Where(p => p.IsPublished && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Yeni gelen ürünleri getirir
        /// </summary>
        public async Task<IReadOnlyList<Product>> GetNewArrivalsAsync(int count = 10)
        {
            var allProducts = await GetAllAsync();
            return allProducts
                .Where(p => p.IsPublished && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// İndirimli ürünleri getirir
        /// </summary>
        public async Task<IReadOnlyList<Product>> GetDiscountedProductsAsync()
        {
            var allProducts = await GetAllAsync();
            return allProducts
                .Where(p => p.DiscountPrice.HasValue &&
                           p.DiscountPrice < p.BasePrice &&
                           p.IsPublished &&
                           !p.IsDeleted)
                .ToList();
        }

        /// <summary>
        /// Ürünün öne çıkan durumunu ayarlar
        /// </summary>
        public async Task SetFeaturedStatusAsync(int productId, bool isFeatured)
        {
            var product = await GetByIdAsync(productId);
            if (product != null)
            {
                // This assumes you have a IsFeatured property
                // If not, you might need to implement this differently
                // product.IsFeatured = isFeatured;
                product.LastModifiedAt = DateTime.UtcNow;
                product.LastModifiedBy = "System";

                await UpdateAsync(product);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Ürün istatistiklerini getirir
        /// </summary>
        public async Task<Dictionary<string, int>> GetProductStatisticsAsync()
        {
            var allProducts = await GetAllAsync();

            return new Dictionary<string, int>
            {
                ["TotalProducts"] = allProducts.Count,
                ["PublishedProducts"] = allProducts.Count(p => p.IsPublished && !p.IsDeleted),
                ["InStockProducts"] = allProducts.Count(p => p.Quantity > 0 && !p.IsDeleted),
                ["OutOfStockProducts"] = allProducts.Count(p => p.Quantity == 0 && !p.IsDeleted),
                ["DiscountedProducts"] = allProducts.Count(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.BasePrice && !p.IsDeleted),
                ["DeletedProducts"] = allProducts.Count(p => p.IsDeleted)
            };
        }

        public async Task BulkSoftDeleteAsync(List<int> productIds)
        {
            foreach (var id in productIds)
            {
                var product = await GetByIdAsync(id);
                if (product != null)
                {
                    await SoftDeleteAsync(product);
                }
            }
        }

        public async Task BulkUpdatePublishedStatusAsync(List<int> productIds, bool isPublished)
        {
            foreach (var id in productIds)
            {
                var product = await GetByIdAsync(id);
                if (product != null)
                {
                    product.IsPublished = isPublished;
                    await UpdateAsync(product);
                }
            }
        }

        public async Task<byte[]> ExportProductsAsync(ExportFilterDto filter)
        {
            // Get filtered products (for demo, use GetAllAsync and filter manually)
            var products = await GetAllAsync();
            if (filter.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == filter.CategoryId.Value).ToList();
            if (!string.IsNullOrEmpty(filter.Status))
            {
                if (bool.TryParse(filter.Status, out var isPublished))
                    products = products.Where(p => p.IsPublished == isPublished).ToList();
            }
            if (filter.StartDate.HasValue)
                products = products.Where(p => p.CreatedAt >= filter.StartDate.Value).ToList();
            if (filter.EndDate.HasValue)
                products = products.Where(p => p.CreatedAt <= filter.EndDate.Value).ToList();
            if (filter.TagIds?.Any() == true)
                products = products.Where(p => p.ProductProductTags.Any(t => filter.TagIds.Contains(t.ProductTagId))).ToList();

            // Build CSV
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,Name,SKU,Price,IsPublished,CreatedAt");
            foreach (var p in products)
            {
                sb.AppendLine($"{p.Id},\"{p.Name}\",{p.SKU},{p.BasePrice},{p.IsPublished},{p.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }


    }
}
