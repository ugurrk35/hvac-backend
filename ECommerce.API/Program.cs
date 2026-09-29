using ECommerce.Adaptor.Payment.Abstract;
using ECommerce.Adaptor.Payment.PayTR;
using ECommerce.API.Services;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using ECommerce.Repository.Repo;
using ECommerce.Repository.Repo.Blog;
using ECommerce.Repository.UnitOfWork;
using ECommerce.Service.Abstract;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Concrete;
using ECommerce.Service.Concrete.Base;
using equals.Domain.Interfaces;
using Ganss.Xss;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
// Tek bağlantı — Singleton pattern
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetConnectionString("Redis") ??
                 builder.Configuration["Redis:ConnectionString"];
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.SectionName));
builder.Services.AddScoped<ITurnstileValidator, TurnstileValidator>();
//builder.WebHost.UseUrls("http://0.0.0.0:8080");
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
var redisKeyPrefix = builder.Configuration["Redis:KeyPrefix"] ?? "kombiklimaburada";
if (!redisKeyPrefix.EndsWith(':')) redisKeyPrefix += ':';
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = redisKeyPrefix;
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Construction Directory API", Version = "v1" });
    // Use full type names to avoid schemaId collisions (esp. with nested types)
    c.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));

    // JWT Swagger Configuration
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Gelen claim’leri olduğu gibi koru (sub, role, vs.)
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});



builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o =>
        {
            o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            //EF Core artık otomatik olarak: Include + Include → Split Query Warning tamamen kaybolur Join patlaması olmaz
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
});


//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection"),
//        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ProductCampaignQuoteService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
builder.Services.AddScoped(typeof(IIdentityRepository<>), typeof(IdentityRepository<>));


builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(ISpecification<>), typeof(BaseSpecification<>));
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IProductTagRepository, ProductTagRepository>();
builder.Services.AddScoped<IProductProductTagRepository, ProductProductTagRepository>();
builder.Services.AddScoped<IProductRelatedRepository, ProductRelatedRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IProductPriceRepository, ProductPriceRepository>();
builder.Services.AddScoped<IBlogPostRepository, BlogPostRepository>();
builder.Services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();
builder.Services.AddScoped<IBlogTagRepository, BlogTagRepository>();
builder.Services.AddScoped<IBlogPostCommentRepository, BlogPostCommentRepository>();


builder.Services.AddScoped<IRoleIdentityRepository, RoleIdentityRepository>();

builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
builder.Services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();

builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
builder.Services.AddScoped<IProductAttributeValueRepository, ProductAttributeValueRepository>();
builder.Services.AddScoped<IProductAttributeCombinationRepository, ProductAttributeCombinationRepository>();
builder.Services.AddScoped<IProductAttributeCombinationValueRepository, ProductAttributeCombinationValueRepository>();
builder.Services.AddHealthChecks();
//builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));
builder.Services.AddScoped<IDateTime, DateTimeService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IProductTagService, ProductTagService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IServerSideConversionService, ServerSideConversionService>();
builder.Services.AddHttpClient();
builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection(SmtpEmailOptions.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<EmailConfirmationService>();
builder.Services.Configure<PayTRConfig>(builder.Configuration.GetSection("PayTR"));
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<PayTRConfig>>().Value);
builder.Services.AddScoped<IPaymentProvider, PayTRPaymentProvider>();
builder.Services.AddScoped<IPaymentCallbackValidator, PayTRCallbackValidator>();

// Response Compression servisini ekle
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // HTTPS için de sıkıştır
    options.Providers.Add<GzipCompressionProvider>(); // Gzip kullan
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/plain", "text/html" } // sıkıştırılacak mime tipleri
    );
});

// Gzip ayarları
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // CompressionLevel.Optimal da kullanılabilir
});

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBlogPostService, BlogPostService>();
builder.Services.AddScoped<IBlogCategoryService, BlogCategoryService>();
builder.Services.AddScoped<IBlogTagService, BlogTagService>();
builder.Services.AddScoped<IBlogPostCommentService, BlogPostCommentService>();

builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IProductPriceService, ProductPriceService>();
builder.Services.AddScoped<IPriceQuoteService, PriceQuoteService>();
builder.Services.AddScoped<IProductAttributeService, ProductAttributeService>();
builder.Services.AddScoped<IProductAttributeValueService, ProductAttributeValueService>();
builder.Services.AddScoped<IProductAttributeCombinationService, ProductAttributeCombinationService>();
builder.Services.AddScoped<IProductAttributeCombinationValueService, ProductAttributeCombinationValueService>();
var defaultAllowedOrigins = new[]
{
    "http://localhost:3000",
    "http://localhost:3001",
    "https://kombiklimaburada.com",
    "https://www.kombiklimaburada.com",
    "https://admin.kombiklimaburada.com"
};
var configuredAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowedOrigins = defaultAllowedOrigins
    .Concat(configuredAllowedOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var trustedProxyIps = builder.Configuration.GetSection("ReverseProxy:TrustedProxyIps").Get<string[]>() ?? [];
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var value in trustedProxyIps)
    {
        if (IPAddress.TryParse(value, out var ipAddress)) options.KnownProxies.Add(ipAddress);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => CreateFixedWindowLimiter(context, permitLimit: 5, window: TimeSpan.FromMinutes(1)));
    options.AddPolicy("upload", context => CreateFixedWindowLimiter(context, permitLimit: 10, window: TimeSpan.FromMinutes(1)));
    options.AddPolicy("payment", context => CreateFixedWindowLimiter(context, permitLimit: 10, window: TimeSpan.FromMinutes(1)));
});


