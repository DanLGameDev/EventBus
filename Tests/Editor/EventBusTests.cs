using System;
using NUnit.Framework;

namespace DGP.EventBus.Editor.Tests
{
    public class EventBusTests
    {
        private struct TestEvent : IEvent
        {
            public int TestValue;
        }

        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
        }

        private class MockHandler
        {
            public int InvokeCount;
            public int LastTestValue;
            private readonly Action<TestEvent> _onTestEvent;
            
            public MockHandler() {
                _onTestEvent = OnTestEvent;
                EventBus<TestEvent>.Register(_onTestEvent);
            }

            public void Deregister()
            {
                EventBus<TestEvent>.Deregister(_onTestEvent);
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
            
            public MockHandlerEmptyArgs() {
                _onTestEvent = OnTestEvent;
                EventBus<TestEvent>.Register(_onTestEvent);
            }
            
            public void Deregister()
            {
                EventBus<TestEvent>.Deregister(_onTestEvent);
            }

            private void OnTestEvent() => InvokeCount++;
        }
        
        [Test]
        public void TestArgEvent() {
            var handler = new MockHandler();
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
            Assert.AreEqual(42, handler.LastTestValue);
        }
        
        [Test]
        public void TestNoArgEvent() {
            var handler = new MockHandlerEmptyArgs();
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
        }
        
        [Test]
        public void TestDeregisterArgHandler()
        {
            var handler = new MockHandler();
            
            // First event should be received
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler.InvokeCount);
            
            // Deregister the handler
            handler.Deregister();
            
            // This event should not be received
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 100});
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
            Assert.AreEqual(42, handler.LastTestValue, "LastTestValue should not change after deregistration");
        }
        
        [Test]
        public void TestDeregisterNoArgHandler()
        {
            var handler = new MockHandlerEmptyArgs();
            
            // First event should be received
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, handler.InvokeCount);
            
            // Deregister the handler
            handler.Deregister();
            
            // This event should not be received
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
        }
        
        [Test]
        public void TestClearAllBindings()
        {
            var handler1 = new MockHandler();
            var handler2 = new MockHandlerEmptyArgs();
            
            // First event should be received by both handlers
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 42});
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);
            
            // Clear all bindings
            EventBus<TestEvent>.ClearAllBindings();
            
            // This event should not be received by any handler
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 100});
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
            EventBus<TestEvent>.Register(handler1);
            EventBus<TestEvent>.Register(handler2);
            
            // First event should be received by both handlers
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);
            
            // Deregister first handler
            EventBus<TestEvent>.Deregister(handler1);
            
            // Second event should be received only by second handler
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count, "Handler1 should not receive events after deregistration");
            Assert.AreEqual(2, handler2Count);
            
            // Deregister second handler
            EventBus<TestEvent>.Deregister(handler2);
            
            // Third event should not be received by any handler
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(2, handler2Count, "Handler2 should not receive events after deregistration");
        }
        
        [Test]
        public void TestDirectBindingRegistrationAndDeregistration()
        {
            int eventCount = 0;
            var binding = new EventBinding<TestEvent>(_ => eventCount++);
            
            // Register the binding directly
            EventBus<TestEvent>.Register(binding);
            
            // First event should be received
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, eventCount);
            
            // Deregister the binding
            EventBus<TestEvent>.Deregister(binding);
            
            // This event should not be received
            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, eventCount, "Handler should not receive events after deregistration");
        }
    }
}