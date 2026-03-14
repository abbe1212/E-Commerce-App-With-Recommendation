using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.ViewModels;
using Ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.OutputCaching;

namespace Ecoomerce.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<ProductController> _logger;
        private readonly IActivityLogService _activityLogService;
        private readonly IMemoryCache _cache;
        private readonly IRecommendationService _recommendationService;

        public ProductController(
            IProductService productService,
            IReviewService reviewService,
            ICartService cartService,
            IWishlistService wishlistService,
            ICategoryRepository categoryRepository,
            ILogger<ProductController> logger,
            IActivityLogService activityLogService,
            IMemoryCache cache,
            IRecommendationService recommendationService)
        {
            _productService = productService;
            _reviewService = reviewService;
            _cartService = cartService;
            _wishlistService = wishlistService;
            _categoryRepository = categoryRepository;
            _logger = logger;
            _activityLogService = activityLogService;
            _cache = cache;
            _recommendationService = recommendationService;
        }


        // Action for the main products page (handles searching and filtering)
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "searchTerm", "categoryId", "brandId", "minPrice", "maxPrice", "sortBy", "page", "pageSize" })]
        public async Task<IActionResult> Index(string? searchTerm, int? categoryId, int? brandId, decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1, int pageSize = 12)
        {
            try 
            {
                _logger.LogInformation("Fetching products with search term: {SearchTerm}, category ID: {CategoryId}, page: {Page}", searchTerm, categoryId, page);
                
                // Validate pageSize
                pageSize = pageSize switch
                {
                    12 or 24 or 48 => pageSize,
                    _ => 12
                };

                // Fetch categories for filter dropdown with caching (10 minutes)
                var categories = await _cache.GetOrCreateAsync("all_categories", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    return await _categoryRepository.ListAllAsync();
                });
                
                var categoryList = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryID.ToString(),
                    Text = c.Name,
                    Selected = c.CategoryID == categoryId
                }).ToList();

                // Use server-side paginated search - ALL filtering/sorting/paging done in SQL
                var (products, totalCount) = await _productService.SearchPagedAsync(
                    searchTerm, categoryId, brandId, minPrice, maxPrice, sortBy, page, pageSize);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var viewModel = new ProductSearchViewModel
                {
                    Products = products.ToList(),
                    Categories = categoryList,
                    SearchTerm = searchTerm,
                    CategoryId = categoryId,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    SortBy = sortBy,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products.");
                TempData["Error"] = "An error occurred while loading products. Please try again.";
                return View(new ProductSearchViewModel());
            }
        }

        // Search Suggestions for Autocomplete
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { suggestions = new object[0] });
            }

            try
            {
                var cacheKey = $"search_suggestions_{query.ToLower().Trim()}";
                var suggestions = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                    var (products, _) = await _productService.SearchPagedAsync(
                        query, null, null, null, null, null, 1, 8);
                    return products
                        .Select(p => new
                        {
                            productId = p.ProductID,
                            name = p.Name,
                            price = p.Price,
                            imageUrl = p.ImageURL,
                            categoryName = p.CategoryName
                        })
                        .ToList<object>();
                });

                return Json(new { suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search suggestions");
                return Json(new { suggestions = new object[0] });
            }
        }


        // Action for product details page
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Fetching product details for ID: {ProductId}", id);
                
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found", id);
                    return NotFound();
                }

                // Track recently viewed products
                AddToRecentlyViewed(id);

                // Get recommended products (related products)
                var recommendedProducts = (await _recommendationService.GetRelatedProductsAsync(id, 6))
                    .Where(p => p.ProductID != id).Take(4).ToList();

                // Get recently viewed products (using batch fetch)
                var recentlyViewedIds = GetRecentlyViewedIds().Where(rvId => rvId != id).Take(4).ToList();
                var recentlyViewedProductsList = recentlyViewedIds.Any() 
                    ? (await _productService.GetByIdsAsync(recentlyViewedIds)).ToList()
                    : new List<Ecommerce.Application.DTOs.Products.ProductDto>();

                // Check if product is in wishlist (only for authenticated users)
                bool isInWishlist = false;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, id);
                    }
                }

                var viewModel = new ProductDetailsViewModel
                {
                    Product = product,
                    RecommendedProducts = recommendedProducts,
                    RecentlyViewedProducts = recentlyViewedProductsList,
                    IsInWishlist = isInWishlist
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching product details for ID: {ProductId}", id);
                TempData["Error"] = "An error occurred while loading product details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to add product to recently viewed
        private void AddToRecentlyViewed(int productId)
        {
            var recentlyViewed = GetRecentlyViewedIds().ToList();
            
            // Remove if already exists
            recentlyViewed.Remove(productId);
            
            // Add to beginning
            recentlyViewed.Insert(0, productId);
            
            // Keep only last 20
            if (recentlyViewed.Count > 20)
                recentlyViewed = recentlyViewed.Take(20).ToList();
            
            // Store in cookie
            Response.Cookies.Append("RecentlyViewed", string.Join(",", recentlyViewed), 
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });
        }

        // Helper method to get recently viewed product IDs
        private IEnumerable<int> GetRecentlyViewedIds()
        {
            var cookie = Request.Cookies["RecentlyViewed"];
            if (string.IsNullOrEmpty(cookie))
                return Enumerable.Empty<int>();
            
            return cookie.Split(',')
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse);
        }


        // Action for category-specific product listing
        public async Task<IActionResult> Category(int categoryId, int page = 1)
        {
            try
            {
                _logger.LogInformation("Fetching products for category ID: {CategoryId}, page: {Page}", categoryId, page);
                
                var products = await _productService.GetProductsByCategoryAsync(categoryId);
                
                // Pagination
                const int pageSize = 12;
                var totalProducts = products.Count();
                var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
                var paginatedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var viewModel = new ProductSearchViewModel
                {
                    Products = paginatedProducts,
                    CategoryId = categoryId,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products for category ID: {CategoryId}", categoryId);
                TempData["Error"] = "An error occurred while loading category products. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX action for quick product search
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Json(new { success = false, message = "Search term too short" });
                }

                var products = await _productService.SearchProductAsync(term);
                var results = products.Take(5).Select(p => new
                {
                    id = p.ProductID,
                    name = p.Name,
                    price = p.Price.ToString("C"),
                    imageUrl = p.ImageURL,
                    url = Url.Action("Details", new { id = p.ProductID })
                });

                return Json(new { success = true, products = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during quick search for term: {SearchTerm}", term);
                return Json(new { success = false, message = "Search failed" });
            }
        }

        // Add product to cart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation("Adding product {ProductId} to cart for user {UserId}", productId, userId);

                // Verify product exists
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (!product.IsAvailable)
                {
                    return Json(new { success = false, message = "Product is out of stock" });
                }

                await _cartService.AddItemToCartAsync(userId, productId, quantity);

                // Log cart activity
                await _activityLogService.LogActivityAsync(
                    userId,
                    "CartItemAdded",
                    "Product",
                    productId,
                    $"Added product '{product.Name}' to cart (Quantity: {quantity})"
                );

                return Json(new { success = true, message = "Product added to cart successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                return Json(new { success = false, message = "Failed to add product to cart" });
            }
        }

        // Toggle wishlist (add/remove)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation("Toggling wishlist for product {ProductId} and user {UserId}", productId, userId);

                // Check if product exists
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if already in wishlist
                bool isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, productId);

                if (isInWishlist)
                {
                    await _wishlistService.RemoveFromWishlistAsync(userId, productId);
                    
                    // Log wishlist removal activity
                    await _activityLogService.LogActivityAsync(
                        userId,
                        "WishlistItemRemoved",
                        "Product",
                        productId,
                        $"Removed product '{product.Name}' from wishlist"
                    );
                    
                    return Json(new { success = true, isInWishlist = false, message = "Removed from wishlist" });
                }
                else
                {
                    await _wishlistService.AddToWishlistAsync(userId, productId);
                    
                    // Log wishlist addition activity
                    await _activityLogService.LogActivityAsync(
                        userId,
                        "WishlistItemAdded",
                        "Product",
                        productId,
                        $"Added product '{product.Name}' to wishlist"
                    );
                    
                    return Json(new { success = true, isInWishlist = true, message = "Added to wishlist!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling wishlist for product {ProductId}", productId);
                return Json(new { success = false, message = "Failed to update wishlist" });
            }
        }

        // Check if product is in wishlist
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, isInWishlist = false });
                }

                bool isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, productId);
                return Json(new { success = true, isInWishlist });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking wishlist for product {ProductId}", productId);
                return Json(new { success = false, isInWishlist = false });
            }
        }

        // Get featured products - View
        [HttpGet]
        public async Task<IActionResult> Featured(int count = 12)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} featured products", count);
                var products = await _productService.GetFeaturedProductsAsync(count);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                TempData["Error"] = "Failed to load featured products";
                return RedirectToAction(nameof(Index));
            }
        }

        // Get top selling products - View
        [HttpGet]
        public async Task<IActionResult> TopSelling(int count = 12)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} top selling products", count);
                var products = await _productService.GetTopSellingProductsAsync(count);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top selling products");
                TempData["Error"] = "Failed to load top selling products";
                return RedirectToAction(nameof(Index));
            }
        }

        // Get latest products - View
        [HttpGet]
        public async Task<IActionResult> Latest(int count = 12)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} latest products", count);
                // Use server-side method to get latest products
                var latestProducts = await _productService.GetLatestProductsAsync(count);
                return View(latestProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest products");
                TempData["Error"] = "Failed to load latest products";
                return RedirectToAction(nameof(Index));
            }
        }

        // Get products on sale - View
        [HttpGet]
        public async Task<IActionResult> OnSale(int count = 24)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} products on sale", count);
                // Use server-side method to get products on sale
                var saleProducts = await _productService.GetOnSaleProductsAsync(count);
                return View(saleProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sale products");
                TempData["Error"] = "Failed to load sale products";
                return RedirectToAction(nameof(Index));
            }
        }

        // Get featured products - AJAX/API
        [HttpGet]
        public async Task<IActionResult> GetFeaturedProducts(int count = 8)
        {
            try
            {
                var products = await _productService.GetFeaturedProductsAsync(count);
                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                return Json(new { success = false, message = "Failed to load featured products" });
            }
        }

        // Get top selling products - AJAX/API
        [HttpGet]
        public async Task<IActionResult> GetTopSellingProducts(int count = 8)
        {
            try
            {
                var products = await _productService.GetTopSellingProductsAsync(count);
                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top selling products");
                return Json(new { success = false, message = "Failed to load top selling products" });
            }
        }

        // Compare products
        [HttpGet]
        public async Task<IActionResult> Compare(int[] productIds)
        {
            try
            {
                if (productIds == null || productIds.Length < 2)
                {
                    TempData["Error"] = "Please select at least 2 products to compare";
                    return RedirectToAction(nameof(Index));
                }

                if (productIds.Length > 4)
                {
                    TempData["Error"] = "You can compare up to 4 products at a time";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Comparing products: {ProductIds}", string.Join(", ", productIds));

                var products = new List<Ecommerce.Application.DTOs.Products.ProductDto>();
                foreach (var id in productIds)
                {
                    var product = await _productService.GetProductByIdAsync(id);
                    if (product != null)
                    {
                        products.Add(product);
                    }
                }

                if (products.Count < 2)
                {
                    TempData["Error"] = "Not enough valid products to compare";
                    return RedirectToAction(nameof(Index));
                }

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing products");
                TempData["Error"] = "An error occurred while comparing products";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
