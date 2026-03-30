using FluentValidation;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBulkBusinessItem;

/// <summary>
/// Validator for CreateBulkBusinessItemCommand to ensure uploaded file meets requirements
/// </summary>
public class CreateBulkBusinessItemCommandValidator : AbstractValidator<CreateBulkBusinessItemCommand>
{
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx",
        ".xls"
    };

    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    public CreateBulkBusinessItemCommandValidator()
    {
        RuleFor(x => x.file)
            .NotNull()
            .WithMessage("File is required");

        When(x => x.file != null, () =>
        {
            RuleFor(x => x.file.FileName)
                .NotEmpty()
                .WithMessage("File name is required")
                .Must(BeValidExcelFile)
                .WithMessage("Only Excel files (.xlsx, .xls) are allowed");

            RuleFor(x => x.file.Length)
                .GreaterThan(0)
                .WithMessage("File cannot be empty")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)}MB");
        });
    }

    private bool BeValidExcelFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return AllowedFileExtensions.Contains(extension);
    }
}
