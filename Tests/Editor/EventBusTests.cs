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

        private class MockHandler
        {
            private EventBinding<TestEvent> binding;
            public int InvokeCount;
            public int LastTestValue;
            
            public MockHandler() {
                binding = EventBus<TestEvent>.Register(OnTestEvent);
            }

            private void OnTestEvent(TestEvent @event) {
                InvokeCount++;
                LastTestValue = @event.TestValue;
            }
        }

        private class MockHandlerEmptyArgs
        {
            private EventBinding<TestEvent> binding;
            public int InvokeCount;
            
            public MockHandlerEmptyArgs() {
                binding = EventBus<TestEvent>.Register(OnTestEvent);
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
        
        
        // Test Cached Value
        [Test]
        public void TestCachedValueEvent()
        {
            var handler = new MockHandlerEmptyArgs();
            EventBus<TestEvent>.Raise(new TestEvent {TestValue = 42});

            TestEvent? lastValue = null;
            Action<TestEvent> callback = (e) => lastValue = e;

            EventBus<TestEvent>.Register(callback, true);

        }
    }
}
