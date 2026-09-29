using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ECommerce.Domain.Entity;
using ECommerce.API.Helper;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Ürün yönetimi: oluşturma, güncelleme, silme, listeleme ve detay işlemleri.
    /// Domain route (slug) eşlemelerini de güncel tutar.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]

    public class AdminProductController : ControllerBase
    {

        private readonly IProductService _productService;
        private readonly ILogger<AdminProductController> _logger;
        private readonly ECommerce.Repository.Data.ApplicationDbContext _db;
        private readonly StackExchange.Redis.IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;

        public AdminProductController(
            IProductService productService,
            ILogger<AdminProductController> logger,
            ECommerce.Repository.Data.ApplicationDbContext db,
            StackExchange.Redis.IConnectionMultiplexer redis,
            IConfiguration configuration)
        {
            _productService = productService;
            _logger = logger;
            _db = db;
            _redis = redis;
            _configuration = configuration;
        }

        private async Task UpsertDomainRouteAsync(string slug, int entityId)
        {
            if (string.IsNullOrWhiteSpace(slug) || entityId <= 0) return;
            var existing = await _db.DomainRoutes.FirstOrDefaultAsync(r => r.Slug == slug);
            if (existing == null)
            {
                _db.DomainRoutes.Add(new ECommerce.Domain.Entity.DomainRoute
                {
                    Slug = slug,
                    EntityType = "product",
                    EntityId = entityId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.EntityType = "product";
                existing.EntityId = entityId;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            // invalidate resolve cache keys
            try
            {
                var db = _redis.GetDatabase();
                var lowerSlug = slug.ToLowerInvariant();
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/product/by-slug/{slug}"));
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/product/by-slug/{lowerSlug}"));
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/resolve-slug/{slug}"));
                await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/routes/resolve/{slug}"));
            }
            catch { }
            await InvalidatePublicProductCachesAsync();
        }

        private async Task DeactivateDomainRouteAsync(int productId)
        {
            var rows = await _db.DomainRoutes.Where(r => r.EntityType == "product" && r.EntityId == productId).ToListAsync();
            if (rows.Count > 0)
            {
                foreach (var r in rows)
                {
                    r.IsActive = false;
                    r.UpdatedAt = DateTime.UtcNow;
                    try
                    {
                        var db = _redis.GetDatabase();
                        var lowerSlug = r.Slug.ToLowerInvariant();
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/product/by-slug/{r.Slug}"));
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/product/by-slug/{lowerSlug}"));
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/resolve-slug/{r.Slug}"));
                        await db.KeyDeleteAsync(RedisCacheKeys.ApiResponse(_configuration, $"/api/routes/resolve/{r.Slug}"));
                    }
                    catch { }
                }
                await _db.SaveChangesAsync();
            }
            await InvalidatePublicProductCachesAsync();
        }

        private async Task InvalidatePublicProductCachesAsync()
        {
            try
            {
                var cache = _redis.GetDatabase();
                await cache.KeyDeleteAsync("home:bestsellers");
                await cache.KeyDeleteAsync("home:featured");
                await cache.KeyDeleteAsync("product:popular:published:v2:count:10");
                await cache.KeyDeleteAsync("product:featured:published:v2:count:10");
            }
            catch { }
        }
        /// <summary>
        /// Ürünü günceller
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(DataResponse<ProductDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
        {
            try
            {
                var sub = User.FindFirst("sub")?.Value;  // "ugurrk35"
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // "3"
                var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList(); // ["Admin"]
                if (id != updateDto.Id)
                {
                    return BadRequest(BaseResponse.CreateFailure("ID uyumsuzluğu"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(BaseResponse.CreateFailure("Geçersiz veri", errors));
                }

                var existingProduct = await _productService.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                // Slug uniqueness check (excluding current product)
                var isSlugUnique = await _productService.IsSlugUniqueAsync(updateDto.Slug, id);
                if (!isSlugUnique)
                {
                    return BadRequest(BaseResponse.CreateFailure("Bu slug zaten kullanımda"));
                }

                // SKU uniqueness check (excluding current product)
                var isSKUUnique = await _productService.IsSKUUniqueAsync(updateDto.SKU, id);
                if (!isSKUUnique)
                {
                    return BadRequest(BaseResponse.CreateFailure("Bu SKU zaten kullanımda"));
                }

                var product = await _productService.UpdateProductWithRelationsAsync(updateDto);
                var productDto = product.ToDto();
                // Upsert/Deactivate route based on publish state
                if (productDto.IsPublished)
                    await UpsertDomainRouteAsync(productDto.Slug, productDto.Id);
                else
                    await DeactivateDomainRouteAsync(productDto.Id);

                return Ok(DataResponse<ProductDto>.CreateSuccess(productDto, "Ürün başarıyla güncellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün güncellenirken hata oluştu. ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün güncellenirken bir hata oluştu"));
            }
        }
        /// <summary>
        /// Tüm ürünleri sayfalama ile getirir
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductListDto>), (int)HttpStatusCode.OK)]
        [Authorize]
        public async Task<ActionResult<PagedResponse<ProductListDto>>> GetProducts([FromQuery] ProductFilterDto filter)
        {
            try
            {
                var sub = User.FindFirst("sub")?.Value;  // "ugurrk35"
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // "3"
                var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList(); // ["Admin"]
                var (products, totalCount) = await _productService.GetPagedProductsAsync(filter);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                var response = new PagedResponse<ProductListDto>
                {
                    Success = true,
                    Message = "Ürünler başarıyla getirildi",
                    Items = productDtos,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// ID'ye göre ürün detayını getirir
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DataResponse<ProductDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                var productDto = product.ToDto();

               

                return Ok(DataResponse<ProductDto>.CreateSuccess(productDto, "Ürün başarıyla getirildi"));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün detayı getirilirken hata oluştu. ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Slug'a göre ürün detayını getirir
        /// </summary>
        [HttpGet("by-slug/{slug}")]
        [ProducesResponseType(typeof(DataResponse<ProductDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<ProductDto>>> GetProductBySlug(string slug)
        {
            try
            {
                var product = await _productService.GetBySlugAsync(slug);
                if (product == null)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                var productDto = product.ToDto();
                return Ok(DataResponse<ProductDto>.CreateSuccess(productDto, "Ürün başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün detayı getirilirken hata oluştu. Slug: {Slug}", slug);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Yeni ürün oluşturur
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DataResponse<ProductDto>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<DataResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(BaseResponse.CreateFailure("Geçersiz veri", errors));
                }
                createDto.Slug = createDto.Slug.Trim();
                // Slug uniqueness check
                var isSlugUnique = await _productService.IsSlugUniqueAsync(createDto.Slug);
                if (!isSlugUnique)
                {
                    return BadRequest(BaseResponse.CreateFailure("Bu slug zaten kullanımda"));
                }

                // SKU uniqueness check
                var isSKUUnique = await _productService.IsSKUUniqueAsync(createDto.SKU);
                if (!isSKUUnique)
                {

                    return BadRequest(BaseResponse.CreateFailure("Bu SKU zaten kullanımda"));
                }

                var product = await _productService.CreateProductWithRelationsAsync(createDto);
                var productDto = product.ToDto();
                // Upsert/Deactivate route based on publish state
                if (productDto.IsPublished)
                    await UpsertDomainRouteAsync(productDto.Slug, productDto.Id);
                else
                    await DeactivateDomainRouteAsync(productDto.Id);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                    DataResponse<ProductDto>.CreateSuccess(productDto, "Ürün başarıyla oluşturuldu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün oluşturulurken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürün oluşturulurken bir hata oluştu"));
            }
        }

        [HttpPost("{id:int}/duplicate")]
        public async Task<ActionResult<DataResponse<object>>> DuplicateProduct(int id)
        {
            var source = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductProductTags)
                .Include(p => p.RelatedProducts)
                .Include(p => p.ProductPrices)
                .Include(p => p.ProductAttributeCombination)
                    .ThenInclude(c => c.ProductAttributeCombinationValues)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (source == null) return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var now = DateTime.UtcNow;
            var copy = new Product { Name = $"{source.Name} (Kopya)", Slug = $"{source.Slug}-kopya-{suffix}", SKU = $"{source.SKU}-COPY-{suffix}".ToUpperInvariant(), ShortDescription = source.ShortDescription, Description = source.Description, TechnicalDetails = source.TechnicalDetails, DeliveryInstallationDetails = source.DeliveryInstallationDetails, DocumentsDetails = source.DocumentsDetails, AdditionalCategoryIdsJson = source.AdditionalCategoryIdsJson, BasePrice = source.BasePrice, DiscountPrice = source.DiscountPrice, Quantity = source.Quantity, CategoryId = source.CategoryId, MetaTitle = source.MetaTitle, MetaDescription = source.MetaDescription, MetaKeywords = source.MetaKeywords, CanonicalUrl = source.CanonicalUrl, OgTitle = source.OgTitle, OgDescription = source.OgDescription, OgImage = source.OgImage, TwitterCardType = source.TwitterCardType, Brand = source.Brand, GTIN = source.GTIN, MPN = source.MPN, IsPublished = false, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false, RowVersion = Guid.NewGuid().ToByteArray(), ProductImages = new List<ProductImage>(), ProductProductTags = new List<ProductProductTag>(), ProductAttributeCombination = new List<ProductAttributeCombination>(), RelatedProducts = new List<ProductRelated>() };
            foreach (var image in source.ProductImages) copy.ProductImages.Add(new ProductImage { ImageId = image.ImageId, SortOrder = image.SortOrder, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false });
            foreach (var tag in source.ProductProductTags) copy.ProductProductTags.Add(new ProductProductTag { ProductTagId = tag.ProductTagId, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false });
            foreach (var related in source.RelatedProducts) copy.RelatedProducts.Add(new ProductRelated { RelatedProductId = related.RelatedProductId, SortOrder = related.SortOrder, RecommendationType = related.RecommendationType, ShowOnProductPage = related.ShowOnProductPage, ShowInCart = related.ShowInCart, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false });
            var copiedCombinations = source.ProductAttributeCombination.ToList();
            _db.Products.Add(copy); await _db.SaveChangesAsync();
            var combinationIds = new Dictionary<int, int>();
            foreach (var combination in copiedCombinations)
            {
                var next = new ProductAttributeCombination { ProductId = copy.Id, Sku = $"{combination.Sku}-COPY-{suffix}".ToUpperInvariant(), Price = combination.Price, Quantity = combination.Quantity, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false };
                _db.ProductAttributeCombinations.Add(next);
                await _db.SaveChangesAsync();
                combinationIds[combination.Id] = next.Id;
                foreach (var value in combination.ProductAttributeCombinationValues)
                    _db.ProductAttributeCombinationValues.Add(new ProductAttributeCombinationValue { ProductAttributeCombinationId = next.Id, ProductAttributeId = value.ProductAttributeId, ProductAttributeValueId = value.ProductAttributeValueId, PersonalizationText = value.PersonalizationText, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false });
            }
            foreach (var price in source.ProductPrices)
                _db.ProductPrices.Add(new ProductPrice { ProductId = copy.Id, ProductAttributeCombinationId = price.ProductAttributeCombinationId is int sourceCombinationId && combinationIds.TryGetValue(sourceCombinationId, out var copiedCombinationId) ? copiedCombinationId : null, Price = price.Price, DiscountPrice = price.DiscountPrice, Currency = price.Currency, CustomerGroupId = price.CustomerGroupId, StartDate = price.StartDate, EndDate = price.EndDate, CreatedAt = now, CreatedBy = "System", IsActive = true, IsDeleted = false });
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = copy.Id }, DataResponse<object>.CreateSuccess(new { id = copy.Id }, "Ürün taslak olarak kopyalandı"));
        }

       

        /// <summary>
        /// Ürünü kalıcı olarak siler. Sipariş veya sepet geçmişi olan ürünler korunur.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> DeleteProduct(int id)
        {
            return await PermanentlyDeleteProduct(id);
        }

        /// <summary>
        /// Sipariş veya sepet geçmişi olmayan ürünü ve bağlı yönetim verilerini kalıcı olarak siler.
        /// </summary>
        [HttpDelete("{id:int}/permanent")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> PermanentlyDeleteProduct(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == id);
            if (product == null)
                return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));

            if (await _db.OrderItems.AnyAsync(item => item.ProductId == id) ||
                await _db.CartItems.AnyAsync(item => item.ProductId == id))
            {
                return BadRequest(BaseResponse.CreateFailure("Sipariş veya sepet geçmişi olan ürün kalıcı olarak silinemez."));
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var combinationIds = await _db.ProductAttributeCombinations
                    .Where(item => item.ProductId == id)
                    .Select(item => item.Id)
                    .ToListAsync();

                _db.ProductAttributeCombinationValues.RemoveRange(
                    _db.ProductAttributeCombinationValues.Where(item => combinationIds.Contains(item.ProductAttributeCombinationId)));
                _db.ProductAttributeCombinations.RemoveRange(_db.ProductAttributeCombinations.Where(item => item.ProductId == id));
                _db.ProductPrices.RemoveRange(_db.ProductPrices.Where(item => item.ProductId == id));
                _db.ProductImages.RemoveRange(_db.ProductImages.Where(item => item.ProductId == id));
                _db.ProductProductTags.RemoveRange(_db.ProductProductTags.Where(item => item.ProductId == id));
                _db.ProductRelateds.RemoveRange(_db.ProductRelateds.Where(item => item.ProductId == id || item.RelatedProductId == id));
                _db.DomainRoutes.RemoveRange(_db.DomainRoutes.Where(item => item.EntityType == "product" && item.EntityId == id));
                _db.Products.Remove(product);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(BaseResponse.CreateSuccess("Ürün kalıcı olarak silindi"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ürün kalıcı silinirken hata oluştu. ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün kalıcı olarak silinirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Ürün arama yapar
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> SearchProducts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(BaseResponse.CreateFailure("Arama terimi gerekli"));
                }

                var products = await _productService.SearchByNameAsync(searchTerm);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arama sırasında hata oluştu. Arama terimi: {SearchTerm}", searchTerm);
                return StatusCode(500, BaseResponse.CreateFailure("Arama sırasında bir hata oluştu"));
            }
        }

        /// <summary>
        /// Kategoriye göre ürünleri getirir
        /// </summary>
        [HttpGet("category/{categoryId:int}")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _productService.GetByCategoryIdAsync(categoryId);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"Kategori için {products.Count} ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriye göre ürünler getirilirken hata oluştu. Kategori ID: {CategoryId}", categoryId);
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Fiyat aralığına göre ürünleri getirir
        /// </summary>
        [HttpGet("price-range")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetProductsByPriceRange(
            [FromQuery] decimal minPrice,
            [FromQuery] decimal maxPrice)
        {
            try
            {
                if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
                {
                    return BadRequest(BaseResponse.CreateFailure("Geçersiz fiyat aralığı"));
                }

                var products = await _productService.GetByPriceRangeAsync(minPrice, maxPrice);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"Fiyat aralığı için {products.Count} ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiyat aralığına göre ürünler getirilirken hata oluştu. Min: {MinPrice}, Max: {MaxPrice}", minPrice, maxPrice);
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Stokta bulunan ürünleri getirir
        /// </summary>
        [HttpGet("in-stock")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetInStockProducts()
        {
            try
            {
                var products = await _productService.GetInStockProductsAsync();
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"Stokta {products.Count} ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stokta bulunan ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Popüler ürünleri getirir
        /// </summary>
        [HttpGet("popular")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetPopularProducts([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 100)
                {
                    count = 10;
                }

                var products = await _productService.GetPopularProductsAsync(count);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} popüler ürün getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Popüler ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Öne çıkan ürünleri getirir
        /// </summary>
        [HttpGet("featured")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetFeaturedProducts([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 100)
                {
                    count = 10;
                }

                var products = await _productService.GetFeaturedProductsAsync(count);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} öne çıkan ürün getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öne çıkan ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Yeni gelen ürünleri getirir
        /// </summary>
        [HttpGet("new-arrivals")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetNewArrivals([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 100)
                {
                    count = 10;
                }

                var products = await _productService.GetNewArrivalsAsync(count);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} yeni ürün getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yeni ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// İndirimli ürünleri getirir
        /// </summary>
        [HttpGet("discounted")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetDiscountedProducts()
        {
            try
            {
                var products = await _productService.GetDiscountedProductsAsync();
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} indirimli ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İndirimli ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// İlgili ürünleri getirir
        /// </summary>
        [HttpGet("{id:int}/related")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetRelatedProducts(int id, [FromQuery] int count = 5)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    count = 5;
                }

                var products = await _productService.GetRelatedProductsAsync(id, count);
                var productDtos = products.Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} ilgili ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İlgili ürünler getirilirken hata oluştu. Ürün ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("İlgili ürünler getirilirken bir hata oluştu"));
            }
        }
        [HttpGet("{id:int}/cross-sell-settings")]
        public async Task<IActionResult> GetCrossSellSettings(int id) => Ok(await _db.ProductRelateds.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.SortOrder).Select(x => new CrossSellSetting(x.RelatedProductId, x.RecommendationType, x.ShowOnProductPage, x.ShowInCart, x.SortOrder)).ToListAsync());
        [HttpPut("{id:int}/cross-sell-settings")]
        public async Task<IActionResult> SetCrossSellSettings(int id, [FromBody] List<CrossSellSetting> settings)
        { var relations = await _db.ProductRelateds.Where(x => x.ProductId == id).ToListAsync(); foreach(var relation in relations){var value=settings.FirstOrDefault(x=>x.RelatedProductId==relation.RelatedProductId); if(value!=null){relation.RecommendationType=value.RecommendationType;relation.ShowOnProductPage=value.ShowOnProductPage;relation.ShowInCart=value.ShowInCart;relation.SortOrder=value.SortOrder;}} await _db.SaveChangesAsync(); return NoContent(); }

        /// <summary>
        /// Ürün stok günceller
        /// </summary>
        [HttpPatch("{id:int}/stock")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> UpdateStock(int id, [FromBody] UpdateStockDto updateStockDto)
        {
            try
            {
                if (id != updateStockDto.ProductId)
                {
                    return BadRequest(BaseResponse.CreateFailure("ID uyumsuzluğu"));
                }

                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                await _productService.UpdateStockAsync(id, updateStockDto.QuantityChange);
                return Ok(BaseResponse.CreateSuccess("Stok başarıyla güncellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok güncellenirken hata oluştu. Ürün ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("Stok güncellenirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Toplu fiyat güncelleme
        /// </summary>
        [HttpPatch("bulk-update-prices")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<BaseResponse>> BulkUpdatePrices([FromBody] BulkUpdatePricesDto bulkUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(BaseResponse.CreateFailure("Geçersiz veri", errors));
                }

                await _productService.BulkUpdatePricesAsync(bulkUpdateDto.Products);
                return Ok(BaseResponse.CreateSuccess($"{bulkUpdateDto.Products.Count} ürün fiyatı güncellendi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu fiyat güncelleme sırasında hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Fiyatlar güncellenirken bir hata oluştu"));
            }
        }

        /// <summary>
        /// Applies a price and/or exact stock quantity to every product matching the supplied filters.
        /// This operation is intentionally capped to prevent accidental catalog-wide updates.
        /// </summary>
        [HttpPatch("bulk-update-by-filter")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<BaseResponse>> BulkUpdateByFilter([FromBody] BulkUpdateProductsByFilterDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage).ToList();
                return BadRequest(BaseResponse.CreateFailure("Geçersiz veri", errors));
            }

            if (!request.BasePrice.HasValue && !request.Quantity.HasValue)
                return BadRequest(BaseResponse.CreateFailure("Fiyat veya stok miktarından en az biri girilmelidir."));

            request.Filter ??= new ProductFilterDto();
            request.Filter.PageNumber = 1;
            request.Filter.PageSize = 501;

            try
            {
                var (products, totalCount) = await _productService.GetPagedProductsAsync(request.Filter);
                if (totalCount == 0)
                    return NotFound(BaseResponse.CreateFailure("Bu filtrelerle eşleşen ürün bulunamadı."));
                if (totalCount > 500)
                    return BadRequest(BaseResponse.CreateFailure("Tek seferde en fazla 500 ürün güncellenebilir. Filtreleri daraltın."));

                var productIds = products.Select(product => product.Id).ToList();
                var combinationsByProductId = request.BasePrice.HasValue
                    ? (await _db.ProductAttributeCombinations
                        .Where(combination => productIds.Contains(combination.ProductId) && combination.IsActive && !combination.IsDeleted && combination.Price > 0)
                        .ToListAsync())
                        .GroupBy(combination => combination.ProductId)
                        .ToDictionary(group => group.Key, group => group.ToList())
                    : new Dictionary<int, List<ProductAttributeCombination>>();

                foreach (var product in products)
                {
                    if (request.BasePrice.HasValue)
                    {
                        var priceDifference = request.BasePrice.Value - product.BasePrice;
                        product.BasePrice = request.BasePrice.Value;
                        product.DiscountPrice = null;

                        // Variant prices override the product price in the storefront.
                        // Preserve each variant's existing price difference while moving it
                        // together with the new base price.
                        if (combinationsByProductId.TryGetValue(product.Id, out var combinations))
                            foreach (var combination in combinations)
                                combination.Price += priceDifference;
                    }

                    if (request.Quantity.HasValue)
                        product.Quantity = request.Quantity.Value;

                    product.LastModifiedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                return Ok(BaseResponse.CreateSuccess($"{totalCount} ürün güncellendi."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Filtreye göre toplu ürün güncelleme sırasında hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler güncellenirken bir hata oluştu."));
            }
        }

        /// <summary>
        /// Ürünü öne çıkan olarak işaretler/kaldırır
        /// </summary>
        [HttpPatch("{id:int}/featured")]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<BaseResponse>> SetFeaturedStatus(int id, [FromBody] bool isFeatured)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                await _productService.SetFeaturedStatusAsync(id, isFeatured);
                var message = isFeatured ? "Ürün öne çıkan olarak işaretlendi" : "Ürün öne çıkanlıktan kaldırıldı";
                try
                {
                    var cache = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache)) as Microsoft.Extensions.Caching.Distributed.IDistributedCache;
                    if (cache != null)
                    {
                        foreach (var c in new[] { 4, 6, 8, 10 })
                        {
                            await cache.RemoveAsync($"product:featured:count:{c}");
                        }
                    }
                }
                catch { }
                return Ok(BaseResponse.CreateSuccess(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öne çıkan durumu güncellenirken hata oluştu. Ürün ID: {ProductId}", id);
                return StatusCode(500, BaseResponse.CreateFailure("Öne çıkan durumu güncellenirken bir hata oluştu"));
            }
        }

        [HttpPatch("bulk-publish")]
        public async Task<ActionResult<BaseResponse>> BulkPublish([FromBody] BulkPublishRequest request)
        {
            var ids = request.ProductIds.Distinct().Take(500).ToList();
            if (!ids.Any()) return BadRequest(BaseResponse.CreateFailure("En az bir ürün seçilmelidir."));
            var products = await _db.Products.Where(item => ids.Contains(item.Id) && !item.IsDeleted).ToListAsync();
            foreach (var product in products) product.IsPublished = request.IsPublished;
            await _db.SaveChangesAsync();
            return Ok(BaseResponse.CreateSuccess($"{products.Count} ürün güncellendi."));
        }

        /// <summary>
        /// Ürün istatistiklerini getirir
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(DataResponse<Dictionary<string, int>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<Dictionary<string, int>>>> GetProductStatistics()
        {
            try
            {
                var statistics = await _productService.GetProductStatisticsAsync();
                return Ok(DataResponse<Dictionary<string, int>>.CreateSuccess(statistics, "İstatistikler başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün istatistikleri getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("İstatistikler getirilirken bir hata oluştu"));
            }
        }
    }

    public record BulkPublishRequest(List<int> ProductIds, bool IsPublished);
}

public record CrossSellSetting(int RelatedProductId, CrossSellRecommendationType RecommendationType, bool ShowOnProductPage, bool ShowInCart, int SortOrder);
