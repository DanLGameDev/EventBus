using System;
using NUnit.Framework;

namespace DGP.EventBus.Editor.Tests
{
    [TestFixture] 
    public class DelegateDeregistrationTests
    {
        private class TestHandler
        {
            public int CallCount { get; private set; }
            public string InstanceId { get; }

            public TestHandler(string instanceId)
            {
                InstanceId = instanceId;
            }

            public void HandleEvent(TestEvent evt)
            {
                CallCount++;
            }
        }

        [SetUp]
        public void Setup()
        {
            EventBus<TestEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<TestEvent>.ClearAllBindings();
        }

        [Test]
        public void Deregister_SameMethodNewDelegateReference_ShouldRemoveHandler()
        {
            // Arrange
            var handler = new TestHandler("test");
            
            // Register with one delegate reference
            EventBus<TestEvent>.Register(handler.HandleEvent);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);

            // Act - Deregister with a NEW delegate reference to the same method
            EventBus<TestEvent>.Deregister(handler.HandleEvent);

            // Assert - This should pass after we fix the EventBus
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count, 
                "Handler should be removed even with new delegate reference to same method");
        }

        [Test]
        public void Deregister_SameMethodDifferentInstance_ShouldNotRemoveOtherHandler()
        {
            // Arrange
            var handler1 = new TestHandler("handler1");
            var handler2 = new TestHandler("handler2");
            
            EventBus<TestEvent>.Register(handler1.HandleEvent);
            EventBus<TestEvent>.Register(handler2.HandleEvent);
            Assert.AreEqual(2, EventBus<TestEvent>.BindingsContainer.Count);

            // Act - Deregister handler1's method
            EventBus<TestEvent>.Deregister(handler1.HandleEvent);

            // Assert - Should only remove handler1, not handler2
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
            
            // Verify handler2 still works
            EventBus<TestEvent>.Raise(new TestEvent(1, "test"));
            Assert.AreEqual(0, handler1.CallCount);
            Assert.AreEqual(1, handler2.CallCount);
        }

        [Test]
        public void Deregister_Unity_MonoBehaviour_Pattern_ShouldWork()
        {
            // This simulates the exact pattern used in Unity MonoBehaviours
            // where OnEnable/OnDisable create new delegate references each time
            
            // Arrange
            var handler = new TestHandler("unity-like");
            
            // Simulate OnEnable - register handler
            Action<TestEvent> onEnableDelegate = handler.HandleEvent;
            EventBus<TestEvent>.Register(onEnableDelegate);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);

            // Simulate OnDisable - create new delegate reference (this is what Unity does)
            Action<TestEvent> onDisableDelegate = handler.HandleEvent;
            
            // These should be different references but same method
            Assert.AreNotSame(onEnableDelegate, onDisableDelegate, 
                "Delegates should be different references");
            Assert.AreEqual(onEnableDelegate.Method, onDisableDelegate.Method, 
                "But should point to same method");
            Assert.AreSame(onEnableDelegate.Target, onDisableDelegate.Target, 
                "And same target instance");

            // Act - Deregister using the new delegate reference
            EventBus<TestEvent>.Deregister(onDisableDelegate);

            // Assert - This should pass after we fix the EventBus
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count,
                "Handler should be removed even with different delegate reference");
        }

        [Test]
        public void Deregister_MultipleRegistrations_SameMethodNewReferences_ShowsDuplicatePrevention()
        {
            // Arrange
            var handler = new TestHandler("multi");
    
            // Register the same method multiple times
            EventBus<TestEvent>.Register(handler.HandleEvent);
            EventBus<TestEvent>.Register(handler.HandleEvent);  // Should return existing
            EventBus<TestEvent>.Register(handler.HandleEvent);  // Should return existing
    
            // Assert - Should only have 1 binding due to duplicate prevention
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count,
                "EventBus should prevent duplicate registrations of same method");

            // Act - Deregister
            EventBus<TestEvent>.Deregister(handler.HandleEvent);

            // Assert - Should remove the one binding
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count);
        }

        [Test]
        public void Register_CachedDelegateReference_DeregistrationWorks()
        {
            // This test shows the current workaround works
            
            // Arrange
            var handler = new TestHandler("cached");
            Action<TestEvent> cachedDelegate = handler.HandleEvent;
            
            // Register with cached reference
            EventBus<TestEvent>.Register(cachedDelegate);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);

            // Act - Deregister with same cached reference
            EventBus<TestEvent>.Deregister(cachedDelegate);

            // Assert - This should pass even before the fix
            Assert.AreEqual(0, EventBus<TestEvent>.BindingsContainer.Count,
                "Cached delegate reference deregistration should work");
        }
    }
}