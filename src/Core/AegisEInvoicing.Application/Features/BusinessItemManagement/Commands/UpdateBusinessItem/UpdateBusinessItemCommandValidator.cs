using FluentValidation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.UpdateBusinessItem;

public class UpdateBusinessItemCommandValidator : AbstractValidator<UpdateBusinessItemCommand>
{
    public UpdateBusinessItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Business item ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item name is required")
            .MaximumLength(200)
            .WithMessage("Item name must not exceed 200 characters");

        RuleFor(x => x.ItemDescription)
            .NotEmpty()
            .WithMessage("Item description is required")
            .MaximumLength(1000)
            .WithMessage("Item description must not exceed 1000 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price must be greater than or equal to 0")
            .LessThan(1000000000)
            .WithMessage("Unit price must be less than 1,000,000,000");

        RuleFor(x => x.ItemType)
            .IsInEnum()
            .WithMessage("Item type must be either Goods or Service");

        RuleFor(x => x.ServiceCode)
            .NotNull()
            .WithMessage("Code is required");

        When(x => x.ServiceCode != null, () =>
        {
            RuleFor(x => x.ServiceCode.Code)
                .NotEmpty()
                .WithMessage("Code is required")
                .MaximumLength(50)
                .WithMessage("Code must not exceed 50 characters");

            RuleFor(x => x.ServiceCode.Name)
                .NotEmpty()
                .WithMessage("Code description is required")
                .MaximumLength(200)
                .WithMessage("Code description must not exceed 200 characters");
        });

        RuleForEach(x => x.TaxCategories).ChildRules(tc =>
        {
            tc.RuleFor(x => x.Code).NotEmpty().WithMessage("Tax category code is required");
            tc.RuleFor(x => x.Name).NotEmpty().WithMessage("Tax category name is required");
            tc.When(x => x.IsPercentage, () =>
            {
                tc.RuleFor(x => x.Percent)
                    .NotNull().WithMessage("Percent is required for percentage tax")
                    .InclusiveBetween(0, 100).WithMessage("Percent must be between 0 and 100");
            });
            tc.When(x => !x.IsPercentage, () =>
            {
                tc.RuleFor(x => x.FlatAmount)
                    .NotNull().WithMessage("Flat amount is required for flat fee tax")
                    .GreaterThanOrEqualTo(0).WithMessage("Flat amount must be non-negative");
            });
        });
    }
}