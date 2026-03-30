using FluentValidation;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.UpdateItemCategory;

/// <summary>
/// Validator for UpdateItemCategoryCommand
/// Ensures ItemCategory name is at least 2 characters long
/// </summary>
public class UpdateItemCategoryCommandValidator : AbstractValidator<UpdateItemCategoryCommand>
{
    public UpdateItemCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Item category ID is required");

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
