using AutoMapper;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Enums;
using Ecommerce.Core.Interfaces;
using Ecommerce.Infrastructure.Repositories;

namespace Ecommerce.Application.Services.Implementations
{
    public class OrderService : IOrderService
    {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IOrderRepository _orderRepository;
            private readonly ICartRepository _cartRepository;
            private readonly IProductRepository _productRepository; 
            private readonly IMapper _mapper;

            public OrderService(
                IUnitOfWork unitOfWork,
                IOrderRepository orderRepository,
                ICartRepository cartRepository,
                IProductRepository productRepository, 
                IMapper mapper)
            {
                _unitOfWork = unitOfWork;
                _orderRepository = orderRepository;
                _cartRepository = cartRepository;
                _productRepository = productRepository; 
                _mapper = mapper;
            }

            // Done
            public async Task CancelOrderAsync(int orderId)
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order != null && order.Status != OrderStatus.cancelled && order.Status != OrderStatus.delivered)
                {
                await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        order.Status = OrderStatus.cancelled;

                        foreach (var item in order.OrderItems)
                        {
                            var productToUpdate = await _productRepository.GetByIdAsync(item.ProductID);
                            if (productToUpdate != null)
                            {
                                productToUpdate.StockQuantity += item.Quantity;
                                await _productRepository.UpdateAsync(productToUpdate);
                            }
                        }

                        await _orderRepository.UpdateAsync(order);
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }
                }
            }

        public async Task<OrderDto> CreateOrderAsync(string userId, string shippingAddress, string paymentMethod, string shippingMethod = "Standard", decimal shippingCost = 0, string orderNotes = "", decimal discountAmount = 0)
        {

            // Get user's cart
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                decimal subTotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cart.Items)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductID);
                    if (product == null || product.StockQuantity < cartItem.Quantity)
                    {
                        throw new InvalidOperationException($"Product '{product?.Name ?? "ID: " + cartItem.ProductID}' is out of stock or insufficient quantity.");
                    }

                    subTotal += product.Price * cartItem.Quantity;
                    orderItems.Add(new OrderItem
                    {
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price
                    });
                }
                
                // Calculate tax (assuming 8% as per view)
                decimal taxAmount = subTotal * 0.08m;
                
                // Calculate final total
                decimal totalAmount = subTotal + taxAmount + shippingCost - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

                // Calculate estimated delivery date based on shipping method
                DateTime estimatedDeliveryDate = DateTime.UtcNow;
                switch (shippingMethod.ToLower())
                {
                    case "express":
                        estimatedDeliveryDate = estimatedDeliveryDate.AddDays(2);
                        break;
                    case "free":
                        estimatedDeliveryDate = estimatedDeliveryDate.AddDays(7);
                        break;
                    case "standard":
                    default:
                        estimatedDeliveryDate = estimatedDeliveryDate.AddDays(5);
                        break;
                }

                // Create order
                var order = new Order
                {
                    UserID = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.pending,
                    ShippingAddress = shippingAddress,
                    ShippingMethod = shippingMethod,
                    EstimatedDeliveryDate = estimatedDeliveryDate,
                    OrderNotes = orderNotes,
                    TotalAmount = totalAmount,
                    OrderItems = orderItems
                };

                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync(); // ضروري للحصول على OrderID

                // تحديث المخزون بعد إنشاء الطلب بنجاح
                foreach (var item in order.OrderItems)
                {
                    var productToUpdate = await _productRepository.GetByIdAsync(item.ProductID);
                    if (productToUpdate != null)
                    {
                        productToUpdate.StockQuantity -= item.Quantity;
                        await _productRepository.UpdateAsync(productToUpdate);
                    }
                }

                // مسح عربة التسوق
                cart.Items.Clear();
                await _cartRepository.UpdateAsync(cart);

                // إتمام المعاملة بنجاح
                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<OrderDto>(order);
            }
            catch
            {
                // التراجع عن كل التغييرات في حالة حدوث أي خطأ
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }


        // Done
        public async Task<OrderDetailsDto> GetOrderDetailsAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            return _mapper.Map<OrderDetailsDto>(order);
        }


        // Done
        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        // Get all orders for admin
        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.ListAllAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        // Done
        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                await _orderRepository.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
