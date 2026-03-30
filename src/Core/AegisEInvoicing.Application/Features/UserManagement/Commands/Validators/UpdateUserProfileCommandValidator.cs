using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for UpdateUserProfileCommand
/// </summary>
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("Phone number must not exceed 50 characters")
            .Must(BeValidPhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }

    private static bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, spaces, hyphens, and apostrophes
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }

    private static bool BeValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return true; // Optional field

        // Remove common formatting characters
        var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Must be between 7 and 15 digits (international standard)
        return cleanNumber.All(char.IsDigit) && cleanNumber.Length >= 7 && cleanNumber.Length <= 15;
    }
}