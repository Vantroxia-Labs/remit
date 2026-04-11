using FluentValidation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;

public class CreateBusinessItemCommandValidator : AbstractValidator<CreateBusinessItemCommand>
{
    public CreateBusinessItemCommandValidator()
    {
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


        RuleFor(x => x.ItemCategoryId)
            .NotEmpty()
            .WithMessage("Item category ID is required");

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
    }
}