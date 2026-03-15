using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.DTOs.Cart;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public CartService(IUnitOfWork unitOfWork, ICartRepository cartRepository, IProductRepository productRepository, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        // Done
        public async Task AddItemToCartAsync(string userId, int productId, int quantity)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId) ?? new Cart 
                {
                   UserID = userId,
                   CreatedAt  = DateTime.UtcNow,
                   Items = new List<CartItem>()
                };
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductID == productId);
            var product = await _productRepository.GetByIdAsync(productId);
            
            if(product == null || !product.IsAvailable || product.StockQuantity < quantity)
                throw new ArgumentException("Product is not available or has insufficient stock.", nameof(productId));
            
            if (existingItem != null)
                existingItem.Quantity += quantity;

            else
                cart.Items.Add(new CartItem { ProductID = productId, Quantity = quantity });

            if (cart.CartID == 0) 
                await _cartRepository.AddAsync(cart);
            else
                await _cartRepository.UpdateAsync(cart);

            await _unitOfWork.SaveChangesAsync();
        }

        // Done
        public async Task ClearCartAsync(string userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart != null && cart.Items.Any())
            {
                cart.Items.Clear();
                await _cartRepository.UpdateAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Done
        public async Task<decimal> GetCartTotalAsync(string userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                return 0;

            decimal total = 0;
            foreach (var item in cart.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductID);
                if (product != null)
                {
                    total += product.Price * item.Quantity;
                }
            }
            return total;
        }

        // Done
        public async Task<CartDto> GetOrCreateCartAsync(string id)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(id);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserID = id,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<CartItem>()
                };
                await _cartRepository.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }

            return new CartDto
            {
                CartID = cart.CartID,
                UserID = cart.UserID,
                Items = cart.Items.Select(item => new CartItemDto
                {
                    CartItemID = item.CartItemID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    ProductName = item.Product?.Name ?? string.Empty,
                    ImageURL = item.Product?.ImageURL,
                    UnitPrice = item.Product?.Price ?? 0m,
                    IsAvailable = item.Product?.IsAvailable ?? false,
                    StockQuantity = item.Product?.StockQuantity ?? 0
                }).ToList()
            };
        }

        // Done
        public async Task RemoveItemFromCartAsync(string userId, int productId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart?.Items != null)
            {
                var itemToRemove = cart.Items.FirstOrDefault(i => i.ProductID == productId);
                if (itemToRemove != null)
                {
                    cart.Items.Remove(itemToRemove);
                    await _cartRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        // Done
        public async Task UpdateItemQuantityAsync(string userId, int productId, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                await RemoveItemFromCartAsync(userId, productId);
                return;
            }
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart != null)
            {
                var itemToUpdate = cart.Items.FirstOrDefault(ci => ci.ProductID == productId);
                if (itemToUpdate != null)
                {
                    itemToUpdate.Quantity = newQuantity;
                    await _cartRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
}
