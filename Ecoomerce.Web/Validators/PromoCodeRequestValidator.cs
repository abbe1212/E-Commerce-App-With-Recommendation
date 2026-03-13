using FluentValidation;
using Ecoomerce.Web.Models;

namespace Ecoomerce.Web.Validators
{
    public class PromoCodeRequestValidator : AbstractValidator<PromoCodeRequest>
    {
        public PromoCodeRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Promo code is required.")
                .MaximumLength(50).WithMessage("Promo code must not exceed 50 characters.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Promo code must contain only alphanumeric characters.");

            RuleFor(x => x.SubTotal)
                .GreaterThan(0).WithMessage("SubTotal must be greater than 0.");
        }
    }
}
