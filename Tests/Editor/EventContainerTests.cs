using System;
using NUnit.Framework;

namespace DGP.EventBus.Editor.Tests
{
    public class EventContainerTests
    {
        private struct TestEvent : IEvent
        {
            public int TestValue;
        }
        
        private EventContainer _eventContainer;

        [SetUp]
        public void Setup()
        {
            // Create a new event container for each test to ensure test isolation
            _eventContainer = new EventContainer();
        }

        private class MockHandler
        {
            public int InvokeCount;
            public int LastTestValue;
            private readonly Action<TestEvent> _onTestEvent;
            private readonly EventContainer _container;
            
            public MockHandler(EventContainer container) {
                _container = container;
                _onTestEvent = OnTestEvent;
                _container.Register<TestEvent>(_onTestEvent);
            }

            public void Deregister()
            {
                _container.Deregister<TestEvent>(_onTestEvent);
            }

            private void OnTestEvent(TestEvent @event) {
                InvokeCount++;
                LastTestValue = @event.TestValue;
            }
        }

        private class MockHandlerEmptyArgs
        {
            public int InvokeCount;
            private readonly Action _onTestEvent;
            private readonly EventContainer _container;
            
            public MockHandlerEmptyArgs(EventContainer container) {
                _container = container;
                _onTestEvent = OnTestEvent;
                _container.Register<TestEvent>(_onTestEvent);
            }
            
            public void Deregister()
            {
                _container.Deregister<TestEvent>(_onTestEvent);
            }

            private void OnTestEvent() => InvokeCount++;
        }
        
        [Test]
        public void TestArgEvent() {
            var handler = new MockHandler(_eventContainer);
            _eventContainer.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
            Assert.AreEqual(42, handler.LastTestValue);
        }
        
        [Test]
        public void TestNoArgEvent() {
            var handler = new MockHandlerEmptyArgs(_eventContainer);
            _eventContainer.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
        }
        
        [Test]
        public void TestDeregisterArgHandler()
        {
            var handler = new MockHandler(_eventContainer);
            
            // First event should be received
            _eventContainer.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
            
            // Deregister the handler
            handler.Deregister();
            
            // This event should not be received
            _eventContainer.Raise(new TestEvent {TestValue = 100});
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
            Assert.AreEqual(42, handler.LastTestValue, "LastTestValue should not change after deregistration");
        }
        
        [Test]
        public void TestDeregisterNoArgHandler()
        {
            var handler = new MockHandlerEmptyArgs(_eventContainer);
            
            // First event should be received
            _eventContainer.Raise<TestEvent>(new TestEvent());
            Assert.AreEqual(1, handler.InvokeCount);
            
            // Deregister the handler
            handler.Deregister();
            
            // This event should not be received
            _eventContainer.Raise<TestEvent>(new TestEvent());
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
        }
        
        [Test]
        public void TestClearBindings()
        {
            var handler1 = new MockHandler(_eventContainer);
            var handler2 = new MockHandlerEmptyArgs(_eventContainer);
            
            // First event should be received by both handlers
            _eventContainer.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);
            
            // Clear bindings for TestEvent
            _eventContainer.ClearBindings<TestEvent>();
            
            // This event should not be received by any handler
            _eventContainer.Raise(new TestEvent {TestValue = 100});
            Assert.AreEqual(1, handler1.InvokeCount, "Handler1 should not receive events after clearing bindings");
            Assert.AreEqual(1, handler2.InvokeCount, "Handler2 should not receive events after clearing bindings");
        }
        
        [Test]
        public void TestClearAllBindings()
        {
            var handler1 = new MockHandler(_eventContainer);
            var handler2 = new MockHandlerEmptyArgs(_eventContainer);
            
            // First event should be received by both handlers
            _eventContainer.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);
            
            // Clear all bindings
            _eventContainer.ClearAllBindings();
            
