using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        
        [UnityTest]
        public IEnumerator TestArgEvent() {
            var handler = new MockHandler();
            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 42}).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual(1, handler.InvokeCount);
            Assert.AreEqual(42, handler.LastTestValue);
        }
        
        [UnityTest]
        public IEnumerator TestNoArgEvent() {
            var handler = new MockHandlerEmptyArgs();
            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 42}).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual(1, handler.InvokeCount);
        }
        
        [UnityTest]
        public IEnumerator TestDeregisterArgHandler()
        {
            var handler = new MockHandler();
            
            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 42}).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler.InvokeCount);
            
            handler.Deregister();
            
            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 100}).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
            Assert.AreEqual(42, handler.LastTestValue, "LastTestValue should not change after deregistration");
        }
        
        [UnityTest]
        public IEnumerator TestDeregisterNoArgHandler()
        {
            var handler = new MockHandlerEmptyArgs();
            
            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler.InvokeCount);
            
            handler.Deregister();
            
            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
        }
        
        [UnityTest]
        public IEnumerator TestClearAllBindings()
        {
            var handler1 = new MockHandler();
            var handler2 = new MockHandlerEmptyArgs();
            
            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 42}).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);
            
            EventBus<TestEvent>.ClearAllBindings();
            
            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent {TestValue = 100}).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler1.InvokeCount, "Handler1 should not receive events after clearing all bindings");
            Assert.AreEqual(1, handler2.InvokeCount, "Handler2 should not receive events after clearing all bindings");
        }
        
        [UnityTest]
        public IEnumerator TestMultipleRegistrationsAndDeregistrations()
        {
            int handler1Count = 0;
            int handler2Count = 0;
            
            Action<TestEvent> handler1 = _ => handler1Count++;
            Action handler2 = () => handler2Count++;
            
            EventBus<TestEvent>.Register(handler1);
            EventBus<TestEvent>.Register(handler2);
            
            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);
            
            EventBus<TestEvent>.Deregister(handler1);
            
            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler1Count, "Handler1 should not receive events after deregistration");
            Assert.AreEqual(2, handler2Count);
            
            EventBus<TestEvent>.Deregister(handler2);
            
            var task3 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task3.IsCompleted);
            if (task3.IsFaulted) throw task3.Exception;
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(2, handler2Count, "Handler2 should not receive events after deregistration");
        }
        
        [UnityTest]
        public IEnumerator TestDirectBindingRegistrationAndDeregistration()
        {
            int eventCount = 0;
            var binding = new EventBinding<TestEvent>(_ => eventCount++);
            
            EventBus<TestEvent>.Register(binding);
            
            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, eventCount);
            
            EventBus<TestEvent>.Deregister(binding);
            
            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, eventCount, "Handler should not receive events after deregistration");
        }

        [UnityTest]
        public IEnumerator TestAsyncHandler()
        {
            int asyncCount = 0;
            EventBus<TestEvent>.Register(async (TestEvent evt) => {
                await UniTask.Delay(50);
                asyncCount++;
            });

            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual(1, asyncCount);
        }

        [UnityTest]
        public IEnumerator TestConcurrentRaise()
        {
            var results = new System.Collections.Generic.List<int>();
            
            EventBus<TestEvent>.Register(async (TestEvent evt) => {
                await UniTask.Delay(100);
                results.Add(1);
            });
            
            EventBus<TestEvent>.Register(async (TestEvent evt) => {
                await UniTask.Delay(50);
                results.Add(2);
            });

            var task = EventBus<TestEvent>.RaiseConcurrentAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual(2, results.Count);
            // In concurrent mode, handler 2 (50ms delay) should complete before handler 1 (100ms delay)
            Assert.AreEqual(2, results[0]);
            Assert.AreEqual(1, results[1]);
        }
    }
}