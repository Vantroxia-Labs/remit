using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Events.UserManagement;
using FluentAssertions;

namespace AegisEInvoicing.UnitTests.DomainTests.Events.UserManagement;

/// <summary>
/// Comprehensive tests for User Management domain events targeting 100% code coverage
/// </summary>
public class UserManagementEventsTests
{
    #region UserCreatedEvent Tests

    [Fact]
    public void UserCreatedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "user@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var createdBy = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var @event = new UserCreatedEvent(userId, tenantId, email, firstName, lastName, createdBy, createdAt);

        // Assert
        @event.UserId.Should().Be(userId);
        @event.TenantId.Should().Be(tenantId);
        @event.Email.Should().Be(email);
        @event.FirstName.Should().Be(firstName);
        @event.LastName.Should().Be(lastName);
        @event.CreatedBy.Should().Be(createdBy);
        @event.CreatedAt.Should().Be(createdAt);
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.EventVersion.Should().Be(1);
    }

    [Fact]
    public void UserCreatedEvent_WithNullTenantId_ShouldBeAllowed()
    {
        // Act
        var @event = new UserCreatedEvent(
            Guid.NewGuid(),
            null,
            "user@example.com",
            "John",
            "Doe",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Assert
        @event.TenantId.Should().BeNull();
    }

    #endregion

    #region UserActivatedEvent Tests

    [Fact]
    public void UserActivatedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activatedBy = Guid.NewGuid();
        var activatedAt = DateTimeOffset.UtcNow;
        var emailAddress = "chsj@test.com";

        // Act
        var @event = new UserActivatedEvent(userId, null, Email: emailAddress, activatedBy, activatedAt);

        // Assert
        @event.UserId.Should().Be(userId);
        @event.ActivatedBy.Should().Be(activatedBy);
        @event.ActivatedAt.Should().Be(activatedAt);
    }

    #endregion
       
    #region UserLoginSuccessfulEvent Tests

    [Fact]
    public void UserLoginSuccessfulEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var email = "user@example.com";
        var ipAddress = "192.168.1.1";
        var loginAt = DateTimeOffset.UtcNow;

        // Act
        var @event = new UserLoginSuccessfulEvent(userId, tenantId, email, ipAddress, loginAt);

        // Assert
        @event.UserId.Should().Be(userId);
        @event.TenantId.Should().Be(tenantId);
        @event.Email.Should().Be(email);
        @event.IpAddress.Should().Be(ipAddress);
        @event.LoginAt.Should().Be(loginAt);
    }

    #endregion   

    #region Aegis Events Tests

    [Fact]
    public void UserAegisProfileUpdatedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@Aegis.com";
        var updatedBy = Guid.NewGuid();
        var changes = new Dictionary<string, object>
        {
            { "AegisEmployeeId", "Aegis12345" },
            { "AegisDepartment", "Audit" },
            { "Designation", "Senior Manager" }
        };
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var @event = new UserAegisProfileUpdatedEvent(userId, email, updatedBy, changes, updatedAt);

        // Assert
        @event.UserId.Should().Be(userId);
        @event.Email.Should().Be(email);
        @event.UpdatedBy.Should().Be(updatedBy);
        @event.Changes.Should().BeEquivalentTo(changes);
        @event.UpdatedAt.Should().Be(updatedAt);
    }

    #endregion

    #region Common Event Tests

    [Fact]
    public void AllUserManagementEvents_ShouldInheritFromDomainEvent()
    {
        // Arrange
        var eventTypes = new[]
        {
            typeof(UserCreatedEvent),
            typeof(UserActivatedEvent),
            typeof(UserDeactivatedEvent),
            typeof(UserDeletedEvent),
            typeof(UserPasswordChangedEvent),
            typeof(UserPasswordResetEvent),
            typeof(UserProfileUpdatedEvent),
            typeof(UserEmailUpdatedEvent),
            typeof(UserEmailVerifiedEvent),
            typeof(UserPhoneVerifiedEvent),
            typeof(UserLoginSuccessfulEvent),
            typeof(UserLoginFailedEvent),
            typeof(UserLockedOutEvent),
            typeof(UserUnlockedEvent),
            typeof(UserRoleAssignedEvent),
            typeof(UserRoleRemovedEvent),
            typeof(UserBranchAssignedEvent),
            typeof(UserBranchRemovedEvent),
            typeof(UserSuspendedEvent),
            typeof(UserAegisProfileUpdatedEvent),
            typeof(UserAegisRoleChangedEvent)
        };

        // Assert - Events can inherit from either DomainEvent or DomainEventBase
        var validBaseClassNames = new[] { "DomainEvent", "DomainEventBase" };
        foreach (var type in eventTypes)
        {
            type.BaseType.Should().NotBeNull();
            validBaseClassNames.Should().Contain(type.BaseType!.Name);
        }
    }

    [Fact]
    public void UserManagementEvents_ShouldBeRecords()
    {
        // Act
        var createdEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            "Test",
            "User",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var clonedEvent = createdEvent with { Email = "new@example.com" };

        // Assert
        clonedEvent.Email.Should().Be("new@example.com");
        clonedEvent.UserId.Should().Be(createdEvent.UserId);
        clonedEvent.FirstName.Should().Be(createdEvent.FirstName);
    }

    [Fact]
    public async Task UserManagementEvents_ConcurrentCreation_ShouldHaveUniqueEventIds()
    {
        // Arrange
        var events = new List<IDomainEvent>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var evt = new UserCreatedEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    $"user{i}@example.com",
                    $"User{i}",
                    $"Test{i}",
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);
                lock (events) { events.Add(evt); }
            }));
            tasks.Add(Task.Run(() =>
            {
                var evt = new UserLoginSuccessfulEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "user@example.com",
                    "192.168.1.1",
                    DateTimeOffset.UtcNow);
                lock (events) { events.Add(evt); }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        events.Should().HaveCount(100);
        var uniqueEventIds = events.Select(e => e.EventId).Distinct().Count();
        uniqueEventIds.Should().Be(100);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UserManagementEvents_WithNullStrings_ShouldBeAllowed()
    {
        // Act & Assert - Should not throw
        var createdEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            null,
            null!,
            null!,
            null!,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        createdEvent.Email.Should().BeNull();
        createdEvent.FirstName.Should().BeNull();
        createdEvent.LastName.Should().BeNull();
    }

    [Fact]
    public void UserManagementEvents_WithEmptyGuids_ShouldBeAllowed()
    {
        // Act
        var createdEvent = new UserCreatedEvent(
            Guid.Empty,
            Guid.Empty,
            "test@example.com",
            "Test",
            "User",
            Guid.Empty,
            DateTimeOffset.UtcNow);

        // Assert
        createdEvent.UserId.Should().Be(Guid.Empty);
        createdEvent.TenantId.Should().Be(Guid.Empty);
        createdEvent.CreatedBy.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UserManagementEvents_WithMinMaxDates_ShouldBeAllowed()
    {
        // Act
        var minDateEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            "Test",
            "User",
            Guid.NewGuid(),
            DateTimeOffset.MinValue);

        var maxDateEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            "Test",
            "User",
            Guid.NewGuid(),
            DateTimeOffset.MaxValue);

        // Assert
        minDateEvent.CreatedAt.Should().Be(DateTimeOffset.MinValue);
        maxDateEvent.CreatedAt.Should().Be(DateTimeOffset.MaxValue);
    }

    #endregion
}