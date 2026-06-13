using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Events;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Events;

/// <summary>
/// Comprehensive tests for Invoice domain events targeting 100% code coverage
/// </summary>
public class InvoiceEventsTests
{
    #region InvoiceCreatedEvent Tests

    [Fact]
    public void InvoiceCreatedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-001";
        var tenantId = Guid.NewGuid();

        // Act
        var @event = new InvoiceCreatedEvent(invoiceId, invoiceReferenceNumber, tenantId);

        // Assert
        @event.InvoiceId.Should().Be(invoiceId);
        @event.InvoiceReferenceNumber.Should().Be(invoiceReferenceNumber);
        @event.TenantId.Should().Be(tenantId);
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.EventVersion.Should().Be(1);
    }

    [Fact]
    public void InvoiceCreatedEvent_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var @event = new InvoiceCreatedEvent(Guid.NewGuid(), "INV-001", Guid.NewGuid());

        // Assert
        @event.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void InvoiceCreatedEvent_ShouldSupportRecordEquality()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-001";
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        // Act
        var event1 = new InvoiceCreatedEvent(invoiceId, invoiceReferenceNumber, tenantId)
        {
            EventId = eventId,
            OccurredOn = occurredOn
        };
        var event2 = new InvoiceCreatedEvent(invoiceId, invoiceReferenceNumber, tenantId)
        {
            EventId = eventId,
            OccurredOn = occurredOn
        };

        // Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
        event1.GetHashCode().Should().Be(event2.GetHashCode());
    }

    [Fact]
    public void InvoiceCreatedEvent_ShouldSupportRecordInequality()
    {
        // Act
        var event1 = new InvoiceCreatedEvent(Guid.NewGuid(), "INV-001", Guid.NewGuid());
        var event2 = new InvoiceCreatedEvent(Guid.NewGuid(), "INV-002", Guid.NewGuid());

        // Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void InvoiceCreatedEvent_WithEmptyReferenceNumber_ShouldBeAllowed()
    {
        // Act
        var @event = new InvoiceCreatedEvent(Guid.NewGuid(), string.Empty, Guid.NewGuid());

        // Assert
        @event.InvoiceReferenceNumber.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceCreatedEvent_WithNullReferenceNumber_ShouldBeAllowed()
    {
        // Act
        var @event = new InvoiceCreatedEvent(Guid.NewGuid(), null!, Guid.NewGuid());

        // Assert
        @event.InvoiceReferenceNumber.Should().BeNull();
    }

    [Fact]
    public void InvoiceCreatedEvent_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-001";
        var tenantId = Guid.NewGuid();

        // Act
        var @event = new InvoiceCreatedEvent(invoiceId, invoiceReferenceNumber, tenantId);
        var toString = @event.ToString();

        // Assert
        toString.Should().NotBeNullOrEmpty();
        toString.Should().Contain("InvoiceCreatedEvent");
        toString.Should().Contain("INV-2024-001");
    }

    [Fact]
    public void InvoiceCreatedEvent_ShouldSupportWith()
    {
        // Arrange
        var originalEvent = new InvoiceCreatedEvent(
            Guid.NewGuid(),
            "INV-001",
            Guid.NewGuid());

        var newInvoiceId = Guid.NewGuid();

        // Act
        var modifiedEvent = originalEvent with { InvoiceId = newInvoiceId };

        // Assert
        modifiedEvent.InvoiceId.Should().Be(newInvoiceId);
        modifiedEvent.InvoiceReferenceNumber.Should().Be(originalEvent.InvoiceReferenceNumber);
        modifiedEvent.TenantId.Should().Be(originalEvent.TenantId);
        modifiedEvent.EventId.Should().Be(originalEvent.EventId);
    }

    #endregion

    #region InvoiceApprovedEvent Tests

    [Fact]
    public void InvoiceApprovedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-002";
        var tenantId = Guid.NewGuid();
        var approvedBy = Guid.NewGuid();

        // Act
        var @event = new InvoiceApprovedEvent(invoiceId, invoiceReferenceNumber, tenantId, approvedBy);

        // Assert
        @event.InvoiceId.Should().Be(invoiceId);
        @event.InvoiceReferenceNumber.Should().Be(invoiceReferenceNumber);
        @event.TenantId.Should().Be(tenantId);
        @event.ApprovedBy.Should().Be(approvedBy);
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void InvoiceApprovedEvent_WithDifferentValues_ShouldMaintainValues()
    {
        // Arrange
        var event1 = new InvoiceApprovedEvent(Guid.NewGuid(), "INV-001", Guid.NewGuid(), Guid.NewGuid());
        var event2 = new InvoiceApprovedEvent(Guid.NewGuid(), "INV-002", Guid.NewGuid(), Guid.NewGuid());

        // Assert
        event1.InvoiceReferenceNumber.Should().Be("INV-001");
        event2.InvoiceReferenceNumber.Should().Be("INV-002");
        event1.Should().NotBe(event2);
    }

    #endregion

    #region InvoiceSignedEvent Tests

    [Fact]
    public void InvoiceSignedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-003";
        var tenantId = Guid.NewGuid();

        // Act
        var @event = new InvoiceSignedEvent(invoiceId, invoiceReferenceNumber, tenantId);

        // Assert
        @event.InvoiceId.Should().Be(invoiceId);
        @event.InvoiceReferenceNumber.Should().Be(invoiceReferenceNumber);
        @event.TenantId.Should().Be(tenantId);
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void InvoiceSignedEvent_WithEmptyReferenceNumber_ShouldBeAllowed()
    {
        // Act
        var @event = new InvoiceSignedEvent(
            Guid.NewGuid(),
            string.Empty,
            Guid.NewGuid());

        // Assert
        @event.InvoiceReferenceNumber.Should().BeEmpty();
    }

    [Fact]
    public void InvoiceSignedEvent_ShouldSupportRecordFeatures()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-001";
        var tenantId = Guid.NewGuid();

        // Act
        var event1 = new InvoiceSignedEvent(invoiceId, invoiceReferenceNumber, tenantId)
        {
            EventId = Guid.NewGuid()
        };
        var event2 = event1 with { InvoiceReferenceNumber = "INV-002" };

        // Assert
        event2.InvoiceReferenceNumber.Should().Be("INV-002");
        event2.InvoiceId.Should().Be(event1.InvoiceId);
        event2.TenantId.Should().Be(event1.TenantId);
        event2.EventId.Should().Be(event1.EventId);
    }

    #endregion

    #region InvoiceSubmittedEvent Tests

    [Fact]
    public void InvoiceSubmittedEvent_Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceReferenceNumber = "INV-2024-004";
        var tenantId = Guid.NewGuid();
        var firsSubmissionId = "SUB-2024-001";

        // Act
        var @event = new InvoiceSubmittedEvent(
            invoiceId,
            invoiceReferenceNumber,
            tenantId,
            firsSubmissionId);

        // Assert
        @event.InvoiceId.Should().Be(invoiceId);
        @event.InvoiceReferenceNumber.Should().Be(invoiceReferenceNumber);
        @event.TenantId.Should().Be(tenantId);
        @event.FIRSSubmissionId.Should().Be(firsSubmissionId);
    }

    [Fact]
    public void InvoiceSubmittedEvent_WithNullValues_ShouldBeAllowed()
    {
        // Act
        var @event = new InvoiceSubmittedEvent(
            Guid.NewGuid(),
            null!,
            Guid.NewGuid(),
            null!);

        // Assert
        @event.InvoiceReferenceNumber.Should().BeNull();
        @event.FIRSSubmissionId.Should().BeNull();
    }

    #endregion

    #region Common Event Tests

    [Theory]
    [InlineData(typeof(InvoiceCreatedEvent))]
    [InlineData(typeof(InvoiceApprovedEvent))]
    [InlineData(typeof(InvoiceSignedEvent))]
    [InlineData(typeof(InvoiceSubmittedEvent))]
    public void AllInvoiceEvents_ShouldInheritFromDomainEvent(Type eventType)
    {
        // Assert
        eventType.BaseType.Should().NotBeNull();
        eventType.BaseType!.Name.Should().Be("DomainEvent");
    }

    [Fact]
    public void AllInvoiceEvents_ShouldBeRecords()
    {
        // Arrange
        var eventTypes = new[]
        {
            typeof(InvoiceCreatedEvent),
            typeof(InvoiceApprovedEvent),
            typeof(InvoiceSignedEvent),
            typeof(InvoiceSubmittedEvent)
        };

        // Assert
        foreach (var type in eventTypes)
        {
            type.IsClass.Should().BeTrue();
            type.GetMethod("<Clone>$", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Should().NotBeNull();
        }
    }

    [Fact]
    public async Task InvoiceEvents_ConcurrentCreation_ShouldHaveUniqueEventIds()
    {
        // Arrange
        var events = new List<IDomainEvent>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 25; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var evt = new InvoiceCreatedEvent(Guid.NewGuid(), $"INV-{i}", Guid.NewGuid());
                lock (events) { events.Add(evt); }
            }, TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() =>
            {
                var evt = new InvoiceApprovedEvent(Guid.NewGuid(), $"INV-{i}", Guid.NewGuid(), Guid.NewGuid());
                lock (events) { events.Add(evt); }
            }, TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() =>
            {
                var evt = new InvoiceSignedEvent(Guid.NewGuid(), $"INV-{i}", Guid.NewGuid());
                lock (events) { events.Add(evt); }
            }, TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() =>
            {
                var evt = new InvoiceSubmittedEvent(Guid.NewGuid(), $"INV-{i}", Guid.NewGuid(), $"SUB-{i}");
                lock (events) { events.Add(evt); }
            }, TestContext.Current.CancellationToken));
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
    public void InvoiceEvents_WithEmptyGuids_ShouldBeAllowed()
    {
        // Act
        var createdEvent = new InvoiceCreatedEvent(Guid.Empty, "INV", Guid.Empty);
        var approvedEvent = new InvoiceApprovedEvent(Guid.Empty, "INV", Guid.Empty, Guid.Empty);
        var signedEvent = new InvoiceSignedEvent(Guid.Empty, "INV", Guid.Empty);
        var submittedEvent = new InvoiceSubmittedEvent(Guid.Empty, "INV", Guid.Empty, "SUB");

        // Assert
        createdEvent.InvoiceId.Should().Be(Guid.Empty);
        approvedEvent.InvoiceId.Should().Be(Guid.Empty);
        signedEvent.InvoiceId.Should().Be(Guid.Empty);
        submittedEvent.InvoiceId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void InvoiceEvents_WithVeryLongStrings_ShouldBeAllowed()
    {
        // Arrange
        var veryLongString = new string('A', 10000);

        // Act
        var createdEvent = new InvoiceCreatedEvent(Guid.NewGuid(), veryLongString, Guid.NewGuid());
        var approvedEvent = new InvoiceApprovedEvent(Guid.NewGuid(), veryLongString, Guid.NewGuid(), Guid.NewGuid());
        var signedEvent = new InvoiceSignedEvent(Guid.NewGuid(), veryLongString, Guid.NewGuid());
        var submittedEvent = new InvoiceSubmittedEvent(Guid.NewGuid(), veryLongString, Guid.NewGuid(), veryLongString);

        // Assert
        createdEvent.InvoiceReferenceNumber.Should().Be(veryLongString);
        approvedEvent.InvoiceReferenceNumber.Should().Be(veryLongString);
        signedEvent.InvoiceReferenceNumber.Should().Be(veryLongString);
        submittedEvent.FIRSSubmissionId.Should().Be(veryLongString);
    }

    #endregion
}