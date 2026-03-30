using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands;
using AegisEInvoicing.Application.Features.BusinessManagement.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AegisEInvoicing.UnitTests.ApplicationTests.Features.BusinessManagement.Handlers;

/// <summary>
/// Tests for UpdateSubscriptionCommandHandler
/// Focuses on constructor validation and error handling paths
/// </summary>
public class UpdateSubscriptionCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateSubscriptionCommandHandler> _logger;

    public UpdateSubscriptionCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _logger = Substitute.For<ILogger<UpdateSubscriptionCommandHandler>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new UpdateSubscriptionCommandHandler(null!, _currentUserService, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullCurrentUserService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new UpdateSubscriptionCommandHandler(_context, null!, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("currentUserService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new UpdateSubscriptionCommandHandler(_context, _currentUserService, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateHandler()
    {
        // Act
        var handler = new UpdateSubscriptionCommandHandler(_context, _currentUserService, _logger);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region Handle Tests - Error Paths

    [Fact]
    public async Task Handle_WithNullCurrentUserId_ShouldReturnFailure()
    {
        // Arrange
        var handler = new UpdateSubscriptionCommandHandler(_context, _currentUserService, _logger);
        var command = new UpdateSubscriptionCommand
        {
            BusinessId = Guid.NewGuid(),
            PlatformSubscriptionId = Guid.NewGuid()
        };
        _currentUserService.UserId.Returns((Guid?)null);

        // Act - the handler catches InvalidOperationException and returns the message
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Current user ID is not available");
    }

    [Fact]
    public async Task Handle_WhenDatabaseExceptionThrown_ShouldReturnFailure()
    {
        // Arrange
        var handler = new UpdateSubscriptionCommandHandler(_context, _currentUserService, _logger);
        var command = new UpdateSubscriptionCommand
        {
            BusinessId = Guid.NewGuid(),
            PlatformSubscriptionId = Guid.NewGuid()
        };

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _context.Businesses.Returns(x => throw new Exception("Database error"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to update subscription");
    }

    [Fact]
    public void Command_ShouldHaveRequiredProperties()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        // Act
        var command = new UpdateSubscriptionCommand
        {
            BusinessId = businessId,
            PlatformSubscriptionId = subscriptionId
        };

        // Assert
        command.BusinessId.Should().Be(businessId);
        command.PlatformSubscriptionId.Should().Be(subscriptionId);
    }

    #endregion

    #region UpdateSubscriptionResult Tests

    [Fact]
    public void UpdateSubscriptionResult_Success_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var result = new UpdateSubscriptionResult
        {
            Success = true,
            Message = "Subscription updated successfully"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Subscription updated successfully");
    }

    [Fact]
    public void UpdateSubscriptionResult_Failure_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var result = new UpdateSubscriptionResult
        {
            Success = false,
            Message = "Failed to update subscription"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to update subscription");
    }

    #endregion
}