var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    app.Logger.LogInformation("Veritabanı migration kontrolü başlatılıyor.");
    await db.Database.MigrateAsync();
    // Legacy databases can have a migration-history entry without the archive columns.
    // This is idempotent and keeps startup self-healing until all environments converge.
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchiveReason\" character varying(1000);");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchivedAt\" timestamp with time zone;");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchivedByUserId\" integer;");
    await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Orders_IsDeleted_ArchivedAt\" ON \"Orders\" (\"IsDeleted\", \"ArchivedAt\");");
    // Some legacy databases have the migration in history but are missing this
    // column physically. The admin order timeline depends on it.
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"OrderStatusHistories\" ADD COLUMN IF NOT EXISTS \"EventType\" character varying(32) NOT NULL DEFAULT 'status';");
    // The same legacy migration-history issue can leave the internal order-notes
    // table absent, which must not make the order detail page fail.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "OrderNotes" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "OrderId" integer NOT NULL REFERENCES "Orders"("Id") ON DELETE CASCADE,
            "Content" character varying(2000) NOT NULL,
            "CreatedBy" text NOT NULL DEFAULT '',
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "LastModifiedBy" text,
            "LastModifiedAt" timestamp with time zone,
            "IsDeleted" boolean NOT NULL DEFAULT FALSE,
            "IsActive" boolean NOT NULL DEFAULT TRUE
        );
        """);
    await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_OrderNotes_OrderId_CreatedAt\" ON \"OrderNotes\" (\"OrderId\", \"CreatedAt\");");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductRelateds\" ADD COLUMN IF NOT EXISTS \"RecommendationType\" integer NOT NULL DEFAULT 0;");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductRelateds\" ADD COLUMN IF NOT EXISTS \"ShowOnProductPage\" boolean NOT NULL DEFAULT TRUE;");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductRelateds\" ADD COLUMN IF NOT EXISTS \"ShowInCart\" boolean NOT NULL DEFAULT TRUE;");
    // Checkout currently uses payment method ID 1 for the PayTR flow. Keep the
    // reference data self-healing so a fresh production database can accept payments.
    var paytrPaymentMethod = await db.PaymentMethods.SingleOrDefaultAsync(method => method.Id == 1);
    if (paytrPaymentMethod == null)
    {
        db.PaymentMethods.Add(new PaymentMethod
        {
            Id = 1,
            Name = "PayTR",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        });
        await db.SaveChangesAsync();
        app.Logger.LogInformation("PayTR ödeme yöntemi oluşturuldu.");
    }
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ShippingCheckoutSettings" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "IsMethodSelectionEnabled" boolean NOT NULL DEFAULT FALSE,
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "CreatedBy" text NULL,
            "IsActive" boolean NOT NULL DEFAULT FALSE,
            "LastModifiedAt" timestamp with time zone NULL,
            "LastModifiedBy" text NULL,
            "IsDeleted" boolean NOT NULL DEFAULT FALSE
        );
        """);
    var contentSanitizer = new HtmlSanitizer();
    var cmsPages = await db.Set<CmsPage>().ToListAsync();
    var blogPosts = await db.Set<BlogPost>().ToListAsync();
    var contentChanged = false;
    foreach (var page in cmsPages)
    {
        var sanitized = contentSanitizer.Sanitize(page.ContentHtml ?? string.Empty);
        if (page.ContentHtml == sanitized) continue;
        page.ContentHtml = sanitized;
        contentChanged = true;
    }
    foreach (var post in blogPosts)
    {
        var sanitized = contentSanitizer.Sanitize(post.Content ?? string.Empty);
        if (post.Content == sanitized) continue;
        post.Content = sanitized;
        contentChanged = true;
    }
    if (contentChanged)
    {
        await db.SaveChangesAsync();
        app.Logger.LogInformation("Mevcut HTML içerikleri güvenli biçimde temizlendi.");
    }

    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"]?.Trim();
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(bootstrapEmail) != string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        throw new InvalidOperationException("BootstrapAdmin:Email ve BootstrapAdmin:Password birlikte tanımlanmalıdır.");
    }

    if (!string.IsNullOrWhiteSpace(bootstrapEmail))
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Admin rolü oluşturulamadı: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
        }

        var admin = await userManager.FindByEmailAsync(bootstrapEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = bootstrapEmail.ToLowerInvariant(),
                Email = bootstrapEmail,
                EmailConfirmed = true,
                FirstName = builder.Configuration["BootstrapAdmin:FirstName"]?.Trim() ?? "Site",
                LastName = builder.Configuration["BootstrapAdmin:LastName"]?.Trim() ?? "Yöneticisi",
                IsActive = true,
                IsGuest = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "BootstrapAdmin"
            };

            var createResult = await userManager.CreateAsync(admin, bootstrapPassword!);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Başlangıç admin kullanıcısı oluşturulamadı: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");

            app.Logger.LogInformation("Başlangıç admin kullanıcısı oluşturuldu: {Email}", bootstrapEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!addRoleResult.Succeeded)
                throw new InvalidOperationException($"Başlangıç admin kullanıcısına rol atanamadı: {string.Join("; ", addRoleResult.Errors.Select(error => error.Description))}");
        }
    }

    app.Logger.LogInformation("Veritabanı hazır.");
}

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

app.UseCors("CorsPolicy");
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

/* GLOBAL EXCEPTION */


app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "Unhandled exception | Path: {Path} | Method: {Method}",
            context.Request.Path,
            context.Request.Method
        );

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal Server Error"
        });
    }
});

/* STATIC FILES */
var uploadPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "wwwroot",
    "uploads",
    "images"
);

Directory.CreateDirectory(uploadPath);

// Serves campaign device photos from wwwroot/uploads/campaigns.
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/api/uploads/images"
});


app.MapGet("/health", () => "Healthy");

app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Run();

static RateLimitPartition<string> CreateFixedWindowLimiter(HttpContext context, int permitLimit, TimeSpan window) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        });
