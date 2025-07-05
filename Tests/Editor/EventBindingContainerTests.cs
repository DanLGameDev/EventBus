using System;
using System.Linq;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Bindings;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.EditMode
{
    [TestFixture]
    public class EventBindingContainerTests
    {
        private EventBindingContainer<TestEvent> _container;

        [SetUp]
        public void Setup()
        {
            _container = new EventBindingContainer<TestEvent>();
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
            var binding = _container.Register(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBinding<TestEvent>>(binding);
            Assert.IsInstanceOf<TypedEventBinding<TestEvent>>(binding);
            Assert.AreEqual(0, binding.Priority);
        }

        [Test]
        public void Register_TypedHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            const int expectedPriority = 5;

            // Act
            var binding = _container.Register(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameTypedHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };

            // Act
            var binding1 = _container.Register(handler);
            var binding2 = _container.Register(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
            Assert.AreEqual(1, _container.Bindings.Count);
        }

        [Test]
        public void Register_DifferentTypedHandlers_CreatesMultipleBindings()
        {
            // Arrange
            Action<TestEvent> handler1 = evt => { };
            Action<TestEvent> handler2 = evt => { };

            // Act
            var binding1 = _container.Register(handler1);
            var binding2 = _container.Register(handler2);

            // Assert
            Assert.AreNotSame(binding1, binding2);
            Assert.AreEqual(2, _container.Bindings.Count);
        }

        [Test]
        public void Register_TypedHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action<TestEvent> handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register(handler));
        }

        #endregion

        #region No-Args Handler Registration Tests

        [Test]
        public void Register_NoArgsHandler_ReturnsValidBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding = _container.Register(handler);

            // Assert
            Assert.IsNotNull(binding);
            Assert.IsInstanceOf<IEventBindingNoArgs>(binding);
            Assert.IsInstanceOf<NoArgsEventBinding>(binding);
            Assert.AreEqual(0, binding.Priority);
        }

        [Test]
        public void Register_NoArgsHandlerWithPriority_SetsCorrectPriority()
        {
            // Arrange
            Action handler = () => { };
            const int expectedPriority = 3;

            // Act
            var binding = _container.Register(handler, expectedPriority);

            // Assert
            Assert.AreEqual(expectedPriority, binding.Priority);
        }

        [Test]
        public void Register_SameNoArgsHandlerTwice_ReturnsSameBinding()
        {
            // Arrange
            Action handler = () => { };

            // Act
            var binding1 = _container.Register(handler);
            var binding2 = _container.Register(handler);

            // Assert
            Assert.AreSame(binding1, binding2);
            Assert.AreEqual(1, _container.Bindings.Count);
        }

        [Test]
        public void Register_NoArgsHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register(handler));
        }

        #endregion

        #region Pre-Created Binding Registration Tests

        [Test]
        public void Register_PreCreatedBinding_AddsToContainer()
        {
            // Arrange
            var customBinding = new TypedEventBinding<TestEvent>(evt => { }, 7);

            // Act
            var returnedBinding = _container.Register(customBinding);

            // Assert
            Assert.AreSame(customBinding, returnedBinding);
            Assert.AreEqual(1, _container.Bindings.Count);
            Assert.Contains(customBinding, _container.Bindings.ToList());
        }

        [Test]
        public void Register_PreCreatedBindingNull_ThrowsArgumentNullException()
        {
            // Arrange
            TypedEventBinding<TestEvent> binding = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Register(binding));
        }

        [Test]
        public void Register_MultiplePreCreatedBindings_AddsAll()
        {
            // Arrange
            var binding1 = new TypedEventBinding<TestEvent>(evt => { }, 5);
            var binding2 = new NoArgsEventBinding(() => { }, 3);

            // Act
            _container.Register(binding1);
            _container.Register(binding2);

            // Assert
            Assert.AreEqual(2, _container.Bindings.Count);
            Assert.Contains(binding1, _container.Bindings.ToList());
            Assert.Contains(binding2, _container.Bindings.ToList());
        }

        #endregion

        #region Typed Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingTypedHandler_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            _container.Register(handler);

            // Act
            _container.Deregister(handler);

            // Assert
            Assert.AreEqual(0, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_NonExistentTypedHandler_DoesNothing()
        {
            // Arrange
            Action<TestEvent> registeredHandler = evt => { };
            Action<TestEvent> unregisteredHandler = evt => { };
            _container.Register(registeredHandler);

            // Act
            _container.Deregister(unregisteredHandler);

            // Assert
            Assert.AreEqual(1, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_TypedHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action<TestEvent> handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister(handler));
        }

        [Test]
        public void Deregister_OneOfMultipleTypedHandlers_RemovesOnlySpecified()
        {
            // Arrange
            Action<TestEvent> handler1 = evt => { };
            Action<TestEvent> handler2 = evt => { };
            _container.Register(handler1);
            _container.Register(handler2);

            // Act
            _container.Deregister(handler1);

            // Assert
            Assert.AreEqual(1, _container.Bindings.Count);
            var remainingBinding = _container.Bindings.First() as TypedEventBinding<TestEvent>;
            Assert.IsTrue(remainingBinding.MatchesHandler(handler2));
        }

        #endregion

        #region No-Args Handler Deregistration Tests

        [Test]
        public void Deregister_ExistingNoArgsHandler_RemovesBinding()
        {
            // Arrange
            Action handler = () => { };
            _container.Register(handler);

            // Act
            _container.Deregister(handler);

            // Assert
            Assert.AreEqual(0, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_NonExistentNoArgsHandler_DoesNothing()
        {
            // Arrange
            Action registeredHandler = () => { };
            Action unregisteredHandler = () => { };
            _container.Register(registeredHandler);

            // Act
            _container.Deregister(unregisteredHandler);

            // Assert
            Assert.AreEqual(1, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_NoArgsHandlerNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action handler = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister(handler));
        }

        #endregion

        #region Binding Direct Deregistration Tests

        [Test]
        public void Deregister_ExistingBinding_RemovesBinding()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            var binding = _container.Register(handler);

            // Act
            _container.Deregister(binding);

            // Assert
            Assert.AreEqual(0, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_NonExistentBinding_DoesNothing()
        {
            // Arrange
            Action<TestEvent> handler = evt => { };
            _container.Register(handler);
            var unregisteredBinding = new TypedEventBinding<TestEvent>(evt => { });

            // Act
            _container.Deregister(unregisteredBinding);

            // Assert
            Assert.AreEqual(1, _container.Bindings.Count);
        }

        [Test]
        public void Deregister_BindingNull_ThrowsArgumentNullException()
        {
            // Arrange
            IEventBinding binding = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _container.Deregister(binding));
        }

        #endregion

        #region Clear All Bindings Tests

        [Test]
        public void ClearAllBindings_WithMultipleBindings_RemovesAll()
        {
            // Arrange
            _container.Register((Action<TestEvent>)(evt => { }));
            _container.Register((Action)(() => { }));
            _container.Register(new TypedEventBinding<TestEvent>(evt => { }));

            // Act
            _container.ClearAllBindings();

            // Assert
            Assert.AreEqual(0, _container.Bindings.Count);
        }

        [Test]
        public void ClearAllBindings_WithNoBindings_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _container.ClearAllBindings());
            Assert.AreEqual(0, _container.Bindings.Count);
        }

        #endregion

        #region Mixed Handler Type Tests

        [Test]
        public void Register_MixedHandlerTypes_AllRegisteredCorrectly()
        {
            // Arrange
            Action<TestEvent> typedHandler = evt => { };
            Action noArgsHandler = () => { };

            // Act
            var typedBinding = _container.Register(typedHandler);
            var noArgsBinding = _container.Register(noArgsHandler);

            // Assert
            Assert.AreEqual(2, _container.Bindings.Count);
            Assert.IsInstanceOf<TypedEventBinding<TestEvent>>(typedBinding);
            Assert.IsInstanceOf<NoArgsEventBinding>(noArgsBinding);
        }

        [Test]
        public void Deregister_MixedHandlerTypes_RemovesCorrectBinding()
        {
            // Arrange
            Action<TestEvent> typedHandler = evt => { };
            Action noArgsHandler = () => { };
            _container.Register(typedHandler);
            _container.Register(noArgsHandler);

            // Act
            _container.Deregister(typedHandler);

            // Assert
            Assert.AreEqual(1, _container.Bindings.Count);
            Assert.IsInstanceOf<NoArgsEventBinding>(_container.Bindings.First());
        }

        #endregion

        #region Priority Sorting Tests

        [Test]
        public void Register_HandlersWithDifferentPriorities_SortsCorrectly()
        {
            // Arrange & Act
            var lowPriorityBinding = _container.Register((Action<TestEvent>)(evt => { }), 1);
            var highPriorityBinding = _container.Register((Action<TestEvent>)(evt => { }), 10);
            var mediumPriorityBinding = _container.Register((Action<TestEvent>)(evt => { }), 5);

            // Assert
            var bindings = _container.Bindings.ToList();
            Assert.AreEqual(3, bindings.Count);
            Assert.AreEqual(10, bindings[0].Priority); // Highest first
            Assert.AreEqual(5, bindings[1].Priority);
            Assert.AreEqual(1, bindings[2].Priority);  // Lowest last
        }

        [Test]
        public void Register_HandlersWithSamePriority_MaintainsRegistrationOrder()
        {
            // Arrange
            Action<TestEvent> handler1 = evt => { };
            Action<TestEvent> handler2 = evt => { };
            
            // Act
            var binding1 = _container.Register(handler1, 5);
            var binding2 = _container.Register(handler2, 5);

            // Assert
            var bindings = _container.Bindings.ToList();
            Assert.AreEqual(2, bindings.Count);
            Assert.AreEqual(5, bindings[0].Priority);
            Assert.AreEqual(5, bindings[1].Priority);
            // Both should be TypedEventBinding<TestEvent>, verify they match the handlers
            var typedBinding1 = bindings[0] as TypedEventBinding<TestEvent>;
            var typedBinding2 = bindings[1] as TypedEventBinding<TestEvent>;
            Assert.IsTrue(typedBinding1.MatchesHandler(handler1) || typedBinding1.MatchesHandler(handler2));
            Assert.IsTrue(typedBinding2.MatchesHandler(handler1) || typedBinding2.MatchesHandler(handler2));
        }

        #endregion
    }
}