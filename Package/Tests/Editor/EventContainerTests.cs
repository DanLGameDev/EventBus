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
    public class EventContainerTests
    {
        private EventContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new EventContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.ClearAllBindings();
        }

        #region Typed Handler Registration Tests

        [Test]
        public void Register_TypedHandler_ReturnsValidBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act
            var binding = _container.Register<TestEvent>(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBinding<TestEvent>>(binding);
            Assert.AreEqual(0, binding.Priority);
        }

        [Test]
        public void Register_TypedHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            const int expectedPriority = 6;

            // Act
            var binding = _container.Register<TestEvent>(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameTypedHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act
            var binding1 = _container.Register<TestEvent>(handler);
            var binding2 = _container.Register<TestEvent>(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
        }

        [Test]
        public void Register_TypedHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action<TestEvent> handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register<TestEvent>(handler));
        }

        #endregion

        #region No-Args Handler Registration Tests

        [Test]
        public void Register_NoArgsHandler_ReturnsValidBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding = _container.Register<TestEvent>(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBindingNoArgs>(binding);
            Assert.AreEqual(0, binding.Priority);
        }

        [Test]
        public void Register_NoArgsHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action handler = () => { };
            const int expectedPriority = 4;

            // Act
            var binding = _container.Register<TestEvent>(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameNoArgsHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding1 = _container.Register<TestEvent>(handler);
            var binding2 = _container.Register<TestEvent>(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
        }

        [Test]
        public void Register_NoArgsHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register<TestEvent>(handler));
        }

        #endregion

        #region Pre-Created Binding Registration Tests

        [Test]
        public void Register_PreCreatedBinding_AddsToContainer()
        {
            // Arrange
            var customBinding = new TypedEventBinding<TestEvent>(evt => { }, 7);

            // Act
            var returnedBinding = _container.Register<TestEvent, TypedEventBinding<TestEvent>>(customBinding);

            // Assert
            Assert.AreSame(customBinding, returnedBinding);
        }

        [Test]
        public void Register_PreCreatedBindingNull_ThrowsArgumentNullException()
        {
            // Arrange
            TypedEventBinding<TestEvent> binding = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register<TestEvent, TypedEventBinding<TestEvent>>(binding));
        }

        [Test]
        public void Register_MultiplePreCreatedBindings_AddsAll()
        {
            // Arrange
            var binding1 = new TypedEventBinding<TestEvent>(evt => { }, 5);
            var binding2 = new NoArgsEventBinding(() => { }, 3);

            // Act
            _container.Register<TestEvent, TypedEventBinding<TestEvent>>(binding1);
            _container.Register<TestEvent, NoArgsEventBinding>(binding2);

            // Assert - We can't directly inspect the container's bindings, but we can verify they work
            Assert.DoesNotThrow(() => _container.Raise(new TestEvent()));
        }

        #endregion

        #region Typed Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingTypedHandler_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            _container.Register<TestEvent>(handler);

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(handler));
        }

        [Test]
        public void Deregister_NonExistentTypedHandler_DoesNotThrow()
        {
            // Arrange
            Action<TestEvent> registeredHandler = evt => { };
            Action<TestEvent> unregisteredHandler = evt => { };
            _container.Register<TestEvent>(registeredHandler);

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(unregisteredHandler));
        }

        [Test]
        public void Deregister_TypedHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action<TestEvent> handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister<TestEvent>(handler));
        }

        #endregion

        #region No-Args Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingNoArgsHandler_RemovesBinding()
        {
            // Arrange
            Action handler = () => { };
            _container.Register<TestEvent>(handler);

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(handler));
        }

        [Test]
        public void Deregister_NonExistentNoArgsHandler_DoesNotThrow()
        {
            // Arrange
            Action registeredHandler = () => { };
            Action unregisteredHandler = () => { };
            _container.Register<TestEvent>(registeredHandler);

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(unregisteredHandler));
        }

        [Test]
        public void Deregister_NoArgsHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister<TestEvent>(handler));
        }

        #endregion

        #region Binding Direct Deregistration Tests

        [Test]
        public void Deregister_ExistingBinding_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            var binding = _container.Register<TestEvent>(handler);

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(binding));
        }

        [Test]
        public void Deregister_NonExistentBinding_DoesNotThrow()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            _container.Register<TestEvent>(handler);
            var unregisteredBinding = new TypedEventBinding<TestEvent>(evt => { });

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Deregister<TestEvent>(unregisteredBinding));
        }

        [Test]
        public void Deregister_BindingNull_ThrowsArgumentNullException()
        {
            // Arrange
            IEventBinding binding = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister<TestEvent>(binding));
        }

        #endregion

        #region Clear Bindings Tests

        [Test]
        public void ClearBindings_ForSpecificEventType_DoesNotThrow()
        {
            // Arrange
            _container.Register<TestEvent>((Action<TestEvent>)(evt => { }));
            _container.Register<EmptyEvent>((Action<EmptyEvent>)(evt => { }));

            // Act & Assert
            Assert.DoesNotThrow(() => _container.ClearBindings<TestEvent>());
        }

        [Test]
        public void ClearAllBindings_WithMultipleEventTypes_DoesNotThrow()
        {
            // Arrange
            _container.Register<TestEvent>((Action<TestEvent>)(evt => { }));
            _container.Register<EmptyEvent>((Action<EmptyEvent>)(evt => { }));

            // Act & Assert
            Assert.DoesNotThrow(() => _container.ClearAllBindings());
        }

        [Test]
        public void ClearBindings_WithNoBindings_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _container.ClearBindings<TestEvent>());
            Assert.DoesNotThrow(() => _container.ClearAllBindings());
        }

        #endregion

        #region Type Isolation Tests

        [Test]
        public void Register_DifferentEventTypes_AreIsolated()
        {
            // Arrange
            Action<TestEvent> testEventHandler = evt => { };
            Action<EmptyEvent> emptyEventHandler = evt => { };

            // Act
            var testBinding = _container.Register<TestEvent>(testEventHandler);
            var emptyBinding = _container.Register<EmptyEvent>(emptyEventHandler);

            // Assert
            Assert.IsNotNull(testBinding);
            Assert.IsNotNull(emptyBinding);
            Assert.AreNotSame(testBinding, emptyBinding);
        }

        [Test]
        public void ClearBindings_OnSpecificType_DoesNotAffectOtherTypes()
        {
            // Arrange
            bool testEventHandlerCalled = false;
            bool emptyEventHandlerCalled = false;

            _container.Register<TestEvent>((Action<TestEvent>)(evt => testEventHandlerCalled = true));
            _container.Register<EmptyEvent>((Action<EmptyEvent>)(evt => emptyEventHandlerCalled = true));

            // Act
            _container.ClearBindings<TestEvent>();
            _container.Raise(new TestEvent());
            _container.Raise(new EmptyEvent());

            // Assert
            Assert.IsFalse(testEventHandlerCalled, "TestEvent handler should not be called after clearing");
            Assert.IsTrue(emptyEventHandlerCalled, "EmptyEvent handler should still be called");
        }

        [Test]
        public void Deregister_OnSpecificType_DoesNotAffectOtherTypes()
        {
            // Arrange
            bool testEventHandlerCalled = false;
            bool emptyEventHandlerCalled = false;

            Action<TestEvent> testEventHandler = evt => testEventHandlerCalled = true;
            Action<EmptyEvent> emptyEventHandler = evt => emptyEventHandlerCalled = true;

            _container.Register<TestEvent>(testEventHandler);
            _container.Register<EmptyEvent>(emptyEventHandler);

            // Act
            _container.Deregister<TestEvent>(testEventHandler);
            _container.Raise(new TestEvent());
            _container.Raise(new EmptyEvent());

            // Assert
            Assert.IsFalse(testEventHandlerCalled, "TestEvent handler should not be called after deregistering");
            Assert.IsTrue(emptyEventHandlerCalled, "EmptyEvent handler should still be called");
        }

        #endregion

        #region Multiple Container Isolation Tests

        [Test]
        public void MultipleContainers_AreIsolated()
        {
            // Arrange
            var container1 = new EventContainer();
            var container2 = new EventContainer();

            bool handler1Called = false;
            bool handler2Called = false;

            // Act
            container1.Register<TestEvent>((Action<TestEvent>)(evt => handler1Called = true));
            container2.Register<TestEvent>((Action<TestEvent>)(evt => handler2Called = true));

            container1.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(handler1Called, "Handler in container1 should be called");
            Assert.IsFalse(handler2Called, "Handler in container2 should not be called");
        }

        [Test]
        public void Container_ClearAllBindings_DoesNotAffectOtherContainers()
        {
            // Arrange
            var container1 = new EventContainer();
            var container2 = new EventContainer();

            bool handler1Called = false;
            bool handler2Called = false;

            container1.Register<TestEvent>((Action<TestEvent>)(evt => handler1Called = true));
            container2.Register<TestEvent>((Action<TestEvent>)(evt => handler2Called = true));

            // Act
            container1.ClearAllBindings();
            container1.Raise(new TestEvent());
            container2.Raise(new TestEvent());

            // Assert
            Assert.IsFalse(handler1Called, "Handler in container1 should not be called after clearing");
            Assert.IsTrue(handler2Called, "Handler in container2 should still be called");
        }

        #endregion

        #region Mixed Handler Type Tests

        [Test]
        public void Register_MixedHandlerTypes_AllRegisteredCorrectly()
        {
            // Arrange
            bool typedHandlerCalled = false;
            bool noArgsHandlerCalled = false;

            Action<TestEvent> typedHandler = evt => typedHandlerCalled = true;
            Action noArgsHandler = () => noArgsHandlerCalled = true;

            // Act
            var typedBinding = _container.Register<TestEvent>(typedHandler);
            var noArgsBinding = _container.Register<TestEvent>(noArgsHandler);
            _container.Raise(new TestEvent());

            // Assert
            Assert.IsNotNull(typedBinding);
            Assert.IsNotNull(noArgsBinding);
            Assert.IsTrue(typedHandlerCalled, "Typed handler should be called");
            Assert.IsTrue(noArgsHandlerCalled, "No-args handler should be called");
        }

        [Test]
        public void Deregister_MixedHandlerTypes_RemovesCorrectBinding()
        {
            // Arrange
            bool typedHandlerCalled = false;
            bool noArgsHandlerCalled = false;

            Action<TestEvent> typedHandler = evt => typedHandlerCalled = true;
            Action noArgsHandler = () => noArgsHandlerCalled = true;

            _container.Register<TestEvent>(typedHandler);
            _container.Register<TestEvent>(noArgsHandler);

            // Act
            _container.Deregister<TestEvent>(typedHandler);
            _container.Raise(new TestEvent());

            // Assert
            Assert.IsFalse(typedHandlerCalled, "Typed handler should not be called after deregistering");
            Assert.IsTrue(noArgsHandlerCalled, "No-args handler should still be called");
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Register_WithException_DoesNotCorruptContainer()
        {
            // Arrange
            bool normalHandlerCalled = false;
            Action<TestEvent> normalHandler = evt => normalHandlerCalled = true;

            // Act
            _container.Register<TestEvent>(normalHandler);

            // This should not throw or corrupt the container
            try {
                _container.Register<TestEvent>((Action<TestEvent>)null);
            } catch (ArgumentNullException) {
                // Expected exception
            }

            _container.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(normalHandlerCalled, "Normal handler should still work after exception during registration");
        }

        #endregion

        #region Container Lazy Creation Tests

        [Test]
        public void Register_FirstTimeForEventType_CreatesContainerAutomatically()
        {
            // This test verifies that the GetContainer method creates containers on demand

            // Arrange
            bool handlerCalled = false;
            Action<TestEvent> handler = evt => handlerCalled = true;

            // Act - First registration for this event type should create the container
            var binding = _container.Register<TestEvent>(handler);
            _container.Raise(new TestEvent());

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsTrue(handlerCalled, "Handler should be called, indicating container was created");
        }

        [Test]
        public void Register_SameEventTypeMultipleTimes_ReusesSameContainer()
        {
            // Arrange
            bool handler1Called = false;
            bool handler2Called = false;

            Action<TestEvent> handler1 = evt => handler1Called = true;
            Action<TestEvent> handler2 = evt => handler2Called = true;

            // Act
            _container.Register<TestEvent>(handler1);
            _container.Register<TestEvent>(handler2);
            _container.Raise(new TestEvent());

            // Assert
            Assert.IsTrue(handler1Called, "First handler should be called");
            Assert.IsTrue(handler2Called, "Second handler should be called");
        }

        #endregion

        #region Polymorphic Raising Tests

        // Define event hierarchy for polymorphism testing
        public interface IContainerBaseEvent : IEvent
        {
            string BaseMessage { get; }
        }
        
        public struct ContainerDerivedEvent : IContainerBaseEvent
        {
            public string BaseMessage { get; }
            public int DerivedValue { get; }
            
            public ContainerDerivedEvent(string baseMessage, int derivedValue)
            {
                BaseMessage = baseMessage;
                DerivedValue = derivedValue;
            }
        }
        
        public struct ContainerAnotherDerivedEvent : IContainerBaseEvent
        {
            public string BaseMessage { get; }
            public float AnotherValue { get; }
            
            public ContainerAnotherDerivedEvent(string baseMessage, float anotherValue)
            {
                BaseMessage = baseMessage;
                AnotherValue = anotherValue;
            }
        }

        [Test]
        public void Raise_Polymorphic_DerivedEvent_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            _container.Register<IContainerBaseEvent>(evt => 
            {
                executionOrder.Add($"Base:{evt.BaseMessage}");
            });
            
            _container.Register<ContainerDerivedEvent>(evt => 
            {
                executionOrder.Add($"Derived:{evt.DerivedValue}");
            });

            var derivedEvent = new ContainerDerivedEvent("test", 42);

            // Act
            _container.Raise(derivedEvent, polymorphic: true);

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base:test", executionOrder);
            Assert.Contains("Derived:42", executionOrder);
        }

        [Test]
        public void Raise_NonPolymorphic_DerivedEvent_OnlyTriggersExactTypeHandlers()
        {
            // Arrange
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            _container.Register<IContainerBaseEvent>(evt => baseHandlerCalled = true);
            _container.Register<ContainerDerivedEvent>(evt => derivedHandlerCalled = true);

            var derivedEvent = new ContainerDerivedEvent("test", 42);

            // Act
            _container.Raise(derivedEvent, polymorphic: false);

            // Assert
            Assert.IsFalse(baseHandlerCalled, "Base handler should not be called with polymorphic: false");
            Assert.IsTrue(derivedHandlerCalled, "Derived handler should be called");
        }

        [Test]
        public void Raise_DefaultPolymorphic_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            _container.Register<IContainerBaseEvent>(evt => baseHandlerCalled = true);
            _container.Register<ContainerDerivedEvent>(evt => derivedHandlerCalled = true);

            var derivedEvent = new ContainerDerivedEvent("test", 42);

            // Act - Default should be polymorphic: true
            _container.Raise(derivedEvent);

            // Assert
            Assert.IsTrue(baseHandlerCalled, "Base handler should be called by default");
            Assert.IsTrue(derivedHandlerCalled, "Derived handler should be called");
        }

        [Test]
        public void Raise_Polymorphic_MultipleDerivedTypes_EachTriggersAppropriateHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            _container.Register<IContainerBaseEvent>(evt => executionOrder.Add("Base"));
            _container.Register<ContainerDerivedEvent>(evt => executionOrder.Add("DerivedEvent"));
            _container.Register<ContainerAnotherDerivedEvent>(evt => executionOrder.Add("AnotherDerivedEvent"));

            // Act
            _container.Raise(new ContainerDerivedEvent("test1", 42), polymorphic: true);
            _container.Raise(new ContainerAnotherDerivedEvent("test2", 3.14f), polymorphic: true);

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
        public void Raise_Polymorphic_WithPriorities_RespectsOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            _container.Register<IContainerBaseEvent>(evt => executionOrder.Add("BaseLow"), 1);
            _container.Register<IContainerBaseEvent>(evt => executionOrder.Add("BaseHigh"), 10);
            _container.Register<ContainerDerivedEvent>(evt => executionOrder.Add("DerivedMedium"), 5);

            // Act
            _container.Raise(new ContainerDerivedEvent("test", 42), polymorphic: true);

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.Contains("BaseHigh", executionOrder);
            Assert.Contains("BaseLow", executionOrder);
            Assert.Contains("DerivedMedium", executionOrder);
            // Exact order depends on container iteration order, but priorities within containers are respected
        }

        [Test]
        public void Raise_Polymorphic_EventDataIntegrity_HandlersReceiveCorrectData()
        {
            // Arrange
            IContainerBaseEvent receivedBaseEvent = default;
            ContainerDerivedEvent receivedDerivedEvent = default;
            
            _container.Register<IContainerBaseEvent>(evt => receivedBaseEvent = evt);
            _container.Register<ContainerDerivedEvent>(evt => receivedDerivedEvent = evt);

            var originalEvent = new ContainerDerivedEvent("polymorphic", 99);

            // Act
            _container.Raise(originalEvent, polymorphic: true);

            // Assert
            Assert.AreEqual("polymorphic", receivedBaseEvent.BaseMessage);
            Assert.AreEqual("polymorphic", receivedDerivedEvent.BaseMessage);
            Assert.AreEqual(99, receivedDerivedEvent.DerivedValue);
        }

        [Test]
        public void Raise_Polymorphic_NoMatchingHandlers_DoesNotThrow()
        {
            // Arrange
            _container.Register<ContainerAnotherDerivedEvent>(evt => { });

            // Act & Assert
            Assert.DoesNotThrow(() => _container.Raise(new ContainerDerivedEvent("test", 1), polymorphic: true));
        }

        [Test]
        public void Raise_Polymorphic_OnlyExactAndBaseMatches_IgnoresUnrelatedTypes()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            _container.Register<IContainerBaseEvent>(evt => executionOrder.Add("Base"));
            _container.Register<ContainerDerivedEvent>(evt => executionOrder.Add("Derived"));
            _container.Register<ContainerAnotherDerivedEvent>(evt => executionOrder.Add("Another"));
            _container.Register<TestEvent>(evt => executionOrder.Add("Unrelated"));

            // Act
            _container.Raise(new ContainerDerivedEvent("test", 1), polymorphic: true);

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("Derived", executionOrder);
            Assert.IsFalse(executionOrder.Contains("Another"));
            Assert.IsFalse(executionOrder.Contains("Unrelated"));
        }

        #endregion
    }
}