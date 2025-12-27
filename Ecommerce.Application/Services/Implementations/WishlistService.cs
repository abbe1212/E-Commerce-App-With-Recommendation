using AutoMapper;
using Ecommerce.Application.DTOs.Wishlist;
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
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IMapper _mapper;

        public WishlistService(IUnitOfWork unitOfWork, IWishlistRepository wishlistRepository, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _wishlistRepository = wishlistRepository;
            _mapper = mapper;
        }

        // Done
        public async Task AddToWishlistAsync(string userId, int productId)
        {
            var wishlist = await _wishlistRepository.GetWishlistByUserIdAsync(userId);
            if (!wishlist.Items.Any(i => i.ProductID == productId))
            {
                wishlist.Items.Add(new WishlistItem { ProductID = productId, AddedAt = DateTime.UtcNow });
                if (wishlist.WishlistID == 0)
                    await _wishlistRepository.AddAsync(wishlist);
                else
                    await _wishlistRepository.UpdateAsync(wishlist);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Done
        public async Task<WishlistDto> GetWishlistAsync(string userId)
        {
            var wishlist = await _wishlistRepository.GetWishlistByUserIdAsync (userId);
            if (wishlist == null)
            {
                wishlist = new Wishlist { UserID = userId };
                await _wishlistRepository.AddAsync(wishlist);
                await _unitOfWork.SaveChangesAsync();
            }
            return _mapper.Map<WishlistDto>(wishlist);
        }

        // Done
        public async Task<bool> IsProductInWishlistAsync(string userId, int productId)
        {
            return await _wishlistRepository.IsProductInWishlistAsync (userId, productId);
        }

        // Done
        public async Task RemoveFromWishlistAsync(string userId, int wishlistItemId)
        {
            var wishlist = await _wishlistRepository.GetWishlistByUserIdAsync(userId);
            var itemToRemove = wishlist?.Items.FirstOrDefault(i=>i.WishlistItemID == wishlistItemId);
            if (itemToRemove != null)
            {
                wishlist.Items.Remove(itemToRemove);
                await _wishlistRepository.UpdateAsync(wishlist);
                await _unitOfWork.SaveChangesAsync();

            }
        }
    }
}
