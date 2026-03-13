using FluentValidation;
using Ecommerce.Application.ViewModels;

namespace Ecommerce.Application.Validators
{
    public class CheckoutViewModelValidator : AbstractValidator<CheckoutViewModel>
    {
        public CheckoutViewModelValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Full name is required.");
            RuleFor(x => x.ShippingAddress).NotEmpty().WithMessage("Shipping address is required.");
            RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone number is required.");
            RuleFor(x => x.ShippingMethod)
                .Must(m => new[] { "standard", "express", "free" }.Contains(m?.ToLower()))
                .WithMessage("Shipping method must be Standard, Express, or Free.");
        }
    }
}
