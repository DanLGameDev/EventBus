using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace DGP.EventBus.Editor.Tests.PlayMode
{
    public class MixedHandlerScenarioTests
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

        [UnityTest]
        public IEnumerator TestMixedSyncAndAsyncHandlers()
        {
            bool syncHandlerCalled = false;
            bool uniTaskHandlerCalled = false;
            var callOrder = new List<string>();

            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => {
                syncHandlerCalled = true;
                callOrder.Add("Sync");
            }), 10);

            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async evt => {
                await UniTask.Yield();
                uniTaskHandlerCalled = true;
                callOrder.Add("UniTask");
            }), 1);

            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.IsTrue(syncHandlerCalled, "Sync handler should be called");
            Assert.IsTrue(uniTaskHandlerCalled, "UniTask handler should be called");
            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual("Sync", callOrder[0]);
            Assert.AreEqual("UniTask", callOrder[1]);
        }

        [UnityTest]
        public IEnumerator TestAsyncRaiseWithMixedNoArgHandlers()
        {
            bool syncNoArgCalled = false;
            bool uniTaskNoArgCalled = false;
            var callOrder = new List<string>();

            EventBus<TestEvent>.Register((Action)(() => {
                syncNoArgCalled = true;
                callOrder.Add("SyncNoArg");
            }), 15);

            EventBus<TestEvent>.Register((Func<UniTask>)(async () => {
                await UniTask.Delay(50);
                uniTaskNoArgCalled = true;
                callOrder.Add("UniTaskNoArg");
            }), 5);

            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.IsTrue(syncNoArgCalled, "Sync no-arg handler should be called");
            Assert.IsTrue(uniTaskNoArgCalled, "UniTask no-arg handler should be called");
            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual("SyncNoArg", callOrder[0]);
            Assert.AreEqual("UniTaskNoArg", callOrder[1]);
        }

        [UnityTest]
        public IEnumerator TestEventContainerAsyncRaiseWithMixedHandlers()
        {
            var container = new EventContainer();
            bool syncCalled = false;
            bool asyncCalled = false;
            var callOrder = new List<string>();

            container.Register<TestEvent>((Action<TestEvent>)(evt => {
                syncCalled = true;
                callOrder.Add("Sync");
            }), 10);

            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async evt => {
                await UniTask.Delay(50);
                asyncCalled = true;
                callOrder.Add("Async");
            }), 5);

            var task = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.IsTrue(syncCalled, "Sync handler should be called");
            Assert.IsTrue(asyncCalled, "Async handler should be called");
            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual("Sync", callOrder[0]);
            Assert.AreEqual("Async", callOrder[1]);
        }

        [UnityTest]
        public IEnumerator TestMixedHandlerDeregistrationAndReplacement()
        {
            bool syncCalled = false;
            bool asyncCalled = false;
            bool newHandlerCalled = false;

            Action<TestEvent> syncHandler = evt => syncCalled = true;
            Func<TestEvent, UniTask> asyncHandler = async evt => {
                await UniTask.Yield();
                asyncCalled = true;
            };

            EventBus<TestEvent>.Register(syncHandler, 10);
            EventBus<TestEvent>.Register(asyncHandler, 5);

            var task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.IsTrue(syncCalled, "Sync handler should be called initially");

            syncCalled = false;
            asyncCalled = false;
            EventBus<TestEvent>.Deregister(syncHandler);

            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => newHandlerCalled = true), 15);

            var task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;

            Assert.IsFalse(syncCalled, "Original sync handler should not be called");
            Assert.IsTrue(asyncCalled, "Async handler should be called");
            Assert.IsTrue(newHandlerCalled, "New handler should be called");
        }

        [UnityTest]
        public IEnumerator TestEmptyEventWithMixedHandlers()
        {
            bool syncCalled = false;
            bool asyncCalled = false;

            EventBus<TestEvent>.Register((Action<TestEvent>)(evt => {
                syncCalled = true;
                Assert.AreEqual(0, evt.TestValue, "Default struct value should be 0");
            }));

            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async evt => {
                await UniTask.Yield();
                asyncCalled = true;
                Assert.AreEqual(0, evt.TestValue, "Default struct value should be 0");
            }));

            var task = EventBus<TestEvent>.RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.IsTrue(syncCalled, "Sync handler should handle default event");
            Assert.IsTrue(asyncCalled, "Async handler should handle default event");
        }

        [UnityTest]
        public IEnumerator TestConcurrentExecution()
        {
            var executionOrder = new List<string>();

            EventBus<TestEvent>.Register(async (TestEvent evt) => {
                await UniTask.Delay(100);
                executionOrder.Add("Slow");
            });
            
            EventBus<TestEvent>.Register(async (TestEvent evt) => {
                await UniTask.Delay(50);
                executionOrder.Add("Fast");
            });

            var task = EventBus<TestEvent>.RaiseConcurrentAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("Fast", executionOrder[0]);
            Assert.AreEqual("Slow", executionOrder[1]);
        }
    }
}
