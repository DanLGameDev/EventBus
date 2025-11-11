using System;
using System.Linq;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Bindings;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.EditMode
{
    [TestFixture]
    public class EventBusOfTTests
    {
        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
        }

        #region Typed Handler Registration Tests

        [Test]
        public void Register_TypedHandler_ReturnsValidBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act
            var binding = EventBus<TestEvent>.Register(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBinding<TestEvent>>(binding);
            Assert.AreEqual(0, binding.Priority);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Register_TypedHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            const int expectedPriority = 7;

            // Act
            var binding = EventBus<TestEvent>.Register(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameTypedHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act
            var binding1 = EventBus<TestEvent>.Register(handler);
            var binding2 = EventBus<TestEvent>.Register(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Register_DifferentTypedHandlers_CreatesMultipleBindings()
        {
            // Arrange
            Action<TestEvent> handler1 = evt => { };
            Action<TestEvent> handler2 = evt => { };

            // Act
            var binding1 = EventBus<TestEvent>.Register(handler1);
            var binding2 = EventBus<TestEvent>.Register(handler2);

            // Assert
            Assert.AreNotSame(binding1, binding2);
            Assert.AreEqual(2, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion

        #region No-Args Handler Registration Tests

        [Test]
        public void Register_NoArgsHandler_ReturnsValidBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding = EventBus<TestEvent>.Register(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBindingNoArgs>(binding);
            Assert.AreEqual(0, binding.Priority);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Register_NoArgsHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action handler = () => { };
            const int expectedPriority = 4;

            // Act
            var binding = EventBus<TestEvent>.Register(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameNoArgsHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding1 = EventBus<TestEvent>.Register(handler);
            var binding2 = EventBus<TestEvent>.Register(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion

        #region Pre-Created Binding Registration Tests

        [Test]
        public void Register_PreCreatedBinding_AddsToEventBus()
        {
            // Arrange
            var customBinding = new TypedEventBinding<TestEvent>(evt => { }, 9);

            // Act
            var returnedBinding = EventBus<TestEvent>.Register(customBinding);

            // Assert
            Assert.AreSame(customBinding, returnedBinding);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
            Assert.Contains(customBinding, EventBus<TestEvent>.BindingsContainer.Bindings.ToList());
        }

        [Test]
        public void Register_MultiplePreCreatedBindings_AddsAll()
        {
            // Arrange
            var binding1 = new TypedEventBinding<TestEvent>(evt => { }, 5);
            var binding2 = new NoArgsEventBinding(() => { }, 3);

            // Act
            EventBus<TestEvent>.Register(binding1);
            EventBus<TestEvent>.Register(binding2);

            // Assert
            Assert.AreEqual(2, EventBus<TestEvent>.BindingsContainer.Count);
            Assert.Contains(binding1, EventBus<TestEvent>.BindingsContainer.Bindings.ToList());
            Assert.Contains(binding2, EventBus<TestEvent>.BindingsContainer.Bindings.ToList());
        }

        #endregion

        #region Typed Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingTypedHandler_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            EventBus<TestEvent>.Register(handler);

            // Act
            EventBus<TestEvent>.Deregister(handler);

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Deregister_NonExistentTypedHandler_DoesNothing()
        {
            // Arrange
            Action<TestEvent> registeredHandler = evt => { };
            Action<TestEvent> unregisteredHandler = evt => { };
            EventBus<TestEvent>.Register(registeredHandler);

            // Act
            EventBus<TestEvent>.Deregister(unregisteredHandler);

            // Assert
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Deregister_OneOfMultipleTypedHandlers_RemovesOnlySpecified()
        {
            // Arrange
            Action<TestEvent> handler1 = evt => { };
            Action<TestEvent> handler2 = evt => { };
            EventBus<TestEvent>.Register(handler1);
            EventBus<TestEvent>.Register(handler2);

            // Act
            EventBus<TestEvent>.Deregister(handler1);

            // Assert
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
            var remainingBinding = EventBus<TestEvent>.BindingsContainer.Bindings.First() as TypedEventBinding<TestEvent>;
            Assert.IsTrue(remainingBinding.MatchesHandler(handler2));
        }

        #endregion

        #region No-Args Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingNoArgsHandler_RemovesBinding()
        {
            // Arrange
            Action handler = () => { };
            EventBus<TestEvent>.Register(handler);

            // Act
            EventBus<TestEvent>.Deregister(handler);

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Deregister_NonExistentNoArgsHandler_DoesNothing()
        {
            // Arrange
            Action registeredHandler = () => { };
            Action unregisteredHandler = () => { };
            EventBus<TestEvent>.Register(registeredHandler);

            // Act
            EventBus<TestEvent>.Deregister(unregisteredHandler);

            // Assert
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion

        #region Binding Direct Deregistration Tests

        [Test]
        public void Deregister_ExistingBinding_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            var binding = EventBus<TestEvent>.Register(handler);

            // Act
            EventBus<TestEvent>.Deregister(binding);

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Deregister_NonExistentBinding_DoesNothing()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            EventBus<TestEvent>.Register(handler);
            var unregisteredBinding = new TypedEventBinding<TestEvent>(evt => { });

            // Act
            EventBus<TestEvent>.Deregister(unregisteredBinding);

            // Assert
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion

        #region Clear All Bindings Tests

        [Test]
        public void ClearAllBindings_WithMultipleBindings_RemovesAll()
        {
            // Arrange
            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }));
            EventBus<TestEvent>.Register((Action)(() => { }));
            EventBus<TestEvent>.Register(new TypedEventBinding<TestEvent>(evt => { }));

            // Act
            EventBus<TestEvent>.ClearAllBindings();

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void ClearAllBindings_WithNoBindings_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => EventBus<TestEvent>.ClearAllBindings());
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
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
            EventBus<TestEvent>.Register(testEventHandler);
            EventBus<EmptyEvent>.Register(emptyEventHandler);

            // Assert
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
            Assert.AreEqual(1, EventBus<EmptyEvent>.BindingsContainer.Count);
        }

        [Test]
        public void ClearAllBindings_OnSpecificType_DoesNotAffectOtherTypes()
        {
            // Arrange
            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }));
            EventBus<EmptyEvent>.Register((Action<EmptyEvent>)(evt => { }));

            // Act
            EventBus<TestEvent>.ClearAllBindings();

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
            Assert.AreEqual(1, EventBus<EmptyEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Deregister_OnSpecificType_DoesNotAffectOtherTypes()
        {
            // Arrange
            Action<TestEvent> testEventHandler = evt => { };
            Action<EmptyEvent> emptyEventHandler = evt => { };
            EventBus<TestEvent>.Register(testEventHandler);
            EventBus<EmptyEvent>.Register(emptyEventHandler);

            // Act
            EventBus<TestEvent>.Deregister(testEventHandler);

            // Assert
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
            Assert.AreEqual(1, EventBus<EmptyEvent>.BindingsContainer.Count);
        }

        #endregion

        #region Priority Sorting Tests

        [Test]
        public void Register_HandlersWithDifferentPriorities_SortsCorrectly()
        {
            // Arrange & Act
            var lowPriorityBinding = EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }), 1);
            var highPriorityBinding = EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }), 10);
            var mediumPriorityBinding = EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }), 5);

            // Assert
            var bindings = EventBus<TestEvent>.BindingsContainer.Bindings.ToList();
            Assert.AreEqual(3, bindings.Count);
            Assert.AreEqual(10, bindings[0].Priority); // Highest first
            Assert.AreEqual(5, bindings[1].Priority);
            Assert.AreEqual(1, bindings[2].Priority);  // Lowest last
        }

        [Test]
        public void Register_MixedHandlerTypesWithPriorities_SortsCorrectly()
        {
            // Arrange & Act
            var typedBinding = EventBus<TestEvent>.Register((Action<TestEvent>)(evt => { }), 3);
            var noArgsBinding = EventBus<TestEvent>.Register((Action)(() => { }), 8);
            var preCreatedBinding = EventBus<TestEvent>.Register(new TypedEventBinding<TestEvent>(evt => { }, 5));

            // Assert
            var bindings = EventBus<TestEvent>.BindingsContainer.Bindings.ToList();
            Assert.AreEqual(3, bindings.Count);
            Assert.AreEqual(8, bindings[0].Priority);  // No-args binding (highest)
            Assert.AreEqual(5, bindings[1].Priority);  // Pre-created binding
            Assert.AreEqual(3, bindings[2].Priority);  // Typed binding (lowest)
        }

        #endregion

        #region Static Container Registration Tests

        [Test]
        public void StaticConstructor_RegistersContainerWithEventBus()
        {
            // This test verifies that the static constructor properly registers the container
            // We can't directly test the static constructor, but we can verify its effect
            // by ensuring that EventBus<T> operations work, which they wouldn't without registration

            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act - This should work if the container is properly registered
            var binding = EventBus<TestEvent>.Register(handler);

            // Assert - If the container wasn't registered, this would fail
            Assert.IsNotNull(binding);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion
    }
}