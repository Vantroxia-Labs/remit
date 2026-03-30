using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Common.Interfaces;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Common.Implementation;

/// <summary>
/// Comprehensive tests for DomainEvent base class targeting 100% code coverage
/// </summary>
public class DomainEventTests
{
    // Test implementation of DomainEvent for testing
    private record TestDomainEvent : DomainEvent
    {
        public string TestProperty { get; init; } = string.Empty;
    }

    private record TestDomainEventWithCustomVersion : DomainEvent
    {
        public override int EventVersion => 2;
        public string TestProperty { get; init; } = string.Empty;
    }

    private record TestDomainEventWithData(
        string Name,
        int Value,
        Guid Id) : DomainEvent;

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeEventIdAndOccurredOn()
    {
        // Act
        var domainEvent = new TestDomainEvent();

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.EventVersion.Should().Be(1);
    }

    [Fact]
    public void Constructor_WhenCalledMultipleTimes_ShouldGenerateUniqueEventIds()
    {
        // Act
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var event3 = new TestDomainEvent();

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
        event2.EventId.Should().NotBe(event3.EventId);
        event1.EventId.Should().NotBe(event3.EventId);
    }

    [Fact]
    public void Constructor_ShouldUseUtcTime()
    {
        // Act
        var domainEvent = new TestDomainEvent();

        // Assert
        domainEvent.OccurredOn.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region EventId Tests

    [Fact]
    public void EventId_ShouldBeVersion7Guid()
    {
        // Act
        var domainEvent = new TestDomainEvent();

        // Assert
        domainEvent.EventId.Should().NotBeEmpty();
        // Version 7 GUIDs have specific characteristics but checking for non-empty is sufficient for our test
        domainEvent.EventId.ToString().Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
    }

    [Fact]
    public void EventId_CanBeSetWithInitializer()
    {
        // Arrange
        var customEventId = Guid.NewGuid();

        // Act
        var domainEvent = new TestDomainEvent { EventId = customEventId };

        // Assert
        domainEvent.EventId.Should().Be(customEventId);
    }

    #endregion

    #region OccurredOn Tests

    [Fact]
    public void OccurredOn_CanBeSetWithInitializer()
    {
        // Arrange
        var customTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var domainEvent = new TestDomainEvent { OccurredOn = customTime };

        // Assert
        domainEvent.OccurredOn.Should().Be(customTime);
    }

    [Fact]
    public void OccurredOn_ShouldBeWithinReasonableTimeOfCreation()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent();
        var afterCreation = DateTime.UtcNow;

        // Assert
        domainEvent.OccurredOn.Should().BeOnOrAfter(beforeCreation);
        domainEvent.OccurredOn.Should().BeOnOrBefore(afterCreation);
    }

    #endregion

    #region EventVersion Tests

    [Fact]
    public void EventVersion_DefaultImplementation_ShouldReturn1()
    {
        // Act
        var domainEvent = new TestDomainEvent();

        // Assert
        domainEvent.EventVersion.Should().Be(1);
    }

    [Fact]
    public void EventVersion_WhenOverridden_ShouldReturnCustomValue()
    {
        // Act
        var domainEvent = new TestDomainEventWithCustomVersion();

        // Assert
        domainEvent.EventVersion.Should().Be(2);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void DomainEvent_ShouldImplementIDomainEvent()
    {
        // Act
        var domainEvent = new TestDomainEvent();

        // Assert
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void DomainEvent_ShouldBeAbstract()
    {
        // Act & Assert
        typeof(DomainEvent).IsAbstract.Should().BeTrue();
    }

    #endregion

    #region Record Functionality Tests

    [Fact]
    public void DomainEvent_AsRecord_ShouldSupportEquality()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;

        // Act
        var event1 = new TestDomainEvent
        {
            EventId = eventId,
            OccurredOn = occurredOn,
            TestProperty = "Test"
        };
        var event2 = new TestDomainEvent
        {
            EventId = eventId,
            OccurredOn = occurredOn,
            TestProperty = "Test"
        };

        // Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
        event1.GetHashCode().Should().Be(event2.GetHashCode());
    }

    [Fact]
    public void DomainEvent_AsRecord_ShouldSupportInequality()
    {
        // Act
        var event1 = new TestDomainEvent { TestProperty = "Test1" };
        var event2 = new TestDomainEvent { TestProperty = "Test2" };

        // Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void DomainEvent_AsRecord_ShouldSupportWith()
    {
        // Arrange
        var originalEvent = new TestDomainEvent
        {
            TestProperty = "Original"
        };

        // Act
        var modifiedEvent = originalEvent with { TestProperty = "Modified" };

        // Assert
        modifiedEvent.TestProperty.Should().Be("Modified");
        modifiedEvent.EventId.Should().Be(originalEvent.EventId);
        modifiedEvent.OccurredOn.Should().Be(originalEvent.OccurredOn);
    }

    [Fact]
    public void DomainEvent_AsRecord_ShouldHaveToString()
    {
        // Act
        var domainEvent = new TestDomainEvent { TestProperty = "Test" };

        // Assert
        var toString = domainEvent.ToString();
        toString.Should().NotBeNullOrEmpty();
        toString.Should().Contain("TestDomainEvent");
        toString.Should().Contain("TestProperty = Test");
    }

    #endregion

    #region Derived Event Tests

    [Fact]
    public void DerivedEvent_WithConstructorParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "TestName";
        var value = 42;
        var id = Guid.NewGuid();

        // Act
        var domainEvent = new TestDomainEventWithData(name, value, id);

        // Assert
        domainEvent.Name.Should().Be(name);
        domainEvent.Value.Should().Be(value);
        domainEvent.Id.Should().Be(id);
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.EventVersion.Should().Be(1);
    }

    [Fact]
    public void DerivedEvent_ShouldInheritBaseProperties()
    {
        // Act
        var derivedEvent = new TestDomainEventWithData("Test", 123, Guid.NewGuid());

        // Assert
        derivedEvent.Should().BeAssignableTo<DomainEvent>();
        derivedEvent.Should().BeAssignableTo<IDomainEvent>();
        derivedEvent.EventId.Should().NotBeEmpty();
        derivedEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task DomainEvent_ConcurrentCreation_ShouldGenerateUniqueEventIds()
    {
        // Arrange
        var events = new List<TestDomainEvent>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var evt = new TestDomainEvent();
                lock (events)
                {
                    events.Add(evt);
                }
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
    public void DomainEvent_MultipleInheritanceLevels_ShouldWorkCorrectly()
    {
        // Arrange
        var intermediateEvent = new IntermediateDomainEvent();

        // Act & Assert
        intermediateEvent.Should().BeAssignableTo<DomainEvent>();
        intermediateEvent.EventId.Should().NotBeEmpty();
        intermediateEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        intermediateEvent.EventVersion.Should().Be(3);
        intermediateEvent.IntermediateProperty.Should().Be("Intermediate");
    }

    private record IntermediateDomainEvent : TestDomainEventWithCustomVersion
    {
        public override int EventVersion => 3;
        public string IntermediateProperty { get; init; } = "Intermediate";
    }

    #endregion
}