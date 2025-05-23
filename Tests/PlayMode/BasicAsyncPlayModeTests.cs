using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Editor.Tests.PlayMode
{
    public class BasicAsyncPlayModeTests
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

#if UNITASK_SUPPORT
        [UnityTest]
        public IEnumerator TestAsyncHandler()
        {
            int invokeCount = 0;
            int receivedValue = 0;

            // Register an async handler with explicit delegate type
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async (evt) => {
                await UniTask.Delay(100); // Simulate async work
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously
            UniTask uniTask = EventBus<TestEvent>.RaiseAsync(new TestEvent { TestValue = 42 });
            var task = uniTask.AsTask();
            
            // Wait for the task to complete
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Check for errors
            if (task.IsFaulted)
                throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(42, receivedValue);
        }

        [UnityTest]
        public IEnumerator TestAsyncNoArgHandler()
        {
            int invokeCount = 0;

            // Register an async handler with no args with explicit delegate type
            EventBus<TestEvent>.Register((Func<UniTask>)(async () => {
                await UniTask.Delay(100); // Simulate async work
                invokeCount++;
            }));

            // Raise the event asynchronously
            UniTask uniTask = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            var task = uniTask.AsTask();
            
            // Wait for the task to complete
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Check for errors
            if (task.IsFaulted)
                throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
        }

        [UnityTest]
        public IEnumerator TestMixedSyncAndAsyncHandlers()
        {
            int syncCount = 0;
            int asyncCount = 0;

            // Register both sync and async handlers
            EventBus<TestEvent>.Register((Action<TestEvent>)(_ => syncCount++));
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(100);
                asyncCount++;
            }));

            // Raise the event asynchronously
            UniTask uniTask = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            var task = uniTask.AsTask();
            
            // Wait for the task to complete
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Check for errors
            if (task.IsFaulted)
                throw task.Exception;

            // Verify both handlers were called
            Assert.AreEqual(1, syncCount);
            Assert.AreEqual(1, asyncCount);
        }

        [UnityTest]
        public IEnumerator TestAsyncHandlerDeregistration()
        {
            int invokeCount = 0;
            
            // Define and register the async handler
            Func<TestEvent, UniTask> handler = async evt => {
                await UniTask.Delay(50);
                invokeCount++;
            };
            
            EventBus<TestEvent>.Register(handler);
            
            // Raise once - should be handled
            UniTask uniTask1 = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            var task1 = uniTask1.AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            
            Assert.AreEqual(1, invokeCount);
            
            // Deregister
            EventBus<TestEvent>.Deregister(handler);
            
            // Raise again - should not be handled
            UniTask uniTask2 = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            var task2 = uniTask2.AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            
            Assert.AreEqual(1, invokeCount, "Handler should not be called after deregistration");
        }

        [UnityTest]
        public IEnumerator TestEventContainerAsyncHandler()
        {
            var container = new EventContainer();
            int invokeCount = 0;
            int receivedValue = 0;

            // Register an async handler
            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async (evt) => {
                await UniTask.Delay(100); // Simulate async work
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously
            UniTask uniTask = container.RaiseAsync(new TestEvent { TestValue = 42 });
            var task = uniTask.AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(42, receivedValue);
        }
#endif
    }
}