using AutoMapper;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.ViewModels.Admin_Panel;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ecoomerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductManagementController : Controller
    {
        private readonly IProductService _productService;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductManagementController> _logger;
        private readonly AppDbContext _context;

        public ProductManagementController(
            IProductService productService,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IMapper mapper,
            ILogger<ProductManagementController> logger,
            AppDbContext context)
        {
            _productService = productService;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _mapper = mapper;
            _logger = logger;
            _context = context;
        }

        // GET: Product Management Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                var viewModel = new ManageProductsViewModel
                {
                    Products = products.ToList()
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Failed to load products.";
                return View(new ManageProductsViewModel { Products = new List<Ecommerce.Application.DTOs.Products.ProductDto>() });
            }
        }
        [HttpGet]
        [AllowAnonymous] // Temporary - for testing
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("=== CREATE PRODUCT PAGE REQUESTED ===");
                _logger.LogInformation("User: {User}, IsAuthenticated: {IsAuth}", 
                    User?.Identity?.Name ?? "Anonymous", 
                    User?.Identity?.IsAuthenticated ?? false);
                
                var categories = await GetCategoriesSelectList();
                var brands = await GetBrandsSelectList();
                
                _logger.LogInformation("Loaded {CategoryCount} categories and {BrandCount} brands", 
                    categories.Count, brands.Count);
                
                if (categories.Count == 0)
                {
                    _logger.LogWarning("No categories found in database!");
                    TempData["ErrorMessage"] = "No categories available. Please create categories first.";
                }
                
                if (brands.Count == 0)
                {
                    _logger.LogWarning("No brands found in database!");
                }
                
                var viewModel = new EditProductViewModel
                {
                    Categories = categories,
                    Brands = brands,
                    Images = new List<Ecommerce.Application.DTOs.Products.ProductImageDto>(),
                    Variants = new List<Ecommerce.Application.DTOs.Products.ProductVariantDto>()
                };
                
                _logger.LogInformation("Returning Create view with model");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR IN CREATE PRODUCT ===");
                _logger.LogError("Exception Type: {Type}", ex.GetType().Name);
                _logger.LogError("Message: {Message}", ex.Message);
                _logger.LogError("StackTrace: {StackTrace}", ex.StackTrace);
                
                TempData["ErrorMessage"] = $"Error loading create page: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Handles the submission of the new product form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EditProductViewModel model, IFormFile? imageUpload, IFormFileCollection? GalleryImages)
        {
            // Debug: Log all model values
            _logger.LogInformation("Create Product - Received model: Name={Name}, Price={Price}, CategoryID={CategoryID}, BrandID={BrandID}, StockQuantity={StockQuantity}, ImageURL={ImageURL}, ImageUpload={HasImage}", 
                model.Name, model.Price, model.CategoryID, model.BrandID, model.StockQuantity, model.ImageURL ?? "NULL", imageUpload != null);

            if (!ModelState.IsValid)
            {
                // Debug: Log all validation errors
                _logger.LogWarning("Model validation failed for product creation:");
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        _logger.LogWarning("Validation Error - Field: {Field}, Error: {Error}", error.Key, subError.ErrorMessage);
                    }
                }

                // Repopulate dropdowns if validation fails
                model.Categories = await GetCategoriesSelectList();
                model.Brands = await GetBrandsSelectList();
                model.Images = new List<Ecommerce.Application.DTOs.Products.ProductImageDto>();
                model.Variants = new List<Ecommerce.Application.DTOs.Products.ProductVariantDto>();
                
                // Add validation errors to TempData for debugging
                TempData["ValidationErrors"] = string.Join("; ", ModelState
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => $"{x.ErrorMessage}"));
                
                return View(model);
            }

            try
            {
                _logger.LogInformation("Attempting to create product: {ProductName}", model.Name);
                _logger.LogInformation("File upload received: HasFile={HasFile}, FileName={FileName}, Form ImageURL={ImageURL}", 
                    imageUpload != null, imageUpload?.FileName ?? "None", model.ImageURL ?? "NULL");
                
                // Map ViewModel to Entity
                // Use default placeholder image ONLY if no URL provided AND no file uploaded
                var defaultImageUrl = "https://via.placeholder.com/400x400?text=No+Image";
                var finalImageUrl = defaultImageUrl;
                
                // Handle file upload if present
                if (imageUpload != null && imageUpload.Length > 0)
                {
                    // Generate a unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUpload.FileName);
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    
                    // Ensure directory exists
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageUpload.CopyToAsync(fileStream);
                    }
                    
                    // Set the image URL to the uploaded file path
                    finalImageUrl = $"/images/products/{uniqueFileName}";
                    _logger.LogInformation("Image uploaded successfully: {ImagePath}", finalImageUrl);
                }
                // Check if we have an image URL from the form
                else if (!string.IsNullOrWhiteSpace(model.ImageURL))
                {
                    finalImageUrl = model.ImageURL;
                }
                
                _logger.LogInformation("ImageURL processing: Original='{Original}', Final='{Final}', IsEmpty={IsEmpty}", 
                    model.ImageURL ?? "NULL", finalImageUrl, string.IsNullOrWhiteSpace(model.ImageURL));
                
                var product = new Ecommerce.Core.Entities.Product
                {
                    Name = model.Name,
                    Description = model.Description ?? string.Empty,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    ImageURL = finalImageUrl, // Use the processed image URL instead of model.ImageURL
                    CategoryID = model.CategoryID,
                    BrandID = model.BrandID,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                _logger.LogInformation("Product entity created: Name='{Name}', ImageURL='{ImageUrl}', Price={Price}", 
                    product.Name, product.ImageURL, product.Price);

                _logger.LogInformation("Product entity created, calling repository AddAsync...");
                
                // Save to database
                var savedProduct = await _productRepository.AddAsync(product);

                // Save Gallery Images (file uploads)
                if (GalleryImages != null && GalleryImages.Count > 0)
                {
                    int order = 0;
                    string galleryFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", "gallery");
                    
                    if (!Directory.Exists(galleryFolder))
                    {
                        Directory.CreateDirectory(galleryFolder);
                    }
                    
                    foreach (var file in GalleryImages)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueFileName = $"{savedProduct.ProductID}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            string filePath = Path.Combine(galleryFolder, uniqueFileName);
                            
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            
                            var productImage = new ProductImage
                            {
                                ProductID = savedProduct.ProductID,
                                ImageURL = $"/images/products/gallery/{uniqueFileName}",
                                DisplayOrder = order,
                                IsPrimary = order == 0,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ProductImages.Add(productImage);
                            order++;
                        }
                    }
                }

                // Save Product Variants
                if (model.NewVariants != null && model.NewVariants.Any())
                {
                    int variantOrder = 0;
                    foreach (var v in model.NewVariants.Where(x => !string.IsNullOrWhiteSpace(x.VariantValue)))
                    {
                        var variant = new ProductVariant
                        {
                            ProductID = savedProduct.ProductID,
                            VariantType = v.VariantType,
                            VariantValue = v.VariantValue,
                            PriceAdjustment = v.PriceAdjustment,
                            StockQuantity = v.StockQuantity,
                            ColorCode = v.ColorCode,
                            IsAvailable = v.IsAvailable,
                            DisplayOrder = variantOrder++,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ProductVariants.Add(variant);
                    }
                }

                // Save images and variants to database
                await _context.SaveChangesAsync();


                _logger.LogInformation("Product saved successfully with ID: {ProductId}, ImageURL: '{ImageUrl}'", savedProduct.ProductID, savedProduct.ImageURL);
                
                // Verify the product was saved by retrieving it
                var verifyProduct = await _productRepository.GetByIdAsync(savedProduct.ProductID);
                if (verifyProduct != null)
                {
                    _logger.LogInformation("Product verification successful: {ProductName} (ID: {ProductId}), ImageURL: '{ImageUrl}'", 
                        verifyProduct.Name, verifyProduct.ProductID, verifyProduct.ImageURL);
                    TempData["SuccessMessage"] = $"Product '{model.Name}' created successfully with ID {savedProduct.ProductID}!";
                }
                else
                {
                    _logger.LogWarning("Product was created but verification failed for ID: {ProductId}", savedProduct.ProductID);
                    TempData["SuccessMessage"] = $"Product '{model.Name}' created but verification failed!";
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR CREATING PRODUCT ===");
                _logger.LogError("Product Name: {Name}", model.Name);
                _logger.LogError("Exception Type: {Type}", ex.GetType().FullName);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Inner Exception: {Inner}", ex.InnerException?.Message ?? "None");
                _logger.LogError("Stack Trace: {Stack}", ex.StackTrace);
                
                // Show detailed error to user (in development)
                var errorDetails = $"Failed to create product. Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetails += $" | Inner: {ex.InnerException.Message}";
                }
                
                TempData["ErrorMessage"] = errorDetails;
                model.Categories = await GetCategoriesSelectList();
                model.Brands = await GetBrandsSelectList();
                return View(model);
            }
        }

        // GET: Edit Product
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                // Load existing images and variants
                var productImages = await _context.ProductImages
                    .Where(i => i.ProductID == id)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();
                    
                var productVariants = await _context.ProductVariants
                    .Where(v => v.ProductID == id)
                    .OrderBy(v => v.DisplayOrder)
                    .ToListAsync();

                var viewModel = new EditProductViewModel
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageURL = product.ImageURL,
                    CategoryID = product.CategoryID,
                    BrandID = product.BrandID,
                    Categories = await GetCategoriesSelectList(),
                    Brands = await GetBrandsSelectList(),
                    Images = _mapper.Map<List<Ecommerce.Application.DTOs.Products.ProductImageDto>>(productImages),
                    Variants = _mapper.Map<List<Ecommerce.Application.DTOs.Products.ProductVariantDto>>(productVariants)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId} for editing", id);
                TempData["ErrorMessage"] = "Failed to load product for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: Edit Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProductViewModel model, IFormFileCollection? GalleryImages)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesSelectList();
                model.Brands = await GetBrandsSelectList();
                return View(model);
            }

            try
            {
                // Get existing product
                var existingProduct = await _productRepository.GetByIdAsync(model.ProductID);
                if (existingProduct == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                // Update product properties
                var defaultImageUrl = "https://via.placeholder.com/400x400?text=No+Image";
                existingProduct.Name = model.Name;
                existingProduct.Description = model.Description ?? string.Empty;
                existingProduct.Price = model.Price;
                existingProduct.StockQuantity = model.StockQuantity;
                existingProduct.ImageURL = string.IsNullOrWhiteSpace(model.ImageURL) ? defaultImageUrl : model.ImageURL;
                existingProduct.CategoryID = model.CategoryID;
                existingProduct.BrandID = model.BrandID;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Save product changes
                await _productRepository.UpdateAsync(existingProduct);

                // Handle deleted images
                if (Request.Form.ContainsKey("DeletedImageIds"))
                {
                    var deletedImageIds = Request.Form["DeletedImageIds"]
                        .SelectMany(x => x?.Split(',') ?? Array.Empty<string>())
                        .Where(x => int.TryParse(x, out _))
                        .Select(int.Parse)
                        .ToList();
                    var imagesToDelete = await _context.ProductImages
                        .Where(i => deletedImageIds.Contains(i.ImageID))
                        .ToListAsync();
                    _context.ProductImages.RemoveRange(imagesToDelete);
                }

                // Handle deleted variants
                if (Request.Form.ContainsKey("DeletedVariantIds"))
                {
                    var deletedVariantIds = Request.Form["DeletedVariantIds"]
                        .SelectMany(x => x?.Split(',') ?? Array.Empty<string>())
                        .Where(x => int.TryParse(x, out _))
                        .Select(int.Parse)
                        .ToList();
                    var variantsToDelete = await _context.ProductVariants
                        .Where(v => deletedVariantIds.Contains(v.VariantID))
                        .ToListAsync();
                    _context.ProductVariants.RemoveRange(variantsToDelete);
                }

                // Add new images (file uploads)
                if (GalleryImages != null && GalleryImages.Count > 0)
                {
                    var existingCount = await _context.ProductImages.CountAsync(i => i.ProductID == model.ProductID);
                    int order = existingCount;
                    string galleryFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", "gallery");
                    
                    if (!Directory.Exists(galleryFolder))
                    {
                        Directory.CreateDirectory(galleryFolder);
                    }
                    
                    foreach (var file in GalleryImages)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueFileName = $"{model.ProductID}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            string filePath = Path.Combine(galleryFolder, uniqueFileName);
                            
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            
                            var productImage = new ProductImage
                            {
                                ProductID = model.ProductID,
                                ImageURL = $"/images/products/gallery/{uniqueFileName}",
                                DisplayOrder = order,
                                IsPrimary = existingCount == 0 && order == 0,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ProductImages.Add(productImage);
                            order++;
                        }
                    }
                }

                // Add new variants
                if (model.NewVariants != null && model.NewVariants.Any())
                {
                    var existingVariantCount = await _context.ProductVariants.CountAsync(v => v.ProductID == model.ProductID);
                    int variantOrder = existingVariantCount;
                    foreach (var v in model.NewVariants.Where(x => !string.IsNullOrWhiteSpace(x.VariantValue)))
                    {
                        var variant = new ProductVariant
                        {
                            ProductID = model.ProductID,
                            VariantType = v.VariantType,
                            VariantValue = v.VariantValue,
                            PriceAdjustment = v.PriceAdjustment,
                            StockQuantity = v.StockQuantity,
                            ColorCode = v.ColorCode,
                            IsAvailable = v.IsAvailable,
                            DisplayOrder = variantOrder++,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ProductVariants.Add(variant);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product '{ProductName}' (ID: {ProductId}) updated with images/variants.", model.Name, model.ProductID);
                TempData["SuccessMessage"] = $"Product '{model.Name}' updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product '{ProductName}' (ID: {ProductId})", model.Name, model.ProductID);
                TempData["ErrorMessage"] = "Failed to update product. Please try again.";
                model.Categories = await GetCategoriesSelectList();
                model.Brands = await GetBrandsSelectList();
                return View(model);
            }
        }

        // GET: Product Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Failed to load product details.";
                return RedirectToAction("Index");
            }
        }

        // POST: Delete Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Delete the product
                await _productRepository.DeleteAsync(product);

                _logger.LogInformation("Product '{ProductName}' (ID: {ProductId}) deleted.", product.Name, id);
                return Json(new { success = true, message = $"Product '{product.Name}' deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                return Json(new { success = false, message = "Failed to delete product. Please try again." });
            }
        }

        // AJAX: Toggle Product Status
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Toggle the IsAvailable status
                product.IsAvailable = !product.IsAvailable;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);

                _logger.LogInformation("Product status toggled for '{ProductName}' (ID: {ProductId}) to {Status}", product.Name, id, product.IsAvailable ? "Available" : "Unavailable");
                return Json(new { success = true, message = $"Product status updated to {(product.IsAvailable ? "Available" : "Unavailable")}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for product ID: {ProductId}", id);
                return Json(new { success = false, message = "Failed to update product status." });
            }
        }

        // Private helper methods to populate dropdowns
        private async Task<List<SelectListItem>> GetCategoriesSelectList()
        {
            var categories = await _categoryRepository.ListAllAsync();
            return categories.Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.Name }).ToList();
        }

        private async Task<List<SelectListItem>> GetBrandsSelectList()
        {
            var brands = await _brandRepository.ListAllAsync();
            return brands.Select(b => new SelectListItem { Value = b.BrandID.ToString(), Text = b.Name }).ToList();
        }
    }
}
