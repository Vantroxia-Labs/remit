using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands;
using AegisEInvoicing.Application.Features.BusinessManagement.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AegisEInvoicing.UnitTests.ApplicationTests.Features.BusinessManagement.Handlers;

/// <summary>
/// Tests for RemoveUserFromBusinessCommandHandler
/// Focuses on constructor validation and error handling paths
/// </summary>
public class RemoveUserFromBusinessCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveUserFromBusinessCommandHandler> _logger;

    public RemoveUserFromBusinessCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _logger = Substitute.For<ILogger<RemoveUserFromBusinessCommandHandler>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new RemoveUserFromBusinessCommandHandler(null!, _currentUserService, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullCurrentUserService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new RemoveUserFromBusinessCommandHandler(_context, null!, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("currentUserService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new RemoveUserFromBusinessCommandHandler(_context, _currentUserService, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateHandler()
    {
        // Act
        var handler = new RemoveUserFromBusinessCommandHandler(_context, _currentUserService, _logger);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region Handle Tests - Error Paths

    [Fact]
    public async Task Handle_WithNullCurrentUserId_ShouldReturnFailure()
    {
        // Arrange
        var handler = new RemoveUserFromBusinessCommandHandler(_context, _currentUserService, _logger);
        var command = new RemoveUserFromBusinessCommand
        {
            UserId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid()
        };
        _currentUserService.UserId.Returns((Guid?)null);

        // Act - the handler catches the InvalidOperationException and returns failure
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to remove user from business");
    }

    [Fact]
    public async Task Handle_WhenDatabaseExceptionThrown_ShouldReturnFailure()
    {
        // Arrange
        var handler = new RemoveUserFromBusinessCommandHandler(_context, _currentUserService, _logger);
        var command = new RemoveUserFromBusinessCommand
        {
            UserId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid()
        };

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _context.Users.Returns(x => throw new Exception("Database error"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to remove user from business");
    }

    [Fact]
    public void Command_ShouldHaveRequiredProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        // Act
        var command = new RemoveUserFromBusinessCommand
        {
            UserId = userId,
            BusinessId = businessId
        };

        // Assert
        command.UserId.Should().Be(userId);
        command.BusinessId.Should().Be(businessId);
    }

    #endregion
}
