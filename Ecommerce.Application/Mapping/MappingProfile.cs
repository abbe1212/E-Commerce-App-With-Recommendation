using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Cart;
using Ecommerce.Application.DTOs.Inventory;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.Payment;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.DTOs.Promotion;
using Ecommerce.Application.DTOs.Review;
using Ecommerce.Application.DTOs.Shipping;
using Ecommerce.Application.DTOs.User;
using Ecommerce.Application.DTOs.Wishlist;
using Ecommerce.Application.ViewModels; // For RegisterViewModel
using Ecommerce.Application.ViewModels.Forms___Input_Models;
using Ecommerce.Application.ViewModels.Admin_Panel;
using Ecommerce.Application.ViewModels.User___Account;
using Ecommerce.Core.Entities;
using System.Linq;

namespace Ecommerce.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Product & Catalog Mappings ---
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.Reviews != null && src.Reviews.Any() ? src.Reviews.Average(r => r.Rating) : 0))
                .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.Reviews != null ? src.Reviews.Count : 0))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.ProductTags != null ? src.ProductTags.Select(pt => pt.Tag.Name) : new List<string>()))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ProductImages != null ? src.ProductImages.OrderBy(i => i.DisplayOrder) : null))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants != null ? src.ProductVariants.OrderBy(v => v.DisplayOrder) : null));

            // ProductImage mapping
            CreateMap<ProductImage, ProductImageDto>();
            
            // ProductVariant mapping
            CreateMap<ProductVariant, ProductVariantDto>();

            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore()); // Handled separately in controller
            
            CreateMap<CategoryDto, Category>()
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore())
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());
            CreateMap<Brand, BrandDto>();
            CreateMap<BrandDto, Brand>()
                .ForMember(dest => dest.Products, opt => opt.Ignore());
            CreateMap<Tag, TagDto>();

            // --- Admin Panel ViewModel Mappings ---
            CreateMap<EditProductViewModel, Product>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Brand, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTags, opt => opt.Ignore())
                .ForMember(dest => dest.CartItems, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryLogs, opt => opt.Ignore())
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
                .ForMember(dest => dest.ProductVariants, opt => opt.Ignore());

            CreateMap<Product, EditProductViewModel>()
                .ForMember(dest => dest.Categories, opt => opt.Ignore())
                .ForMember(dest => dest.Brands, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore())
                .ForMember(dest => dest.SelectedTagIds, opt => opt.MapFrom(src => src.ProductTags != null ? src.ProductTags.Select(pt => pt.TagID) : new List<int>()))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ProductImages))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants))
                .ForMember(dest => dest.NewImageUrls, opt => opt.Ignore())
                .ForMember(dest => dest.NewVariants, opt => opt.Ignore());

            // --- Cart Mappings ---
            CreateMap<Cart, CartDto>();
            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.ImageURL, opt => opt.MapFrom(src => src.Product != null ? src.Product.ImageURL : null))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.Product != null && src.Product.IsAvailable))
                .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.Product != null ? src.Product.StockQuantity : 0));

            // --- Order Mappings ---
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.OrderItems != null ? src.OrderItems.Count : 0));
            CreateMap<Order, OrderDetailsDto>();
            CreateMap<OrderItem, OrderItemDto>();

            // --- Payment & Shipping Mappings ---
            CreateMap<Payment, PaymentDto>();
            CreateMap<Shipping, ShippingDto>();

            // --- User & Auth Mappings ---
            CreateMap<ApplicationUser, UserDto>();            CreateMap<UpdateUserDto, ApplicationUser>().ReverseMap();
            CreateMap<UserDto, EditProfileViewModel>().ReverseMap();
            CreateMap<EditProfileViewModel, UpdateUserDto>();
            CreateMap<ApplicationUser, ProfileViewModel>();
            CreateMap<UserDto, ProfileViewModel>();

            // --- Review Mappings ---
            CreateMap<Review, ReviewDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => 
                    (src.User != null ? src.User.FirstName : "") + " " + (src.User != null ? src.User.LastName : "")));
            CreateMap<AddReviewDto, Review>();


            CreateMap<LoginViewModel, LoginDto>();
            CreateMap<RegisterViewModel, RegisterDto>();


            // --- Inventory Mappings ---
            CreateMap<InventoryLog, InventoryLogDto>();

            // --- Wishlist Mappings ---
            CreateMap<Wishlist, WishlistDto>();
            CreateMap<WishlistItem, WishlistItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ImageURL, opt => opt.MapFrom(src => src.Product != null ? src.Product.ImageURL : null))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.Product != null && src.Product.IsAvailable && src.Product.StockQuantity > 0));

            // --- Promotion Mappings ---
            // ➕ ADDED: Missing mapping for PromoCode
            CreateMap<PromoCode, PromoCodeDto>();

            // --- ViewModel/DTO to Entity Mappings (For Inputs) ---
            CreateMap<RegisterViewModel, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));
        }
    }
}