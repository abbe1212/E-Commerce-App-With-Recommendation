using Ecommerce.Application.Mapping;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthenticationService = Ecommerce.Application.Services.Implementations.AuthenticationService;
using IAuthenticationService = Ecommerce.Application.Services.Interfaces.IAuthenticationService;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// AutoMapper Registration
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookies 
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Unit of Work & Repositories Registration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); //  Unit of Work 
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Register the generic repository ONCE

// Register all specific repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IShippingRepository, ShippingRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IInventoryLogRepository, InventoryLogRepository>();
builder.Services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();


// Business Services Registration
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Reporting Services Registration
builder.Services.AddScoped<IInventoryReportService, InventoryReportService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IUserReportService, UserReportService>();

// Email Service Registration
builder.Services.AddTransient<IEmailSenderService, SmtpEmailSenderService>();

// File Upload Service Registration
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
    });
    // Note: Apple Sign-In requires AspNet.Security.OAuth.Apple package
    // Uncomment when ready to implement:
    // .AddApple(options =>
    // {
    //     options.ClientId = builder.Configuration["Authentication:Apple:ClientId"] ?? "";
    //     options.KeyId = builder.Configuration["Authentication:Apple:KeyId"] ?? "";
    //     options.TeamId = builder.Configuration["Authentication:Apple:TeamId"] ?? "";
    // });

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Area Routes ProfileArea
app.MapControllerRoute(
    name: "ProfileArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Area Routes ReportingArea
app.MapControllerRoute(
    name: "ReportingArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


// Area Routes AdminArea
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


// --- Run the Database Seeder ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Get the necessary services
        var context = services.GetRequiredService<AppDbContext>();

        // Ensure the database is created
        await context.Database.MigrateAsync();

        // Call the initializer
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();



// CreateHostBuilder for EF Core Design-Time Tools
// This is critical for `dotnet ef migrations add` to work correctly.
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Program>();
        });
