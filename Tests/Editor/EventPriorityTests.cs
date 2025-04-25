using System;
using NUnit.Framework;

namespace DGP.EventBus.Editor.Tests
{
    public class EventPriorityTests
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

        [Test]
        public void TestPriorityOrderInvocation()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register handlers with different priorities
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Low"; }, 0);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Medium"; }, 5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "High"; }, 10);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order (high to low)
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
        
        [Test]
        public void TestPriorityOrderForNoArgHandlers()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register handlers with different priorities
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "Low"; }, 0);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "Medium"; }, 5);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "High"; }, 10);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order (high to low)
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
        
        [Test]
        public void TestMixedHandlerTypes()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register both arg and no-arg handlers with different priorities
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "ArgHigh"; }, 10);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "NoArgMedium"; }, 5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "ArgLow"; }, 0);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "NoArgVeryHigh"; }, 15);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order regardless of handler type
            Assert.AreEqual("NoArgVeryHighArgHighNoArgMediumArgLow", invocationOrder);
        }
        
        [Test]
        public void TestSamePriorityInvocation()
        {
            // Create lists to track the order of invocation
            var invoked = new System.Collections.Generic.List<int>();
            
            // Register handlers with the same priority
            _eventContainer.Register<TestEvent>(_ => { invoked.Add(1); }, 5);
            _eventContainer.Register<TestEvent>(_ => { invoked.Add(2); }, 5);
            _eventContainer.Register<TestEvent>(_ => { invoked.Add(3); }, 5);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify all handlers were invoked
            Assert.AreEqual(3, invoked.Count);
            // The order of handlers with same priority should match registration order
            Assert.AreEqual(1, invoked[0]);
            Assert.AreEqual(2, invoked[1]);
            Assert.AreEqual(3, invoked[2]);
        }
        
        [Test]
        public void TestNegativePriority()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register handlers with different priorities including negative
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "VeryLow"; }, -10);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Low"; }, -5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Normal"; }, 0);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "High"; }, 5);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order
            Assert.AreEqual("HighNormalLowVeryLow", invocationOrder);
        }
        
        [Test]
        public void TestPriorityAfterDeregistration()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // We need to store the action to deregister it later
            Action<TestEvent> mediumHandler = _ => { invocationOrder += "Medium"; };
            
            // Register handlers with different priorities
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Low"; }, 0);
            _eventContainer.Register<TestEvent>(mediumHandler, 5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "High"; }, 10);
            
            // Deregister the medium priority handler
            _eventContainer.Deregister<TestEvent>(mediumHandler);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify only the remaining handlers were invoked in priority order
            Assert.AreEqual("HighLow", invocationOrder);
        }
        
        [Test]
        public void TestDynamicPriorityChanges()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register initial handlers
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "A"; }, 1);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "B"; }, 2);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual("BA", invocationOrder);
            
            // Reset tracking and add a higher priority handler
            invocationOrder = "";
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "C"; }, 3);
            
            // Raise the event again
            _eventContainer.Raise(new TestEvent());
            Assert.AreEqual("CBA", invocationOrder);
        }
        
        [Test]
        public void TestPriorityWithEventBusStatic()
        {
            // Clear all bindings before the test
            EventBus<TestEvent>.ClearAllBindings();
            
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Register handlers with different priorities using the static EventBus
            EventBus<TestEvent>.Register(_ => { invocationOrder += "Low"; }, 0);
            EventBus<TestEvent>.Register(_ => { invocationOrder += "Medium"; }, 5);
            EventBus<TestEvent>.Register(_ => { invocationOrder += "High"; }, 10);
            
            // Raise the event
            EventBus<TestEvent>.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
        
        [Test]
        public void TestPriorityWithDirectBindingRegistration()
        {
            // Create a string to track the order of invocation
            string invocationOrder = "";
            
            // Create bindings with different priorities
            var lowBinding = new EventBinding<TestEvent>(_ => { invocationOrder += "Low"; }, 0);
            var mediumBinding = new EventBinding<TestEvent>(_ => { invocationOrder += "Medium"; }, 5);
            var highBinding = new EventBinding<TestEvent>(_ => { invocationOrder += "High"; }, 10);
            
            // Register bindings directly
            _eventContainer.Register<TestEvent>(lowBinding);
            _eventContainer.Register<TestEvent>(mediumBinding);
            _eventContainer.Register<TestEvent>(highBinding);
            
            // Raise the event
            _eventContainer.Raise(new TestEvent());
            
            // Verify the handlers were invoked in priority order
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
    }
}