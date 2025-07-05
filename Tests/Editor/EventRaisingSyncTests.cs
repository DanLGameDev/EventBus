using System;
using System.Collections.Generic;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Bindings;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.EditMode
{
    [TestFixture]
    public class EventRaisingSyncTests
    {
        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<PriorityTestEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<PriorityTestEvent>.ClearAllBindings();
        }

        #region EventBindingContainer Sync Raising Tests

        [Test]
        public void Container_Raise_WithTypedHandler_CallsHandler()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool handlerCalled = false;
            TestEvent receivedEvent = default;
            var testEvent = new TestEvent(42, "test");

            container.Register((Action<TestEvent>)(evt => {
                handlerCalled = true;
                receivedEvent = evt;
            }));

            // Act
            container.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(42, receivedEvent.Value);
            Assert.AreEqual("test", receivedEvent.Message);
        }

        [Test]
        public void Container_Raise_WithNoArgsHandler_CallsHandler()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool handlerCalled = false;

            container.Register((Action)(() => handlerCalled = true));

            // Act
            container.Raise(new TestEvent(1, "test"));

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void Container_Raise_WithMixedHandlers_CallsBothTypes()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool typedHandlerCalled = false;
            bool noArgsHandlerCalled = false;
            var testEvent = new TestEvent(99, "mixed");

            container.Register((Action<TestEvent>)(evt => {
                typedHandlerCalled = true;
                Assert.AreEqual(99, evt.Value);
            }));
            container.Register((Action)(() => noArgsHandlerCalled = true));

            // Act
            container.Raise(testEvent);

            // Assert
            Assert.IsTrue(typedHandlerCalled);
            Assert.IsTrue(noArgsHandlerCalled);
        }

        [Test]
        public void Container_Raise_WithNoHandlers_DoesNotThrow()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();

            // Act & Assert
            Assert.DoesNotThrow(() => container.Raise(new TestEvent()));
        }

        [Test]
        public void Container_Raise_WithDefaultEvent_CallsHandler()
        {
            // Arrange
            var container = new EventBindingContainer<EmptyEvent>();
            bool handlerCalled = false;

            container.Register((Action<EmptyEvent>)(evt => handlerCalled = true));

            // Act
            container.Raise();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void Container_Raise_WithPriorityHandlers_ExecutesInCorrectOrder()
        {
            // Arrange
            var container = new EventBindingContainer<PriorityTestEvent>();
            var executionOrder = new List<string>();

            container.Register((Action<PriorityTestEvent>)(evt => executionOrder.Add("Low")), 1);
            container.Register((Action<PriorityTestEvent>)(evt => executionOrder.Add("High")), 10);
            container.Register((Action<PriorityTestEvent>)(evt => executionOrder.Add("Medium")), 5);

            // Act
            container.Raise(new PriorityTestEvent(1));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]); // Highest priority first
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]); // Lowest priority last
        }

        [Test]
        public void Container_Raise_HandlerThrowsException_PropagatesException()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            container.Register((Action<TestEvent>)(evt => throw new InvalidOperationException("Test exception")));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => container.Raise(new TestEvent()));
            Assert.AreEqual("Test exception", exception.Message);
        }

        [Test]
        public void Container_Raise_FirstHandlerThrows_SubsequentHandlersNotCalled()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool secondHandlerCalled = false;

            container.Register((Action<TestEvent>)(evt => throw new InvalidOperationException("First exception")), 10);
            container.Register((Action<TestEvent>)(evt => secondHandlerCalled = true), 5);

            // Act
            try {
                container.Raise(new TestEvent());
            } catch (InvalidOperationException) {
                // Expected exception
            }

            // Assert
            Assert.IsFalse(secondHandlerCalled, "Second handler should not be called when first handler throws");
        }

        #endregion

        #region EventBus<T> Sync Raising Tests

        [Test]
        public void EventBusT_Raise_WithTypedHandler_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            TestEvent receivedEvent = default;
            var testEvent = new TestEvent(55, "eventbus");

            EventBus<TestEvent>.Register(evt => {
                handlerCalled = true;
                receivedEvent = evt;
            });

            // Act
            EventBus<TestEvent>.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(55, receivedEvent.Value);
            Assert.AreEqual("eventbus", receivedEvent.Message);
        }

        [Test]
        public void EventBusT_Raise_WithNoArgsHandler_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            EventBus<TestEvent>.Register(() => handlerCalled = true);

            // Act
            EventBus<TestEvent>.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void EventBusT_Raise_WithMixedHandlers_CallsBothTypes()
        {
            // Arrange
            bool typedHandlerCalled = false;
            bool noArgsHandlerCalled = false;
            var testEvent = new TestEvent(77, "mixed");

            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => {
                typedHandlerCalled = true;
                Assert.AreEqual(77, evt.Value);
            }));
            EventBus<TestEvent>.Register((Action)(() => noArgsHandlerCalled = true));

            // Act
            EventBus<TestEvent>.Raise(testEvent);

            // Assert
            Assert.IsTrue(typedHandlerCalled);
            Assert.IsTrue(noArgsHandlerCalled);
        }

        [Test]
        public void EventBusT_Raise_WithPriorityHandlers_ExecutesInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("Low"), 1);
            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("High"), 10);
            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("Medium"), 5);

            // Act
            EventBus<PriorityTestEvent>.Raise(new PriorityTestEvent(1));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [Test]
        public void EventBusT_Raise_HandlerThrowsException_PropagatesException()
        {
            // Arrange
            EventBus<TestEvent>.Register(evt => throw new InvalidOperationException("EventBus<T> exception"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => EventBus<TestEvent>.Raise(new TestEvent()));
            Assert.AreEqual("EventBus<T> exception", exception.Message);
        }

        #endregion

        #region Static EventBus Sync Raising Tests

        [Test]
        public void EventBus_Raise_WithTypedHandler_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            TestEvent receivedEvent = default;
            var testEvent = new TestEvent(88, "static");

            EventBus<TestEvent>.Register(evt => {
                handlerCalled = true;
                receivedEvent = evt;
            });

            // Act
            EventBus.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(88, receivedEvent.Value);
            Assert.AreEqual("static", receivedEvent.Message);
        }

        [Test]
        public void EventBus_Raise_WithPriorityHandlers_ExecutesInCorrectOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("Low"), 1);
            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("High"), 10);
            EventBus<PriorityTestEvent>.Register(evt => executionOrder.Add("Medium"), 5);

            // Act
            EventBus.Raise(new PriorityTestEvent(1));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [Test]
        public void EventBus_Raise_HandlerThrowsException_PropagatesWrappedException()
        {
            // Arrange
            EventBus<TestEvent>.Register(evt => throw new InvalidOperationException("Static EventBus exception"));

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => EventBus.Raise(new TestEvent()));
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            Assert.AreEqual("Static EventBus exception", exception.InnerException.Message);
        }

        #endregion

        #region EventContainer Sync Raising Tests

        [Test]
        public void EventContainer_Raise_WithTypedHandler_CallsHandler()
        {
            // Arrange
            var container = new EventContainer();
            bool handlerCalled = false;
            TestEvent receivedEvent = default;
            var testEvent = new TestEvent(33, "container");

            container.Register<TestEvent>(evt => {
                handlerCalled = true;
                receivedEvent = evt;
            });

            // Act
            container.Raise(testEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(33, receivedEvent.Value);
            Assert.AreEqual("container", receivedEvent.Message);
        }

        [Test]
        public void EventContainer_Raise_WithNoArgsHandler_CallsHandler()
        {
            // Arrange
            var container = new EventContainer();
            bool handlerCalled = false;

            container.Register<TestEvent>(() => handlerCalled = true);

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void EventContainer_Raise_WithMixedHandlers_CallsBothTypes()
        {
            // Arrange
            var container = new EventContainer();
            bool typedHandlerCalled = false;
            bool noArgsHandlerCalled = false;
            var testEvent = new TestEvent(66, "mixed");

            container.Register<TestEvent>((Action<TestEvent>)(evt => {
                typedHandlerCalled = true;
                Assert.AreEqual(66, evt.Value);
            }));
            container.Register<TestEvent>((Action)(() => noArgsHandlerCalled = true));

            // Act
            container.Raise(testEvent);

            // Assert
            Assert.IsTrue(typedHandlerCalled);
            Assert.IsTrue(noArgsHandlerCalled);
        }

        [Test]
        public void EventContainer_Raise_WithPriorityHandlers_ExecutesInCorrectOrder()
        {
            // Arrange
            var container = new EventContainer();
            var executionOrder = new List<string>();

            container.Register<PriorityTestEvent>(evt => executionOrder.Add("Low"), 1);
            container.Register<PriorityTestEvent>(evt => executionOrder.Add("High"), 10);
            container.Register<PriorityTestEvent>(evt => executionOrder.Add("Medium"), 5);

            // Act
            container.Raise(new PriorityTestEvent(1));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [Test]
        public void EventContainer_Raise_HandlerThrowsException_PropagatesException()
        {
            // Arrange
            var container = new EventContainer();
            container.Register<TestEvent>(evt => throw new InvalidOperationException("Container exception"));

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => container.Raise(new TestEvent()));
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            Assert.AreEqual("Container exception", exception.InnerException.Message);
        }

        #endregion

        #region EventContainer Polymorphic Raising Tests

        // Define event hierarchy for polymorphism testing
        public interface IRaisingBaseEvent : IEvent
        {
            string BaseMessage { get; }
        }
        
        public struct RaisingDerivedEvent : IRaisingBaseEvent
        {
            public string BaseMessage { get; }
            public int DerivedValue { get; }
            
            public RaisingDerivedEvent(string baseMessage, int derivedValue)
            {
                BaseMessage = baseMessage;
                DerivedValue = derivedValue;
            }
        }

        [Test]
        public void EventContainer_Raise_Polymorphic_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            var container = new EventContainer();
            var executionOrder = new List<string>();
            
            container.Register<IRaisingBaseEvent>(evt => executionOrder.Add($"Base:{evt.BaseMessage}"));
            container.Register<RaisingDerivedEvent>(evt => executionOrder.Add($"Derived:{evt.DerivedValue}"));

            var derivedEvent = new RaisingDerivedEvent("test", 42);

            // Act
            container.Raise(derivedEvent, polymorphic: true);

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base:test", executionOrder);
            Assert.Contains("Derived:42", executionOrder);
        }

        [Test]
        public void EventContainer_Raise_NonPolymorphic_OnlyTriggersExactTypeHandlers()
        {
            // Arrange
            var container = new EventContainer();
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            container.Register<IRaisingBaseEvent>(evt => baseHandlerCalled = true);
            container.Register<RaisingDerivedEvent>(evt => derivedHandlerCalled = true);

            var derivedEvent = new RaisingDerivedEvent("test", 42);

            // Act
            container.Raise(derivedEvent, polymorphic: false);

            // Assert
            Assert.IsFalse(baseHandlerCalled, "Base handler should not be called with polymorphic: false");
            Assert.IsTrue(derivedHandlerCalled, "Derived handler should be called");
        }

        [Test]
        public void EventContainer_Raise_DefaultPolymorphic_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            var container = new EventContainer();
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            container.Register<IRaisingBaseEvent>(evt => baseHandlerCalled = true);
            container.Register<RaisingDerivedEvent>(evt => derivedHandlerCalled = true);

            var derivedEvent = new RaisingDerivedEvent("test", 42);

            // Act - Default should be polymorphic: true
            container.Raise(derivedEvent);

            // Assert
            Assert.IsTrue(baseHandlerCalled, "Base handler should be called by default");
            Assert.IsTrue(derivedHandlerCalled, "Derived handler should be called");
        }

        #endregion

        #region Cross-API Compatibility Tests

        [Test]
        public void Raise_MixedRegistrationSources_AllHandlersCalled()
        {
            // Test that handlers registered via different APIs all get called

            // Arrange
            var container = new EventContainer();
            var executionOrder = new List<string>();

            // Register handlers via different APIs
            EventBus<TestEvent>.Register(evt => executionOrder.Add("EventBusT"));
            container.Register<TestEvent>(evt => executionOrder.Add("Container"));

            // Act - Use static EventBus to raise (should trigger EventBus<T> handlers)
            EventBus.Raise(new TestEvent());
            // Use container to raise (should only trigger container handler)
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("EventBusT", executionOrder);
            Assert.Contains("Container", executionOrder);
        }

        [Test]
        public void Raise_EventBusStatic_TriggersEventBusTHandlers()
        {
            // Arrange
            bool eventBusTHandlerCalled = false;
            EventBus<TestEvent>.Register(evt => eventBusTHandlerCalled = true);

            // Act
            EventBus.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(eventBusTHandlerCalled, "EventBus<T> handler should be called by static EventBus.Raise");
        }

        [Test]
        public void Raise_EventBusT_DoesNotTriggerContainerHandlers()
        {
            // Arrange
            var container = new EventContainer();
            bool containerHandlerCalled = false;
            bool eventBusTHandlerCalled = false;

            container.Register<TestEvent>(evt => containerHandlerCalled = true);
            EventBus<TestEvent>.Register(evt => eventBusTHandlerCalled = true);

            // Act
            EventBus<TestEvent>.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(eventBusTHandlerCalled, "EventBus<T> handler should be called");
            Assert.IsFalse(containerHandlerCalled, "Container handler should not be called");
        }

        #endregion

        #region Event Data Verification Tests

        [Test]
        public void Raise_WithComplexEventData_PassesDataCorrectly()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            TestEvent receivedEvent = default;
            var originalEvent = new TestEvent(12345, "complex test data with special chars: !@#$%");

            container.Register((Action<TestEvent>)(evt => receivedEvent = evt));

            // Act
            container.Raise(originalEvent);

            // Assert
            Assert.AreEqual(originalEvent.Value, receivedEvent.Value);
            Assert.AreEqual(originalEvent.Message, receivedEvent.Message);
        }

        [Test]
        public void Raise_WithDefaultEventData_PassesDefaultCorrectly()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            TestEvent receivedEvent = new TestEvent(999, "not default");

            container.Register((Action<TestEvent>)(evt => receivedEvent = evt));

            // Act
            container.Raise();

            // Assert
            Assert.AreEqual(default(TestEvent).Value, receivedEvent.Value);
            Assert.AreEqual(default(TestEvent).Message, receivedEvent.Message);
        }

        [Test]
        public void Raise_MultipleHandlers_AllReceiveSameEventData()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var receivedEvents = new List<TestEvent>();
            var originalEvent = new TestEvent(555, "shared data");

            container.Register((Action<TestEvent>)(evt => receivedEvents.Add(evt)));
            container.Register((Action<TestEvent>)(evt => receivedEvents.Add(evt)));
            container.Register((Action<TestEvent>)(evt => receivedEvents.Add(evt)));

            // Act
            container.Raise(originalEvent);

            // Assert
            Assert.AreEqual(3, receivedEvents.Count);
            foreach (var receivedEvent in receivedEvents) {
                Assert.AreEqual(originalEvent.Value, receivedEvent.Value);
                Assert.AreEqual(originalEvent.Message, receivedEvent.Message);
            }
        }

        #endregion

        #region Handler Execution Count Tests

        [Test]
        public void Raise_SingleHandler_CalledOnce()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            int callCount = 0;

            container.Register((Action<TestEvent>)(evt => callCount++));

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_MultipleHandlers_EachCalledOnce()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            int handler1CallCount = 0;
            int handler2CallCount = 0;
            int handler3CallCount = 0;

            container.Register((Action<TestEvent>)(evt => handler1CallCount++));
            container.Register((Action<TestEvent>)(evt => handler2CallCount++));
            container.Register((Action<TestEvent>)(evt => handler3CallCount++));

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(1, handler1CallCount);
            Assert.AreEqual(1, handler2CallCount);
            Assert.AreEqual(1, handler3CallCount);
        }

        [Test]
        public void Raise_CalledMultipleTimes_HandlersCalledCorrectNumberOfTimes()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            int callCount = 0;

            container.Register((Action<TestEvent>)(evt => callCount++));

            // Act
            container.Raise(new TestEvent());
            container.Raise(new TestEvent());
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(3, callCount);
        }

        #endregion
    }
}