            // This event should not be received by any handler
            _eventContainer.Raise(new TestEvent {TestValue = 100});
            Assert.AreEqual(1, handler1.InvokeCount, "Handler1 should not receive events after clearing all bindings");
            Assert.AreEqual(1, handler2.InvokeCount, "Handler2 should not receive events after clearing all bindings");
        }
        
        [Test]
        public void TestMultipleRegistrationsAndDeregistrations()
        {
            int handler1Count = 0;
            int handler2Count = 0;
            
            // Define the handlers as instance fields to ensure the same reference is used for registration and deregistration
            Action<TestEvent> handler1 = _ => handler1Count++;
            Action handler2 = () => handler2Count++;
            
            // Register both handlers
            _eventContainer.Register<TestEvent>(handler1);
            _eventContainer.Register<TestEvent>(handler2);
            
            // First event should be received by both handlers
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);
            
            // Deregister first handler
            _eventContainer.Deregister<TestEvent>(handler1);
            
            // Second event should be received only by second handler
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count, "Handler1 should not receive events after deregistration");
            Assert.AreEqual(2, handler2Count);
            
            // Deregister second handler
            _eventContainer.Deregister<TestEvent>(handler2);
            
            // Third event should not be received by any handler
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(2, handler2Count, "Handler2 should not receive events after deregistration");
        }
        
        [Test]
        public void TestDirectBindingRegistrationAndDeregistration()
        {
            int eventCount = 0;
            var binding = new EventBinding<TestEvent>(_ => eventCount++);
            
            // Register the binding directly
            _eventContainer.Register<TestEvent>(binding);
            
            // First event should be received
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual(1, eventCount);
            
            // Deregister the binding
            _eventContainer.Deregister<TestEvent>(binding);
            
            // This event should not be received
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual(1, eventCount, "Handler should not receive events after deregistration");
        }
        
        [Test]
        public void TestLastRaisedValue()
        {
            // Raise an event
            var testEvent = new TestEvent { TestValue = 42 };
            _eventContainer.Raise(testEvent);
            
            // Check that LastRaisedValue is set correctly
            var lastValue = _eventContainer.GetLastRaisedValue<TestEvent>();
            Assert.AreEqual(42, lastValue.TestValue);
            
            // Raise another event
            var newEvent = new TestEvent { TestValue = 99 };
            _eventContainer.Raise(newEvent);
            
            // Check that LastRaisedValue is updated
            lastValue = _eventContainer.GetLastRaisedValue<TestEvent>();
            Assert.AreEqual(99, lastValue.TestValue);
        }
        
        [Test]
        public void TestMultipleContainers()
        {
            // Create a second container
            var secondContainer = new EventContainer();
            
            int container1Count = 0;
            int container2Count = 0;
            
            // Register handlers with different containers
            _eventContainer.Register<TestEvent>(_ => container1Count++);
            secondContainer.Register<TestEvent>(_ => container2Count++);
            
            // Raise event on first container
            _eventContainer.Raise(new TestEvent());
            
            // Only the handler on the first container should be called
            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(0, container2Count);
            
            // Raise event on second container
            secondContainer.Raise(new TestEvent());
            
            // Only the handler on the second container should be called
            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(1, container2Count);
            
            // Each container maintains its own state
            _eventContainer.ClearAllBindings();
            
            // Raise events again
            _eventContainer.Raise(new TestEvent());
            secondContainer.Raise(new TestEvent());
            
            // Only the second container's handler should still be active
            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(2, container2Count);
        }
        
        [Test]
        public void TestRepeatLastRaisedValue()
        {
            // Raise an event before registering handlers
            _eventContainer.Raise(new TestEvent { TestValue = 42 });
            
            // Register handler with repeatLastRaisedValue = true
            int invokeCount = 0;
            int lastValue = 0;
            _eventContainer.Register<TestEvent>(e => { invokeCount++; lastValue = e.TestValue; }, true);
            
            // Handler should be called immediately with last raised value
            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(42, lastValue);
            
            // Raise another event
            _eventContainer.Raise(new TestEvent { TestValue = 99 });
            
            // Handler should be called again
            Assert.AreEqual(2, invokeCount);
            Assert.AreEqual(99, lastValue);
        }
    }
}