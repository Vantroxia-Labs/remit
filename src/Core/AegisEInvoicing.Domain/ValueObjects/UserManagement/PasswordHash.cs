using AegisEInvoicing.Domain.Common.Implementation;
using System.Security.Cryptography;
using System.Text;

namespace AegisEInvoicing.Domain.ValueObjects.UserManagement;

/// <summary>
/// Represents a securely hashed password using BCrypt
/// </summary>
public class PasswordHash : ValueObject
{
    public string Hash { get; }
    public string Salt { get; }
    public DateTimeOffset CreatedAt { get; }

    // Parameterless constructor for Entity Framework
    private PasswordHash()
    {
        Hash = string.Empty;
        Salt = string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private PasswordHash(string hash, string salt, DateTimeOffset? createdAt = null)
    {
        Hash = hash;
        Salt = salt;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public static PasswordHash Create(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be null or empty", nameof(plainTextPassword));

        ValidatePasswordStrength(plainTextPassword);

        var salt = GenerateSalt();
        var hash = HashPassword(plainTextPassword, salt);

        return new PasswordHash(hash, salt);
    }

    public static PasswordHash FromExisting(string hash, string salt, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        return new PasswordHash(hash, salt, createdAt);
    }

    public bool Verify(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            return false;

        try
        {
            var hashedInput = HashPassword(plainTextPassword, Salt);
            return Hash == hashedInput;
        }
        catch
        {
            return false;
        }
    }

    public bool IsExpired(TimeSpan maxAge)
    {
        return DateTimeOffset.UtcNow - CreatedAt > maxAge;
    }

    public static void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

        if (password.Length > 128)
            throw new ArgumentException("Password cannot exceed 128 characters", nameof(password));

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch));

        if (!hasUpper)
            throw new ArgumentException("Password must contain at least one uppercase letter", nameof(password));

        if (!hasLower)
            throw new ArgumentException("Password must contain at least one lowercase letter", nameof(password));

        if (!hasDigit)
            throw new ArgumentException("Password must contain at least one digit", nameof(password));

        if (!hasSpecial)
            throw new ArgumentException("Password must contain at least one special character", nameof(password));

        // Check for common weak passwords
        var commonPasswords = new[]
        {
            "password", "123456", "password123", "admin", "qwerty", "letmein",
            "welcome", "monkey", "1234567890", "Password1", "Password123"
        };

        if (commonPasswords.Any(weak => string.Equals(password, weak, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Password is too common and not secure", nameof(password));
    }

    private static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        var combined = password + salt;
        var iterations = 100000;

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            combined,
            Encoding.UTF8.GetBytes(salt),
            iterations,
            HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToBase64String(hashBytes);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hash;
        yield return Salt;
    }

    public override string ToString()
    {
        return $"Hash: {Hash[..8]}... (Created: {CreatedAt:yyyy-MM-dd HH:mm})";
    }
}