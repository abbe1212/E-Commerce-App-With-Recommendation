using FluentValidation;
using Ecommerce.Application.DTOs.Products;

namespace Ecommerce.Application.Validators
{
    public class CreateProductDtoValidator : AbstractValidator<ProductDto>
    {
        public CreateProductDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Product name is required and must be under 200 characters.");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
            RuleFor(x => x.CategoryID).GreaterThan(0).WithMessage("A valid category is required.");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
        }
    }
}
