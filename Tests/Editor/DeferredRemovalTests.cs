using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DGP.EventBus.Editor.Tests
{
    [TestFixture]
    public class DeferredRemovalTests
    {
        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            EventBus<TestEvent>.ClearAllBindings();
        }

        #region Deferred Removal During Event Raising Tests

        [Test]
        public void Deregister_DuringEventRaising_DefersRemovalUntilComplete()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> handlerToRemove = evt => 
            {
                executionOrder.Add("HandlerToRemove");
            };
            
            Action<TestEvent> handlerThatRemoves = evt => 
            {
                executionOrder.Add("HandlerThatRemoves");
                container.Deregister(handlerToRemove);
                // Binding count should still include the handler being removed during raising
                Assert.AreEqual(3, container.Bindings.Count, "Handler should not be removed immediately during raising");
            };
            
            Action<TestEvent> handlerAfter = evt => 
            {
                executionOrder.Add("HandlerAfter");
                // Handler should still be in the list during event raising
                Assert.AreEqual(3, container.Bindings.Count, "Handler should still be in list during raising");
            };

            // Register handlers with priorities to control execution order
            container.Register(handlerToRemove, 10);      // Will execute first
            container.Register(handlerThatRemoves, 5);    // Will execute second and remove first handler
            container.Register(handlerAfter, 1);          // Will execute third

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(3, executionOrder.Count, "All handlers should have executed");
            Assert.AreEqual("HandlerToRemove", executionOrder[0]);
            Assert.AreEqual("HandlerThatRemoves", executionOrder[1]);
            Assert.AreEqual("HandlerAfter", executionOrder[2]);
            
            // After event raising completes, the handler should be removed
            Assert.AreEqual(2, container.Bindings.Count, "Handler should be removed after event raising completes");
        }

        [Test]
        public void Deregister_SelfDuringEventRaising_DefersRemovalUntilComplete()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> selfRemovingHandler = null;
            selfRemovingHandler = evt => 
            {
                executionOrder.Add("SelfRemovingHandler");
                container.Deregister(selfRemovingHandler);
                // Should still be in bindings during raising
                Assert.AreEqual(2, container.Bindings.Count);
            };
            
            Action<TestEvent> otherHandler = evt => 
            {
                executionOrder.Add("OtherHandler");
                // Self-removing handler should still be in bindings
                Assert.AreEqual(2, container.Bindings.Count);
            };

            container.Register(selfRemovingHandler, 10);
            container.Register(otherHandler, 5);

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("SelfRemovingHandler", executionOrder[0]);
            Assert.AreEqual("OtherHandler", executionOrder[1]);
            Assert.AreEqual(1, container.Bindings.Count, "Self-removing handler should be removed after raising");
        }

        [Test]
        public void EventBusT_Deregister_DuringEventRaising_DefersRemoval()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            Action<TestEvent> handlerToRemove = evt => executionOrder.Add("HandlerToRemove");
            Action<TestEvent> handlerThatRemoves = evt => 
            {
                executionOrder.Add("HandlerThatRemoves");
                EventBus<TestEvent>.Deregister(handlerToRemove);
                Assert.AreEqual(2, EventBus<TestEvent>.BindingsContainer.Count);
            };

            EventBus<TestEvent>.Register(handlerToRemove, 10);
            EventBus<TestEvent>.Register(handlerThatRemoves, 5);

            // Act
            EventBus<TestEvent>.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("HandlerToRemove", executionOrder[0]);
            Assert.AreEqual("HandlerThatRemoves", executionOrder[1]);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void EventContainer_Deregister_DuringEventRaising_DefersRemoval()
        {
            // Arrange
            var container = new EventContainer();
            var executionOrder = new List<string>();
            
            Action<TestEvent> handlerToRemove = evt => executionOrder.Add("HandlerToRemove");
            Action<TestEvent> handlerThatRemoves = evt => 
            {
                executionOrder.Add("HandlerThatRemoves");
                container.Deregister<TestEvent>(handlerToRemove);
            };

            container.Register<TestEvent>(handlerToRemove, 10);
            container.Register<TestEvent>(handlerThatRemoves, 5);

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("HandlerToRemove", executionOrder[0]);
            Assert.AreEqual("HandlerThatRemoves", executionOrder[1]);
            
            // Verify the handler was actually removed by raising again
            executionOrder.Clear();
            container.Raise(new TestEvent());
            Assert.AreEqual(1, executionOrder.Count);
            Assert.AreEqual("HandlerThatRemoves", executionOrder[0]);
        }

        [Test]
        public void ClearAllBindings_DuringEventRaising_ClearsImmediately()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> handler1 = evt => executionOrder.Add("Handler1");
            Action<TestEvent> clearHandler = evt => 
            {
                executionOrder.Add("ClearHandler");
                container.ClearAllBindings();
                Assert.AreEqual(0, container.Bindings.Count, "ClearAllBindings should clear immediately");
            };
            Action<TestEvent> handler3 = evt => executionOrder.Add("Handler3");

            container.Register(handler1, 10);
            container.Register(clearHandler, 5);
            container.Register(handler3, 1);

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(2, executionOrder.Count, "Handler3 should not execute after ClearAllBindings");
            Assert.AreEqual("Handler1", executionOrder[0]);
            Assert.AreEqual("ClearHandler", executionOrder[1]);
            Assert.AreEqual(0, container.Bindings.Count);
        }

        #endregion

        #region Registration During Event Raising Tests

        [Test]
        public void Register_DuringEventRaising_HandlerNotCalledInCurrentRaise()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> newHandler = evt => executionOrder.Add("NewHandler");
            
            Action<TestEvent> handlerThatRegisters = evt => 
            {
                executionOrder.Add("HandlerThatRegisters");
                container.Register(newHandler);
                Assert.AreEqual(2, container.Bindings.Count);
            };

            container.Register(handlerThatRegisters, 5);

            // Act
            container.Raise(new TestEvent());

            // Assert
            Assert.AreEqual(1, executionOrder.Count, "New handler should not be called in current raise");
            Assert.AreEqual("HandlerThatRegisters", executionOrder[0]);
            Assert.AreEqual(2, container.Bindings.Count);
            
            // Verify new handler is called in subsequent raises
            container.Raise(new TestEvent());
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("HandlerThatRegisters", executionOrder[1]); // Priority 5 (higher)
            Assert.AreEqual("NewHandler", executionOrder[2]);           // Priority 0 (lower)
        }

        #endregion
    }
}