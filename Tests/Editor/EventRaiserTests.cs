using System;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Editor.Tests;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Editor.Tests.EditMode
{
    [TestFixture]
    public class EventRaiserTests
    {
        private EventContainer _testContainer;

        [SetUp]
        public void Setup()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            _testContainer = new EventContainer();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
        }

        #region Builder Creation Tests

        [Test]
        public void Event_WithEventData_CreatesValidBuilder()
        {
            // Arrange
            var testEvent = new TestEvent(42, "test");

            // Act
            var raiser = RaiseEvent.Event(testEvent);

            // Assert
            Assert.IsNotNull(raiser);
            Assert.IsInstanceOf<EventRaiser<TestEvent>>(raiser);
        }

        [Test]
        public void Event_WithDefaultEvent_CreatesValidBuilder()
        {
            // Act
            var raiser = RaiseEvent.Event<EmptyEvent>();

            // Assert
            Assert.IsNotNull(raiser);
            Assert.IsInstanceOf<EventRaiser<EmptyEvent>>(raiser);
        }

        #endregion

        #region Global Bus Configuration Tests

        [Test]
        public void WithGlobalBus_RaiseSync_UsesGlobalEventBus()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(100, "global");

            EventBus<TestEvent>.Register(evt => {
                handlerCalled = true;
                Assert.AreEqual(100, evt.Value);
                Assert.AreEqual("global", evt.Message);
            });

            // Act
            RaiseEvent.Event(testEvent)
                .WithGlobalBus()
                .RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void DefaultBehavior_UsesGlobalBus()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(200, "default");

            EventBus<TestEvent>.Register(evt => handlerCalled = true);

            // Act
            RaiseEvent.Event(testEvent).RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Container Configuration Tests

        [Test]
        public void WithContainer_RaiseSync_UsesSpecifiedContainer()
        {
            // Arrange
            bool containerHandlerCalled = false;
            bool globalHandlerCalled = false;
            var testEvent = new TestEvent(300, "container");

            _testContainer.Register<TestEvent>(evt => {
                containerHandlerCalled = true;
                Assert.AreEqual(300, evt.Value);
            });

            EventBus<TestEvent>.Register(evt => globalHandlerCalled = true);

            // Act
            RaiseEvent.Event(testEvent)
                .WithContainer(_testContainer)
                .RaiseSync();

            // Assert
            Assert.IsTrue(containerHandlerCalled);
            Assert.IsFalse(globalHandlerCalled);
        }

        [Test]
        public void WithContainer_Null_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                RaiseEvent.Event(new TestEvent()).WithContainer(null));
        }

        [Test]
        public void WithContainer_ThenWithGlobalBus_SwitchesToGlobalBus()
        {
            // Arrange
            bool containerHandlerCalled = false;
            bool globalHandlerCalled = false;
            var testEvent = new TestEvent(400, "switch");

            _testContainer.Register<TestEvent>(evt => containerHandlerCalled = true);
            EventBus<TestEvent>.Register(evt => globalHandlerCalled = true);

            // Act
            RaiseEvent.Event(testEvent)
                .WithContainer(_testContainer)
                .WithGlobalBus()
                .RaiseSync();

            // Assert
            Assert.IsFalse(containerHandlerCalled);
            Assert.IsTrue(globalHandlerCalled);
        }

        #endregion

        #region Polymorphic Configuration Tests

        [Test]
        public void WithPolymorphic_True_EnablesPolymorphicRaising()
        {
            // This test verifies the polymorphic flag is set, actual polymorphic behavior 
            // is tested in container tests
            
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => 
                RaiseEvent.Event(new TestEvent())
                    .WithContainer(_testContainer)
                    .WithPolymorphic(true)
                    .RaiseSync());
        }

        [Test]
        public void WithPolymorphic_False_DisablesPolymorphicRaising()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => 
                RaiseEvent.Event(new TestEvent())
                    .WithContainer(_testContainer)
                    .WithPolymorphic(false)
                    .RaiseSync());
        }

        [Test]
        public void WithPolymorphic_DefaultTrue_EnablesPolymorphicRaising()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => 
                RaiseEvent.Event(new TestEvent())
                    .WithContainer(_testContainer)
                    .WithPolymorphic()
                    .RaiseSync());
        }

        #endregion

        #region Conditional Raising Tests

        [Test]
        public void When_ConditionTrue_RaisesEvent()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = true;

            EventBus<TestEvent>.Register(evt => handlerCalled = true);

            // Act
            RaiseEvent.Event(new TestEvent())
                .When(() => condition)
                .RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public void When_ConditionFalse_DoesNotRaiseEvent()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = false;

            EventBus<TestEvent>.Register(evt => handlerCalled = true);

            // Act
            RaiseEvent.Event(new TestEvent())
                .When(() => condition)
                .RaiseSync();

            // Assert
            Assert.IsFalse(handlerCalled);
        }

        [Test]
        public void When_NullCondition_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                RaiseEvent.Event(new TestEvent()).When(null));
        }

        [Test]
        public void When_ConditionChangesAfterBuilder_UsesConditionAtRaiseTime()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = false;

            EventBus<TestEvent>.Register(evt => handlerCalled = true);

            var raiser = RaiseEvent.Event(new TestEvent())
                .When(() => condition);

            // Change condition after builder creation
            condition = true;

            // Act
            raiser.RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void FluentInterface_AllMethods_ReturnSameInstance()
        {
            // Arrange
            var testEvent = new TestEvent();

            // Act
            var raiser1 = RaiseEvent.Event(testEvent);
            var raiser2 = raiser1.WithGlobalBus();
            var raiser3 = raiser2.WithContainer(_testContainer);
            var raiser4 = raiser3.WithPolymorphic(false);
            var raiser5 = raiser4.When(() => true);

            // Assert
            Assert.AreSame(raiser1, raiser2);
            Assert.AreSame(raiser2, raiser3);
            Assert.AreSame(raiser3, raiser4);
            Assert.AreSame(raiser4, raiser5);
        }

        [Test]
        public void FluentInterface_ComplexChain_WorksCorrectly()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = true;

            _testContainer.Register<TestEvent>(evt => handlerCalled = true);

            // Act
            RaiseEvent.Event(new TestEvent(123, "fluent"))
                .WithContainer(_testContainer)
                .WithPolymorphic(false)
                .When(() => condition)
                .RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void RaiseSync_HandlerThrows_PropagatesException()
        {
            // Arrange
            EventBus<TestEvent>.Register(evt => throw new InvalidOperationException("Test exception"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                RaiseEvent.Event(new TestEvent()).RaiseSync());
            
            Assert.AreEqual("Test exception", exception.Message);
        }

        [Test]
        public void RaiseSync_NoHandlers_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
                RaiseEvent.Event(new TestEvent()).RaiseSync());
        }

        #endregion

        #region Edge Cases

        [Test]
        public void RaiseSync_MultipleConfigurations_LastConfigurationWins()
        {
            // Arrange
            bool containerHandlerCalled = false;
            bool globalHandlerCalled = false;

            _testContainer.Register<TestEvent>(evt => containerHandlerCalled = true);
            EventBus<TestEvent>.Register(evt => globalHandlerCalled = true);

            // Act - Configure container, then global, then container again
            RaiseEvent.Event(new TestEvent())
                .WithContainer(_testContainer)
                .WithGlobalBus()
                .WithContainer(_testContainer)
                .RaiseSync();

            // Assert
            Assert.IsTrue(containerHandlerCalled);
            Assert.IsFalse(globalHandlerCalled);
        }

        [Test]
        public void RaiseSync_EmptyEventWithNoArgs_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            EventBus<EmptyEvent>.Register(() => handlerCalled = true);

            // Act
            RaiseEvent.Event<EmptyEvent>().RaiseSync();

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion
    }
}