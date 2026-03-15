using Ecommerce.Application.DTOs.Cart;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetOrCreateCartAsync(string id);
        Task AddItemToCartAsync(string userId, int productId, int quantity);
        Task RemoveItemFromCartAsync(string userId, int productId);
        Task UpdateItemQuantityAsync(string userId, int productId, int newQuantity);
        Task ClearCartAsync(string userId);
        Task<decimal> GetCartTotalAsync(string userId);
    }
}
