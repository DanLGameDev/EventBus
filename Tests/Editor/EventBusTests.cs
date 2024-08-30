using DGP.EventBus.Runtime;
using NUnit.Framework;

namespace Tests.Editor
{
    public class EventBusTests
    {
        public struct TestEvent : IEvent
        {
            public int TestValue;
        }

        public class MockHandler
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
        
        public class MockHandlerEmptyArgs
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
    }
}
