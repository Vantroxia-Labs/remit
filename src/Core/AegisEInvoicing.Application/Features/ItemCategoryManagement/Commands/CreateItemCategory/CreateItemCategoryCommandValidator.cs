using FluentValidation;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.CreateItemCategory;

/// <summary>
/// Validator for CreateItemCategoryCommand
/// Ensures ItemCategory name is at least 2 characters long
/// </summary>
public class CreateItemCategoryCommandValidator : AbstractValidator<CreateItemCategoryCommand>
{
    public CreateItemCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item category name is required")
            .MinimumLength(2)
            .WithMessage("Item category name must be at least 2 characters long")
            .MaximumLength(200)
            .WithMessage("Item category name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Item category description cannot exceed 500 characters");
    }
}
