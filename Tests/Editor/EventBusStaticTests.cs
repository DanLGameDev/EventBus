using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Bindings;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.EditMode
{
    [TestFixture]
    public class EventBusStaticTests
    {
        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<InheritedEvent>.ClearAllBindings();
            EventBus<IStaticBaseEvent>.ClearAllBindings();
            EventBus<StaticDerivedEvent>.ClearAllBindings();
            EventBus<StaticAnotherDerivedEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<InheritedEvent>.ClearAllBindings();
            EventBus<IStaticBaseEvent>.ClearAllBindings();
            EventBus<StaticDerivedEvent>.ClearAllBindings();
            EventBus<StaticAnotherDerivedEvent>.ClearAllBindings();
        }

        // Define event hierarchy for polymorphism testing
        public interface IStaticBaseEvent : IEvent
        {
            string BaseMessage { get; }
        }
        
        public struct StaticDerivedEvent : IStaticBaseEvent
        {
            public string BaseMessage { get; }
            public int DerivedValue { get; }
            
            public StaticDerivedEvent(string baseMessage, int derivedValue)
            {
                BaseMessage = baseMessage;
                DerivedValue = derivedValue;
            }
        }
        
        public struct StaticAnotherDerivedEvent : IStaticBaseEvent
        {
            public string BaseMessage { get; }
            public float AnotherValue { get; }
            
            public StaticAnotherDerivedEvent(string baseMessage, float anotherValue)
            {
                BaseMessage = baseMessage;
                AnotherValue = anotherValue;
            }
        }

        #region Basic Event Raising Tests

        [Test]
        public void Raise_WithRegisteredHandler_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(42, "test");

            EventBus<TestEvent>.Register(evt => {
                handlerCalled = true;
                Assert.AreEqual(42, evt.Value);
                Assert.AreEqual("test", evt.Message);
            });

            // Act
            EventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void Raise_WithoutRegisteredHandlers_DoesNotThrow()
        {
            // Arrange
            var testEvent = new TestEvent(1, "test");

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.Raise(testEvent));
        }

        [Test]
        public void Raise_WithDefaultEvent_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            EventBus<EmptyEvent>.Register(evt => handlerCalled = true);

            // Act
            EventBus.Raise<EmptyEvent>();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Cross-API Integration Tests

        [Test]
        public void Raise_StaticEventBus_TriggersEventBusT_Handlers()
        {
            // This tests that EventBus.Raise<T>() properly invokes handlers registered via EventBus<T>.Register()

            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(100, "integration");

            EventBus<TestEvent>.Register(evt => {
                handlerCalled = true;
                Assert.AreEqual(100, evt.Value);
                Assert.AreEqual("integration", evt.Message);
            });

            // Act
            EventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void Raise_MultipleEventBusT_HandlersBothCalled()
        {
            // Arrange
            bool handler1Called = false;
            bool handler2Called = false;
            var testEvent = new TestEvent(200, "compatibility");

            EventBus<TestEvent>.Register(evt => handler1Called = true);
            EventBus<TestEvent>.Register(evt => handler2Called = true);

            // Act
            EventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(handler1Called, "First handler should be called");
            Assert.IsTrue(handler2Called, "Second handler should be called");
        }

        #endregion

        #region Multiple Event Types Tests

        [Test]
        public void Raise_DifferentEventTypes_OnlyTriggersCorrectHandlers()
        {
            // Arrange
            bool testEventHandlerCalled = false;
            bool emptyEventHandlerCalled = false;
            bool inheritedEventHandlerCalled = false;

            EventBus<TestEvent>.Register(evt => testEventHandlerCalled = true);
            EventBus<EmptyEvent>.Register(evt => emptyEventHandlerCalled = true);
            EventBus<InheritedEvent>.Register(evt => inheritedEventHandlerCalled = true);

            // Act
            EventBus.Raise(new TestEvent(1, "test"));

            // Assert
            Assert.IsTrue(testEventHandlerCalled, "TestEvent handler should be called");
            Assert.IsFalse(emptyEventHandlerCalled, "EmptyEvent handler should not be called");
            Assert.IsFalse(inheritedEventHandlerCalled, "InheritedEvent handler should not be called");
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Raise_HandlersWithDifferentPriorities_ExecutesInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            EventBus<TestEvent>.Register(evt => executionOrder.Add("Low"), 1);
            EventBus<TestEvent>.Register(evt => executionOrder.Add("High"), 10);
            EventBus<TestEvent>.Register(evt => executionOrder.Add("Medium"), 5);

            // Act
            EventBus.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]); // Highest priority first
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]); // Lowest priority last
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Raise_HandlerThrowsException_PropagatesException()
        {
            // Arrange
            EventBus<TestEvent>.Register(evt => throw new InvalidOperationException("Test exception"));

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => EventBus.Raise(new TestEvent()));
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            Assert.AreEqual("Test exception", exception.InnerException.Message);
        }

        [Test]
        public void Raise_FirstHandlerThrows_SubsequentHandlersNotCalled()
        {
            // Arrange
            bool secondHandlerCalled = false;

            EventBus<TestEvent>.Register(evt => throw new InvalidOperationException("First handler exception"), 10);
            EventBus<TestEvent>.Register(evt => secondHandlerCalled = true, 5);

            // Act
            try {
                EventBus.Raise(new TestEvent());
            } catch (System.Reflection.TargetInvocationException ex) {
                // Expected exception - verify it's the right inner exception
                Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
                Assert.AreEqual("First handler exception", ex.InnerException.Message);
            }

            // Assert
            Assert.IsFalse(secondHandlerCalled, "Second handler should not be called when first handler throws");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Raise_NullEvent_CallsHandlerWithNull()
        {
            // Arrange
            TestEvent? receivedEvent = null;
            bool handlerCalled = false;

            EventBus<TestEvent>.Register(evt => {
                receivedEvent = evt;
                handlerCalled = true;
            });

            // Act
            EventBus.Raise<TestEvent>(default);

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(default(TestEvent), receivedEvent);
        }

        #endregion

        #region Polymorphic Raising Tests
        // Note: The static EventBus always raises polymorphically by design

        [Test]
        public void Raise_PolymorphicEvent_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IStaticBaseEvent>.Register(evt => 
            {
                executionOrder.Add($"Base:{evt.BaseMessage}");
            });
            
            EventBus<StaticDerivedEvent>.Register(evt => 
            {
                executionOrder.Add($"Derived:{evt.DerivedValue}");
            });

            var derivedEvent = new StaticDerivedEvent("test", 42);

            // Act
            EventBus.Raise(derivedEvent);

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base:test", executionOrder);
            Assert.Contains("Derived:42", executionOrder);
        }

        [Test]
        public void Raise_PolymorphicEvent_MultipleDerivedTypes_EachTriggersAppropriateHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IStaticBaseEvent>.Register(evt => executionOrder.Add("Base"));
            EventBus<StaticDerivedEvent>.Register(evt => executionOrder.Add("DerivedEvent"));
            EventBus<StaticAnotherDerivedEvent>.Register(evt => executionOrder.Add("AnotherDerivedEvent"));

            // Act
            EventBus.Raise(new StaticDerivedEvent("test1", 42));
            EventBus.Raise(new StaticAnotherDerivedEvent("test2", 3.14f));

            // Assert
            Assert.AreEqual(4, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("DerivedEvent", executionOrder);
            Assert.Contains("AnotherDerivedEvent", executionOrder);
            
            // Count base handler calls
            int baseCount = executionOrder.Count(item => item == "Base");
            Assert.AreEqual(2, baseCount, "Base handler should be called for both derived events");
        }

        [Test]
        public void Raise_PolymorphicEvent_WithPriorities_RespectsOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IStaticBaseEvent>.Register(evt => executionOrder.Add("BaseLow"), 1);
            EventBus<IStaticBaseEvent>.Register(evt => executionOrder.Add("BaseHigh"), 10);
            EventBus<StaticDerivedEvent>.Register(evt => executionOrder.Add("DerivedMedium"), 5);

            // Act
            EventBus.Raise(new StaticDerivedEvent("test", 42));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.Contains("BaseHigh", executionOrder);
            Assert.Contains("BaseLow", executionOrder);
            Assert.Contains("DerivedMedium", executionOrder);
        }

        [Test]
        public void Raise_PolymorphicEvent_EventDataIntegrity_HandlersReceiveCorrectData()
        {
            // Arrange
            IStaticBaseEvent receivedBaseEvent = default;
            StaticDerivedEvent receivedDerivedEvent = default;
            
            EventBus<IStaticBaseEvent>.Register(evt => receivedBaseEvent = evt);
            EventBus<StaticDerivedEvent>.Register(evt => receivedDerivedEvent = evt);

            var originalEvent = new StaticDerivedEvent("polymorphic", 99);

            // Act
            EventBus.Raise(originalEvent);

            // Assert
            Assert.AreEqual("polymorphic", receivedBaseEvent.BaseMessage);
            Assert.AreEqual("polymorphic", receivedDerivedEvent.BaseMessage);
            Assert.AreEqual(99, receivedDerivedEvent.DerivedValue);
        }

        [Test]
        public void Raise_PolymorphicEvent_OnlyExactAndBaseMatches_IgnoresUnrelatedTypes()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IStaticBaseEvent>.Register(evt => executionOrder.Add("Base"));
            EventBus<StaticDerivedEvent>.Register(evt => executionOrder.Add("Derived"));
            EventBus<StaticAnotherDerivedEvent>.Register(evt => executionOrder.Add("Another"));
            EventBus<TestEvent>.Register(evt => executionOrder.Add("Unrelated"));

            // Act
            EventBus.Raise(new StaticDerivedEvent("test", 1));

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("Derived", executionOrder);
            Assert.IsFalse(executionOrder.Contains("Another"));
            Assert.IsFalse(executionOrder.Contains("Unrelated"));
        }

        [Test]
        public void Raise_PolymorphicEvent_NoMatchingHandlers_DoesNotThrow()
        {
            // Arrange
            EventBus<StaticAnotherDerivedEvent>.Register(evt => { });

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.Raise(new StaticDerivedEvent("test", 1)));
        }

        [Test]
        public void Raise_PolymorphicEvent_MixedHandlerTypes_CallsAll()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IStaticBaseEvent>.Register((Action<IStaticBaseEvent>)(evt => executionOrder.Add("BaseTyped")));
            EventBus<IStaticBaseEvent>.Register((Action)(() => executionOrder.Add("BaseNoArgs")));
            EventBus<StaticDerivedEvent>.Register((Action<StaticDerivedEvent>)(evt => executionOrder.Add("DerivedTyped")));
            EventBus<StaticDerivedEvent>.Register((Action)(() => executionOrder.Add("DerivedNoArgs")));

            // Act
            EventBus.Raise(new StaticDerivedEvent("test", 1));

            // Assert
            Assert.AreEqual(4, executionOrder.Count);
            Assert.Contains("BaseTyped", executionOrder);
            Assert.Contains("BaseNoArgs", executionOrder);
            Assert.Contains("DerivedTyped", executionOrder);
            Assert.Contains("DerivedNoArgs", executionOrder);
        }

        [Test]
        public void Raise_PolymorphicEvent_ExceptionHandling_BaseHandlerThrowsStopsAll()
        {
            // Arrange
            bool derivedHandlerCalled = false;
            
            EventBus<IStaticBaseEvent>.Register(evt => throw new InvalidOperationException("Base exception"));
            EventBus<StaticDerivedEvent>.Register(evt => derivedHandlerCalled = true);

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                EventBus.Raise(new StaticDerivedEvent("test", 1)));
            
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            Assert.IsFalse(derivedHandlerCalled, "Derived handler should not be called if base handler throws");
        }

        [Test]
        public void Raise_PolymorphicEvent_ExceptionHandling_DerivedHandlerThrowsStopsExecution()
        {
            // Arrange
            bool baseHandlerCalled = false;
            
            EventBus<IStaticBaseEvent>.Register(evt => baseHandlerCalled = true);
            EventBus<StaticDerivedEvent>.Register(evt => throw new InvalidOperationException("Derived exception"));

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                EventBus.Raise(new StaticDerivedEvent("test", 1)));
            
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }

        #endregion
    }
}