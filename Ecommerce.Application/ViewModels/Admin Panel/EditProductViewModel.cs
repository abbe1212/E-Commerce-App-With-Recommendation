using Ecommerce.Application.DTOs.Products;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.ViewModels.Admin_Panel
{
    public class EditProductViewModel
    {
        public int ProductID { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty; 

        public string? Description { get; set; } 

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }
        public string? ImageURL { get; set; } 

        [Display(Name = "Category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        public int CategoryID { get; set; }

        [Display(Name = "Brand")]
        public int? BrandID { get; set; }

        [Range(0, 100)]
        [Display(Name = "Discount Percentage")]
        public decimal DiscountPercentage { get; set; }

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Brands { get; set; } = new();
        public List<SelectListItem> Tags { get; set; } = new();

        public List<int> SelectedTagIds { get; set; } = new();
        
        // Product Images (Gallery)
        public List<ProductImageDto> Images { get; set; } = new();
        public List<string> NewImageUrls { get; set; } = new();
        
        // Product Variants (Size, Color)
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<ProductVariantInputModel> NewVariants { get; set; } = new();
    }
    
    // Input model for creating/editing variants
    public class ProductVariantInputModel
    {
        public int? VariantID { get; set; }
        
        [Required]
        public string VariantType { get; set; } = string.Empty;
        
        [Required]
        public string VariantValue { get; set; } = string.Empty;
        
        public string? SKU { get; set; }
        public decimal PriceAdjustment { get; set; } = 0;
        public int StockQuantity { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
        public string? ColorCode { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}