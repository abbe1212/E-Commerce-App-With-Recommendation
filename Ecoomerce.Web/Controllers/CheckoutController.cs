using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.ViewModels;
using Ecommerce.Application.DTOs.Cart;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Ecoomerce.Web.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IActivityLogService _activityLogService;
        private readonly IPromoCodeService _promoCodeService;
        private readonly IShippingService _shippingService;
        private readonly IEmailSenderService _emailSenderService;

        public CheckoutController(
            ICartService cartService,
            IProductService productService,
            IOrderService orderService,
            ILogger<CheckoutController> logger,
            IActivityLogService activityLogService,
            IPromoCodeService promoCodeService,
            IShippingService shippingService,
            IEmailSenderService emailSenderService)
        {
            _cartService = cartService;
            _productService = productService;
            _orderService = orderService;
            _logger = logger;
            _activityLogService = activityLogService;
            _promoCodeService = promoCodeService;
            _shippingService = shippingService;
            _emailSenderService = emailSenderService;
        }

        // Step 1: Cart Summary
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _cartService.GetOrCreateCartAsync(userId);

                // Populate product details for each cart item
                foreach (var item in cart.Items)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductID);
                    if (product != null)
                    {
                        item.ProductName = product.Name;
                        item.ImageURL = product.ImageURL;
                        item.UnitPrice = product.Price;
                    }
                }

                var viewModel = new CheckoutViewModel
                {
                    Cart = new CartViewModel
                    {
                        Items = cart.Items,
                        Tax = cart.Tax,
                        Shipping = cart.Shipping,
                        Discount = cart.Discount
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart for checkout");
                TempData["Error"] = "Failed to load checkout. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // Step 2: Shipping Information
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippingInfo(CheckoutViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Reload cart from database to ensure we have the items
                var cart = await _cartService.GetOrCreateCartAsync(userId);

                // Check if cart is empty
                if (cart == null || cart.Items == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Your cart is empty. Please add items to your cart before checking out.";
                    return RedirectToAction("Index", "Cart");
                }

                // Populate product details for each cart item
                foreach (var item in cart.Items)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductID);
                    if (product != null)
                    {
                        item.ProductName = product.Name;
                        item.ImageURL = product.ImageURL;
                        item.UnitPrice = product.Price;
                    }
                }

                // Store cart info in TempData
                TempData["CartItems"] = System.Text.Json.JsonSerializer.Serialize(cart.Items);
                TempData["CartSubTotal"] = cart.SubTotal;
                TempData["CartTax"] = cart.Tax;
                TempData["CartShipping"] = cart.Shipping;
                TempData["CartDiscount"] = cart.Discount;
                
                // Store new fields from the form
                TempData["OrderNotes"] = model.OrderNotes ?? "";
                TempData["PromoCode"] = model.PromoCode ?? "";

                // Ensure model.Cart is initialized
                if (model.Cart == null)
                {
                    model.Cart = new CartViewModel();
                }

                // Ensure Items list is initialized
                if (model.Cart.Items == null)
                {
                    model.Cart.Items = new List<CartItemDto>();
                }

                // Update model with cart data
                model.Cart.Items = cart.Items;
                model.Cart.Tax = cart.Tax;
                model.Cart.Shipping = cart.Shipping;
                model.Cart.Discount = cart.Discount;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shipping info");
                TempData["Error"] = $"Failed to load shipping information: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Step 3: Payment Method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaymentMethod(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ShippingInfo", model);
            }

            // Store shipping info in TempData
            TempData["FullName"] = model.FullName;
            TempData["Email"] = model.Email;
            TempData["Phone"] = model.Phone;
            TempData["ShippingAddress"] = model.ShippingAddress;
            TempData["City"] = model.City;
            TempData["State"] = model.State;
            TempData["ZipCode"] = model.ZipCode;
            TempData["Country"] = model.Country;
            TempData["UseShippingAsBilling"] = model.UseShippingAsBilling;

            if (!model.UseShippingAsBilling)
            {
                TempData["BillingAddress"] = model.BillingAddress;
            }

            // Restore cart info from TempData
            if (TempData["CartItems"] != null)
            {
                model.Cart.Items = JsonSerializer.Deserialize<List<CartItemDto>>(TempData["CartItems"].ToString());
                TempData.Keep("CartItems");
            }

            // SubTotal is a calculated property
            TempData.Keep("CartSubTotal");

            if (TempData["CartTax"] != null)
            {
                model.Cart.Tax = Convert.ToDecimal(TempData["CartTax"]);
                TempData.Keep("CartTax");
            }

            if (TempData["CartShipping"] != null)
            {
                model.Cart.Shipping = Convert.ToDecimal(TempData["CartShipping"]);
                TempData.Keep("CartShipping");
            }

            if (TempData["CartDiscount"] != null)
            {
                model.Cart.Discount = Convert.ToDecimal(TempData["CartDiscount"]);
                TempData.Keep("CartDiscount");
            }
            
            // Store shipping method
            TempData["ShippingMethod"] = model.ShippingMethod;
            
            // Keep other fields
            TempData.Keep("OrderNotes");
            TempData.Keep("PromoCode");

            return View(model);
        }

        // Step 4: Order Confirmation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OrderConfirmation(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("PaymentMethod", model);
            }

            // Store payment info in TempData
            TempData["PaymentMethod"] = model.PaymentMethod;

            if (model.PaymentMethod == "creditCard")
            {
                // Store masked card information for security
                if (!string.IsNullOrEmpty(model.CardNumber) && model.CardNumber.Length >= 4)
                {
                    TempData["CardLastFour"] = model.CardNumber.Substring(model.CardNumber.Length - 4);
                }
                TempData["CardExpiry"] = model.CardExpiry;
            }

            // Restore shipping info from TempData
            model.FullName = TempData["FullName"]?.ToString();
            model.Email = TempData["Email"]?.ToString();
            model.Phone = TempData["Phone"]?.ToString();
            model.ShippingAddress = TempData["ShippingAddress"]?.ToString();
            model.City = TempData["City"]?.ToString();
            model.State = TempData["State"]?.ToString();
            model.ZipCode = TempData["ZipCode"]?.ToString();
            model.Country = TempData["Country"]?.ToString();
            model.UseShippingAsBilling = Convert.ToBoolean(TempData["UseShippingAsBilling"]);

            if (!model.UseShippingAsBilling)
            {
                model.BillingAddress = TempData["BillingAddress"]?.ToString();
            }

            // Restore cart info from TempData
            if (TempData["CartItems"] != null)
            {
                model.Cart.Items = JsonSerializer.Deserialize<List<CartItemDto>>(TempData["CartItems"].ToString());
                TempData.Keep("CartItems");
            }

            // SubTotal is a calculated property
            TempData.Keep("CartSubTotal");

            if (TempData["CartTax"] != null)
            {
                model.Cart.Tax = Convert.ToDecimal(TempData["CartTax"]);
                TempData.Keep("CartTax");
            }

            if (TempData["CartShipping"] != null)
            {
                model.Cart.Shipping = Convert.ToDecimal(TempData["CartShipping"]);
                TempData.Keep("CartShipping");
            }

            if (TempData["CartDiscount"] != null)
            {
                model.Cart.Discount = Convert.ToDecimal(TempData["CartDiscount"]);
                TempData.Keep("CartDiscount");
            }

            // Keep TempData for the next request
            TempData.Keep();
            
            // Restore new fields for display
            model.ShippingMethod = TempData["ShippingMethod"]?.ToString();
            model.OrderNotes = TempData["OrderNotes"]?.ToString();
            model.PromoCode = TempData["PromoCode"]?.ToString();
            if (TempData["CartDiscount"] != null)
            {
                model.PromoDiscount = Convert.ToDecimal(TempData["CartDiscount"]);
            }

            return View(model);
        }

        // Step 5: Process Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Create order using the service
                var shippingAddress = TempData["ShippingAddress"]?.ToString() ?? string.Empty;
                var paymentMethod = TempData["PaymentMethod"]?.ToString() ?? "CreditCard";
                var shippingMethod = TempData["ShippingMethod"]?.ToString() ?? "Standard";
                var orderNotes = TempData["OrderNotes"]?.ToString() ?? "";
                var promoCode = TempData["PromoCode"]?.ToString();
                
                // Calculate shipping cost based on method (secure server-side calculation)
                decimal shippingCost = shippingMethod.ToLower() == "express" ? 150 : (shippingMethod.ToLower() == "free" ? 0 : 50);
                
                // Get discount amount
                decimal discountAmount = 0;
                if (TempData["CartDiscount"] != null)
                {
                    decimal.TryParse(TempData["CartDiscount"].ToString(), out discountAmount);
                }
                
                var order = await _orderService.CreateOrderAsync(userId, shippingAddress, paymentMethod, shippingMethod, shippingCost, orderNotes, discountAmount);
                
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Failed to create order. Please try again.";
                    return RedirectToAction("OrderConfirmation");
                }
                
                // Log order creation activity
                await _activityLogService.LogActivityAsync(
                    userId,
                    "OrderCreated",
                    "Order",
                    order.OrderID,
                    $"Order created - Total: {order.TotalAmount:C} L.E"
                );
                
                // Send order confirmation email
                try
                {
                    var userEmail = TempData["Email"]?.ToString() ?? User.FindFirstValue(ClaimTypes.Email);
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        await _emailSenderService.SendOrderConfirmationEmailAsync(order.OrderID, userEmail);
                        _logger.LogInformation($"Order confirmation email sent to {userEmail} for order #{order.OrderID}");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Failed to send order confirmation email for order #{order.OrderID}");
                    // Don't fail the order if email fails
                }
                
                // Clear the cart
                await _cartService.ClearCartAsync(userId);
                
                // Clear TempData
                TempData.Clear();

                return RedirectToAction("OrderComplete", new { id = order.OrderID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                TempData["Error"] = "Failed to process your order. Please try again.";
                return RedirectToAction("OrderConfirmation");
            }
        }

        // Step 6: Order Complete
        public async Task<IActionResult> OrderComplete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var orderDetails = await _orderService.GetOrderDetailsAsync(id);

                if (orderDetails == null)
                {
                    return NotFound();
                }

                // Return the DTO directly - the view uses OrderDetailsViewModel which matches the structure
                return View(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details");
                TempData["Error"] = "Failed to load order details. Please check your order history.";
                return RedirectToAction("Index", "Home");
            }
        }

        // AJAX endpoint for applying promo code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromoCode([FromBody] PromoCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Code))
                {
                    return Json(new { success = false, message = "Please enter a promo code." });
                }

                var isValid = await _promoCodeService.ValidatePromoCodeAsync(request.Code);
                
                if (!isValid)
                {
                    return Json(new { success = false, message = "Invalid or expired promo code." });
                }

                var discount = await _promoCodeService.ApplyPromoCodeAsync(request.Code, request.SubTotal);
                var promoDetails = await _promoCodeService.GetPromoCodeByCodeAsync(request.Code);

                return Json(new { 
                    success = true, 
                    discount = discount,
                    discountFormatted = discount.ToString("C"),
                    message = promoDetails?.Description ?? "Promo code applied successfully!",
                    code = request.Code.ToUpper()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promo code");
                return Json(new { success = false, message = "Failed to apply promo code. Please try again." });
            }
        }

        // AJAX endpoint for removing promo code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePromoCode()
        {
            try
            {
                TempData.Remove("PromoCode");
                TempData.Remove("PromoDiscount");
                
                return Json(new { success = true, message = "Promo code removed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing promo code");
                return Json(new { success = false, message = "Failed to remove promo code." });
            }
        }

        // AJAX endpoint for getting shipping methods
        [HttpGet]
        public async Task<IActionResult> GetShippingMethods()
        {
            try
            {
                var providers = await _shippingService.GetAvailableProvidersAsync();
                var shippingMethods = providers.Select(p => new
                {
                    id = p.ProviderId,
                    name = p.Name,
                    description = p.Description,
                    cost = p.BaseCost,
                    costFormatted = p.BaseCost.ToString("C"),
                    estimatedDays = p.EstimatedDeliveryDays,
                    estimatedDate = p.EstimatedDeliveryDays.HasValue 
                        ? DateTime.UtcNow.AddDays(p.EstimatedDeliveryDays.Value).ToString("MMMM dd, yyyy")
                        : "Varies"
                }).ToList();

                return Json(new { success = true, methods = shippingMethods });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping methods");
                return Json(new { 
                    success = false, 
                    message = "Failed to load shipping methods.",
                    methods = new[] {
                        new { id = 1, name = "Standard Shipping", cost = 50m, costFormatted = "$50.00", estimatedDays = 5, estimatedDate = DateTime.UtcNow.AddDays(5).ToString("MMMM dd, yyyy") },
                        new { id = 2, name = "Express Shipping", cost = 150m, costFormatted = "$150.00", estimatedDays = 2, estimatedDate = DateTime.UtcNow.AddDays(2).ToString("MMMM dd, yyyy") }
                    }
                });
            }
        }

        // AJAX endpoint for updating shipping method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingMethod([FromBody] ShippingMethodRequest request)
        {
            try
            {
                var providers = await _shippingService.GetAvailableProvidersAsync();
                var selectedProvider = providers.FirstOrDefault(p => p.ProviderId == request.MethodId);

                if (selectedProvider == null)
                {
                    return Json(new { success = false, message = "Invalid shipping method." });
                }

                var estimatedDate = selectedProvider.EstimatedDeliveryDays.HasValue
                    ? DateTime.UtcNow.AddDays(selectedProvider.EstimatedDeliveryDays.Value)
                    : (DateTime?)null;

                return Json(new
                {
                    success = true,
                    shippingCost = selectedProvider.BaseCost,
                    shippingCostFormatted = selectedProvider.BaseCost.ToString("C"),
                    estimatedDate = estimatedDate?.ToString("MMMM dd, yyyy") ?? "Varies",
                    methodName = selectedProvider.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping method");
                return Json(new { success = false, message = "Failed to update shipping method." });
            }
        }
    }

    // Request models for AJAX endpoints
    public class PromoCodeRequest
    {
        public string Code { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class ShippingMethodRequest
    {
        public int MethodId { get; set; }
    }
}