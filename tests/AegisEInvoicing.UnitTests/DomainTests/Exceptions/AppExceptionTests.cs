using AegisEInvoicing.Domain.Exceptions;
using FluentAssertions;
using System.Net;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Exceptions;

/// <summary>
/// Comprehensive tests for AppException targeting 100% code coverage
/// </summary>
public class AppExceptionTests
{
    // Create a concrete implementation for testing since AppException is abstract
    private class TestAppException : AppException
    {
        public override HttpStatusCode StatusCode { get; }

        public TestAppException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string? errorCode = null, object? details = null)
            : base(message, errorCode, details)
        {
            StatusCode = statusCode;
        }

        public TestAppException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string? errorCode = null, object? details = null)
            : base(message, innerException, errorCode, details)
        {
            StatusCode = statusCode;
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new TestAppException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.ErrorCode.Should().Be("TestApp");
        exception.Details.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var errorCode = "CUSTOM_ERROR";

        // Act
        var exception = new TestAppException(message, HttpStatusCode.BadRequest, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndDetails_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var details = new { Property = "Value", Number = 42 };

        // Act
        var exception = new TestAppException(message, HttpStatusCode.BadRequest, null, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("TestApp");
        exception.Details.Should().Be(details);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var errorCode = "CUSTOM_ERROR";
        var details = new { Property = "Value" };

        // Act
        var exception = new TestAppException(message, HttpStatusCode.NotFound, errorCode, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().Be(details);
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new TestAppException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.ErrorCode.Should().Be("TestApp");
        exception.Details.Should().BeNull();
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithInnerExceptionAndErrorCode_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");
        var errorCode = "CUSTOM_ERROR";

        // Act
        var exception = new TestAppException(message, innerException, HttpStatusCode.BadRequest, errorCode);

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
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");
        var details = new { Error = "Detailed error info" };

        // Act
        var exception = new TestAppException(message, innerException, HttpStatusCode.BadRequest, null, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.ErrorCode.Should().Be("TestApp");
        exception.Details.Should().Be(details);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithAllParametersAndInnerException_ShouldCreateException()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");
        var errorCode = "CUSTOM_ERROR";
        var details = new { Error = "Detailed error info" };

        // Act
        var exception = new TestAppException(message, innerException, HttpStatusCode.NotFound, errorCode, details);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().Be(details);
        exception.InnerException.Should().Be(innerException);
    }

    #endregion

    #region ErrorCode Generation Tests

    [Fact]
    public void ErrorCode_WhenNotProvided_ShouldGenerateFromTypeName()
    {
        // Act
        var exception = new TestAppException("Test message");

        // Assert
        exception.ErrorCode.Should().Be("TestApp");
    }

    [Fact]
    public void ErrorCode_WhenProvidedAsNull_ShouldGenerateFromTypeName()
    {
        // Act
        var exception = new TestAppException("Test message", HttpStatusCode.BadRequest, null);

        // Assert
        exception.ErrorCode.Should().Be("TestApp");
    }

    [Fact]
    public void ErrorCode_WhenProvidedAsEmpty_ShouldUseEmptyString()
    {
        // Act
        var exception = new TestAppException("Test message", HttpStatusCode.BadRequest, "");

        // Assert
        exception.ErrorCode.Should().Be("");
    }

    [Fact]
    public void ErrorCode_WhenProvided_ShouldUseProvidedValue()
    {
        // Arrange
        var customErrorCode = "CUSTOM_ERROR_CODE";

        // Act
        var exception = new TestAppException("Test message", HttpStatusCode.BadRequest, customErrorCode);

        // Assert
        exception.ErrorCode.Should().Be(customErrorCode);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void AppException_ShouldInheritFromException()
    {
        // Arrange
        var exception = new TestAppException("Test message");

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
        exception.Should().BeOfType<TestAppException>();
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void StatusCode_ShouldReturnCorrectValue(HttpStatusCode expectedStatusCode)
    {
        // Act
        var exception = new TestAppException("Test message", expectedStatusCode);

        // Assert
        exception.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void Details_WhenNull_ShouldReturnNull()
    {
        // Act
        var exception = new TestAppException("Test message");

        // Assert
        exception.Details.Should().BeNull();
    }

    [Fact]
    public void Details_WhenProvided_ShouldReturnProvidedValue()
    {
        // Arrange
        var details = new { Error = "Test error", Code = 123 };

        // Act
        var exception = new TestAppException("Test message", HttpStatusCode.BadRequest, null, details);

        // Assert
        exception.Details.Should().Be(details);
        exception.Details.Should().BeOfType(details.GetType());
    }

    [Theory]
    [InlineData("Simple string")]
    [InlineData(42)]
    [InlineData(true)]
    public void Details_WithDifferentTypes_ShouldReturnCorrectValue(object details)
    {
        // Act
        var exception = new TestAppException("Test message", HttpStatusCode.BadRequest, null, details);

        // Assert
        exception.Details.Should().Be(details);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldCreateException()
    {
        // Act
        var exception = new TestAppException("");

        // Assert
        exception.Message.Should().Be("");
        exception.ErrorCode.Should().Be("TestApp");
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldCreateException()
    {
        // Act
        var exception = new TestAppException("Test message", (Exception)null!);

        // Assert
        exception.Message.Should().Be("Test message");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void ErrorCode_WithComplexTypeName_ShouldRemoveExceptionSuffix()
    {
        // Create a more complex exception name to test the replacement
        var exception = new TestAppException("Test");

        // Assert
        exception.ErrorCode.Should().Be("TestApp"); // TestAppException -> TestApp
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AppException_ShouldMaintainStackTrace()
    {
        // Arrange & Act
        TestAppException? caughtException = null;

        try
        {
            ThrowTestException();
        }
        catch (TestAppException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.StackTrace.Should().NotBeNull();
        caughtException.StackTrace.Should().Contain(nameof(ThrowTestException));
    }

    private static void ThrowTestException()
    {
        throw new TestAppException("Test exception from method");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldIncludeMessageAndType()
    {
        // Arrange
        var exception = new TestAppException("Test exception message");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("TestAppException");
        result.Should().Contain("Test exception message");
    }

    [Fact]
    public void ToString_WithInnerException_ShouldIncludeInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner message");
        var exception = new TestAppException("Outer message", innerException);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Outer message");
        result.Should().Contain("Inner message");
        result.Should().Contain("InvalidOperationException");
    }

    #endregion
}