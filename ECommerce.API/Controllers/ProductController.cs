using ECommerce.API.Converter;
using ECommerce.API.Dtos.Products;
using ECommerce.API.Helper;
using ECommerce.Repository.Data;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürünlerle ilgili herkese açık uç noktaları sağlar.
    /// Koleksiyon, listeleme, detay ve ilişik veriler (resimler, etiketler vb.) için zengin yanıtlar üretir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

   
        public ProductController(
            IProductService productService,
            ILogger<ProductController> logger,
            ApplicationDbContext context,
            IDistributedCache cache)
        {
            _productService = productService;
            _logger = logger;
            _context = context;
            _cache = cache;

        }
        [HttpGet("collection/{collection}")]
        //[CacheResponse(72000)] // 20 saat cache
        public async Task<IActionResult> GetCollection(string collection, [FromQuery] string currency = "TRY")
        {
            var products = await _productService.GetCollectionProductsAsync(collection, currency);

           
            return Ok(products);
        }
        [HttpGet("testo")]
        public async Task<IActionResult> GetTesto()
        {
            var products = await _context.Products
        .Include(x => x.ProductImages)
            .ThenInclude(x => x.Image)
        .Include(x => x.Category)
        .Where(p => p.IsPublished && !p.IsDeleted)
        .Select(p => new ProducttestoDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug=p.Slug,
            Price = p.BasePrice.ToString("F2"), // decimal → string
            Href = $"/products/{p.Id}",    // örnek link
            ImageSrc = p.ProductImages.FirstOrDefault() != null
                ? p.ProductImages.First().Image.Url
                : "/images/no-image.png",
            ImageAlt = p.ProductImages.FirstOrDefault() != null
    ? p.ProductImages.FirstOrDefault().Image.AltText
    : p.Name,

            Category = p.Category != null ? p.Category.Name : null
        })
        .ToListAsync();

            return Ok(products);
        }
        public class ProducttestoDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Price { get; set; } = "";
            public string Slug { get; set; } = "";
            public string Href { get; set; } = "";
            public string ImageSrc { get; set; } = "";
            public string ImageAlt { get; set; } = "";
            public string? Category { get; set; }
        }

        [HttpGet("filter-options")]
        [ProducesResponseType(typeof(DataResponse<ProductFilterOptionsResponse>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<ProductFilterOptionsResponse>>> GetFilterOptions([FromQuery] int categoryId)
        {
            var productPrices = await _context.Products
                .AsNoTracking()
                .Where(product => product.CategoryId == categoryId && product.IsPublished && !product.IsDeleted)
                .Select(product => product.DiscountPrice ?? product.BasePrice)
                .ToListAsync();

            var attributeValues = await _context.ProductAttributeCombinationValues
                .AsNoTracking()
                .Where(item => !item.IsDeleted
                    && !item.ProductAttributeCombination.IsDeleted
                    && item.ProductAttributeCombination.Product.CategoryId == categoryId
                    && item.ProductAttributeCombination.Product.IsPublished
                    && !item.ProductAttributeCombination.Product.IsDeleted)
                .Select(item => new
                {
                    AttributeName = item.ProductAttribute.Name,
                    item.ProductAttribute.IsPersonalizationText,
                    Value = item.ProductAttributeValue != null ? item.ProductAttributeValue.Value : null
                })
                .ToListAsync();

            static bool Contains(string value, params string[] candidates) =>
                candidates.Any(candidate => value.Contains(candidate, StringComparison.CurrentCultureIgnoreCase));

            var response = new ProductFilterOptionsResponse
            {
                Sizes = attributeValues
                    .Where(item => !item.IsPersonalizationText && Contains(item.AttributeName, "beden", "ölçü", "numara"))
                    .Select(item => item.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(value => value)
                    .ToList(),
                Colors = attributeValues
                    .Where(item => !item.IsPersonalizationText && Contains(item.AttributeName, "renk"))
                    .Select(item => item.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(value => value)
                    .ToList(),
                Materials = attributeValues
                    .Where(item => !item.IsPersonalizationText && Contains(item.AttributeName, "malzeme", "materyal"))
                    .Select(item => item.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(value => value)
                    .ToList(),
                HasPersonalization = attributeValues.Any(item => item.IsPersonalizationText),
                MinPrice = productPrices.Count == 0 ? 0 : Math.Floor(productPrices.Min()),
                MaxPrice = productPrices.Count == 0 ? 0 : Math.Ceiling(productPrices.Max() / 100m) * 100m
            };

            return Ok(DataResponse<ProductFilterOptionsResponse>.CreateSuccess(response));
        }

        public sealed class ProductFilterOptionsResponse
        {
            public List<string> Sizes { get; set; } = [];
            public List<string> Colors { get; set; } = [];
            public List<string> Materials { get; set; } = [];
            public bool HasPersonalization { get; set; }
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
        }

        /// <summary>
        /// Verilen ID listesine göre ürünleri getirir
        /// </summary>
        [HttpGet("by-ids")]
        [ProducesResponseType(typeof(DataResponse<IReadOnlyList<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetProductsByIds([FromQuery] string ids)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                {
                    return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(new List<ProductListDto>()));
                }

                var idList = ids
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var v) ? v : 0)
                    .Where(v => v > 0)
                    .Distinct()
                    .ToList();

                if (!idList.Any())
                {
                    return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(new List<ProductListDto>()));
                }

                var products = await _context.Products
                    .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
                    .Include(p => p.Category)
                    .Where(p => idList.Contains(p.Id) && p.IsPublished && !p.IsDeleted)
                    .ToListAsync();

                // Verilen sırayı koru
                products = products.OrderBy(p => idList.IndexOf(p.Id)).ToList();

                var productDtos = products.Select(product => product.ToListDto()).ToList();
                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IDs'e göre ürünler çekilirken hata oluştu: {Ids}", ids);
                return StatusCode(500, BaseResponse.CreateFailure("Ürünler getirilirken bir hata oluştu"));
            }
        }
        /// <summary>
        /// Tüm ürünleri sayfalama ile getirir
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductListDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResponse<ProductListDto>>> GetProducts([FromQuery] ProductFilterDto filter)
        {
            try
            {
                // Bu uç nokta mağaza vitrini içindir. Taslaklar yalnızca yönetim uç noktalarından görülebilir.
                filter.IsPublished = true;
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
                if (product == null || !product.IsPublished || product.IsDeleted)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }

                var productDto = product.ToDto();
                productDto.ReviewCount = await _context.ProductReviews.CountAsync(r => r.ProductId == product.Id && r.IsApproved);
                productDto.QuestionCount = await _context.ProductQuestions.CountAsync(q => q.ProductId == product.Id && q.IsApproved);


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
        //[CacheResponse(57600)] // 16 saat cache
        //[Obsolete("Use GET /api/resolve-slug/{slug} instead")] 
        [ProducesResponseType(typeof(DataResponse<ProductDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DataResponse<ProductDetailDto>>> GetProductBySlug(string slug)
        {
            try
            {
                var product = await _productService.GetBySlugAsync(slug);
                if (product == null || !product.IsPublished || product.IsDeleted)
                {
                    return NotFound(BaseResponse.CreateFailure("Ürün bulunamadı"));
                }
                var dto = ProductMapper.ToCustomerDto(product);
                dto.ReviewCount = await _context.ProductReviews.CountAsync(r => r.ProductId == product.Id && r.IsApproved);
                dto.QuestionCount = await _context.ProductQuestions.CountAsync(q => q.ProductId == product.Id && q.IsApproved);

                return Ok(DataResponse<ProductDetailDto>.CreateSuccess(dto, "Ürün başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün detayı getirilirken hata oluştu. Slug: {Slug}", slug);
                return StatusCode(500, BaseResponse.CreateFailure("Ürün getirilirken bir hata oluştu"));
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
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

                return Ok(DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} ürün bulundu"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arama sırasında hata oluştu. Arama terimi: {SearchTerm}", searchTerm);
                return StatusCode(500, BaseResponse.CreateFailure("Arama sırasında bir hata oluştu"));
            }
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSearchSuggestions([FromQuery] string q)
        {
            var term = q?.Trim();
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Ok(DataResponse<List<ProductSuggestionResponse>>.CreateSuccess([]));

            var items = await _context.Products.AsNoTracking()
                .Where(product => product.IsPublished && !product.IsDeleted && (EF.Functions.ILike(product.Name, $"{term}%") || EF.Functions.ILike(product.Name, $"%{term}%") || EF.Functions.ILike(product.SKU, $"%{term}%")))
                .OrderByDescending(product => EF.Functions.ILike(product.Name, $"{term}%"))
                .ThenBy(product => product.Name).Take(8)
                .Select(product => new ProductSuggestionResponse(product.Id, product.Name, product.Slug, product.DiscountPrice ?? product.BasePrice, product.ProductImages.OrderBy(image => image.SortOrder).Select(image => image.Image.Url).FirstOrDefault()))
                .ToListAsync();
            return Ok(DataResponse<List<ProductSuggestionResponse>>.CreateSuccess(items));
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
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

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
                var cacheKey = $"product:popular:published:v2:count:{count}";
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var cachedObj = System.Text.Json.JsonSerializer.Deserialize<DataResponse<IReadOnlyList<ProductListDto>>>(cached);
                    if (cachedObj != null) return Ok(cachedObj);
                }

                var products = await _productService.GetPopularProductsAsync(count);
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

                var response = DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} popüler ürün getirildi");
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
                return Ok(response);
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
                var cacheKey = $"product:featured:published:v2:count:{count}";
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var cachedObj = System.Text.Json.JsonSerializer.Deserialize<DataResponse<IReadOnlyList<ProductListDto>>>(cached);
                    if (cachedObj != null) return Ok(cachedObj);
                }

                var products = await _productService.GetFeaturedProductsAsync(count);
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

                var response = DataResponse<IReadOnlyList<ProductListDto>>.CreateSuccess(productDtos,
                    $"{products.Count} öne çıkan ürün getirildi");
                var json = System.Text.Json.JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
                return Ok(response);
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
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

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
                var productDtos = products.Where(product => product.IsPublished && !product.IsDeleted).Select(product => product.ToListDto()).ToList();

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
        public async Task<ActionResult<DataResponse<IReadOnlyList<ProductListDto>>>> GetRelatedProducts(int id, [FromQuery] int count = 5, [FromQuery] string? placement = null)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    count = 5;
                }

                var forCart = placement?.Trim().ToLowerInvariant() switch
                {
                    "cart" => true,
                    "product" or "productpage" => false,
                    _ => (bool?)null
                };
                var products = await _productService.GetRelatedProductsAsync(id, count, forCart);
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

       
    }

    public record ProductSuggestionResponse(int Id, string Name, string Slug, decimal Price, string? ImageUrl);
}
