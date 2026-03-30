using AegisEInvoicing.Domain.Exceptions;
using FluentAssertions;
using System.Net;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Exceptions;

/// <summary>
/// Comprehensive tests for BadRequestException targeting 100% code coverage
/// </summary>
public class BadRequestExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";

        // Act
        var exception = new BadRequestException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("BadRequest");
        exception.Details.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var errorCode = "INVALID_INPUT";

        // Act
        var exception = new BadRequestException(message, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndDetails_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var details = new { Field = "email", Error = "Invalid format" };

        // Act
        var exception = new BadRequestException(message, null, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("BadRequest");
        exception.Details.Should().Be(details);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var errorCode = "VALIDATION_FAILED";
        var details = new { Field = "email", Error = "Invalid format" };

        // Act
        var exception = new BadRequestException(message, errorCode, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().Be(details);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new BadRequestException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("BadRequest");
        exception.Details.Should().BeNull();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithInnerExceptionAndErrorCode_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var innerException = new ArgumentException("Invalid argument");
        var errorCode = "INVALID_PARAMETER";

        // Act
        var exception = new BadRequestException(message, innerException, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().BeNull();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithInnerExceptionAndDetails_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var innerException = new ArgumentException("Invalid argument");
        var details = new { ValidationErrors = new[] { "Email is required", "Password too short" } };

        // Act
        var exception = new BadRequestException(message, innerException, null, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("BadRequest");
        exception.Details.Should().Be(details);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithAllParametersAndInnerException_ShouldCreateException()
    {
        // Arrange
        var message = "Bad request message";
        var innerException = new ArgumentException("Invalid argument");
        var errorCode = "VALIDATION_ERROR";
        var details = new { ValidationErrors = new[] { "Email is required" } };

        // Act
        var exception = new BadRequestException(message, innerException, errorCode, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().Be(details);
        exception.InnerException.Should().Be(innerException);
    }

    #endregion

    #region StatusCode Tests

    [Fact]
    public void StatusCode_ShouldAlwaysReturnBadRequest()
    {
        // Arrange
        var exception1 = new BadRequestException("Message 1");
        var exception2 = new BadRequestException("Message 2", "ERROR_CODE");
        var exception3 = new BadRequestException("Message 3", new Exception(), "ERROR_CODE", new { });

        // Act & Assert
        exception1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void BadRequestException_ShouldInheritFromAppException()
    {
        // Arrange
        var exception = new BadRequestException("Test message");

        // Act & Assert
        exception.Should().BeAssignableTo<AppException>();
        exception.Should().BeAssignableTo<Exception>();
        exception.Should().BeOfType<BadRequestException>();
    }

    [Fact]
    public void BadRequestException_ShouldBeSealed()
    {
        // Act & Assert
        typeof(BadRequestException).IsSealed.Should().BeTrue();
    }

    #endregion

    #region Exception Handling Tests
    
    [Fact]
    public void BadRequestException_ShouldMaintainStackTrace()
    {
        // Arrange & Act
        BadRequestException? caughtException = null;

        try
        {
            ThrowBadRequestException();
        }
        catch (BadRequestException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.StackTrace.Should().NotBeNull();
        caughtException.StackTrace.Should().Contain(nameof(ThrowBadRequestException));
    }

    private static void ThrowBadRequestException()
    {
        throw new BadRequestException("Test exception from method");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldCreateException()
    {
        // Act
        var exception = new BadRequestException("");

        // Assert
        exception.Message.Should().Be("");
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("BadRequest");
    }

    [Fact]
    public void Constructor_WithNullErrorCode_ShouldUseDefaultErrorCode()
    {
        // Act
        var exception = new BadRequestException("Test message", (string?)null);

        // Assert
        exception.ErrorCode.Should().Be("BadRequest");
    }

    [Fact]
    public void Constructor_WithEmptyErrorCode_ShouldUseEmptyErrorCode()
    {
        // Act
        var exception = new BadRequestException("Test message", "");

        // Assert
        exception.ErrorCode.Should().Be("");
    }   

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldIncludeMessageAndType()
    {
        // Arrange
        var exception = new BadRequestException("Bad request test message");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("BadRequestException");
        result.Should().Contain("Bad request test message");
    }

    [Fact]
    public void ToString_WithInnerException_ShouldIncludeInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception message");
        var exception = new BadRequestException("Outer message", innerException);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Outer message");
        result.Should().Contain("Inner exception message");
        result.Should().Contain("ArgumentException");
    }

    #endregion

    #region Property Access Tests

    [Fact]
    public void Properties_ShouldBeAccessibleViaBaseClass()
    {
        // Arrange
        var message = "Test message";
        var errorCode = "TEST_ERROR";
        var details = new { Property = "Value" };
        var exception = new BadRequestException(message, errorCode, details);

        // Act
        AppException baseException = exception;

        // Assert
        baseException.Message.Should().Be(message);
        baseException.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        baseException.ErrorCode.Should().Be(errorCode);
        baseException.Details.Should().Be(details);
    }

    #endregion

    #region Common Use Cases

    [Fact]
    public void BadRequestException_ValidationErrors_CommonUseCase()
    {
        // Arrange
        var validationErrors = new
        {
            Email = "Email is required",
            Password = "Password must be at least 8 characters"
        };

        // Act
        var exception = new BadRequestException(
            "Validation failed",
            "VALIDATION_ERROR",
            validationErrors);

        // Assert
        exception.Message.Should().Be("Validation failed");
        exception.ErrorCode.Should().Be("VALIDATION_ERROR");
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Details.Should().Be(validationErrors);
    }

    [Fact]
    public void BadRequestException_InvalidFormat_CommonUseCase()
    {
        // Act
        var exception = new BadRequestException(
            "Invalid date format",
            "INVALID_FORMAT");

        // Assert
        exception.Message.Should().Be("Invalid date format");
        exception.ErrorCode.Should().Be("INVALID_FORMAT");
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}