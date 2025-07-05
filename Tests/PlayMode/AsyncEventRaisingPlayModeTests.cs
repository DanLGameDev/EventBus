using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif
using DGP.EventBus;
using DGP.EventBus.Bindings;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.PlayMode
{
    public class AsyncEventRaisingPlayModeTests
    {
        [SetUp]
        public void Setup()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<PriorityTestEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            EventBus<PriorityTestEvent>.ClearAllBindings();
        }

#if UNITASK_SUPPORT
        #region Basic Async Event Raising Tests

        [UnityTest]
        public IEnumerator Container_RaiseAsync_WithAsyncHandler_CallsHandler()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool handlerCalled = false;
            TestEvent receivedEvent = default;
            var testEvent = new TestEvent(42, "async");

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
                receivedEvent = evt;
            }));

            // Act
            var task = container.RaiseAsync(testEvent).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(42, receivedEvent.Value);
            Assert.AreEqual("async", receivedEvent.Message);
        }

        [UnityTest]
        public IEnumerator Container_RaiseAsync_WithNoArgsAsyncHandler_CallsHandler()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool handlerCalled = false;

            container.Register((Func<UniTask>)(async () => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
            }));

            // Act
            var task = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [UnityTest]
        public IEnumerator EventBusT_RaiseAsync_WithAsyncHandler_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(99, "eventbus");

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
                Assert.AreEqual(99, evt.Value);
            });

            // Act
            var task = EventBus<TestEvent>.RaiseAsync(testEvent).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [UnityTest]
        public IEnumerator EventContainer_RaiseAsync_WithAsyncHandler_CallsHandler()
        {
            // Arrange
            var container = new EventContainer();
            bool handlerCalled = false;
            var testEvent = new TestEvent(77, "container");

            container.Register<TestEvent>(async evt => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
                Assert.AreEqual(77, evt.Value);
            });

            // Act
            var task = container.RaiseAsync(testEvent).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Mixed Sync/Async Handler Tests

        [UnityTest]
        public IEnumerator Container_RaiseAsync_MixedSyncAsyncHandlers_CallsBoth()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool syncHandlerCalled = false;
            bool asyncHandlerCalled = false;
            var testEvent = new TestEvent(55, "mixed");

            container.Register((Action<TestEvent>)(evt => 
            {
                syncHandlerCalled = true;
                Assert.AreEqual(55, evt.Value);
            }));

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(50);
                asyncHandlerCalled = true;
                Assert.AreEqual(55, evt.Value);
            }));

            // Act
            var task = container.RaiseAsync(testEvent).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(syncHandlerCalled);
            Assert.IsTrue(asyncHandlerCalled);
        }

        [UnityTest]
        public IEnumerator EventBusT_RaiseAsync_MixedSyncAsyncHandlers_CallsBoth()
        {
            // Arrange
            bool syncHandlerCalled = false;
            bool asyncHandlerCalled = false;
            var testEvent = new TestEvent(33, "mixed");

            EventBus<TestEvent>.Register(evt => 
            {
                syncHandlerCalled = true;
                Assert.AreEqual(33, evt.Value);
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(50);
                asyncHandlerCalled = true;
                Assert.AreEqual(33, evt.Value);
            });

            // Act
            var task = EventBus<TestEvent>.RaiseAsync(testEvent).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(syncHandlerCalled);
            Assert.IsTrue(asyncHandlerCalled);
        }

        #endregion

        #region Sequential vs Concurrent Execution Tests

        [UnityTest]
        public IEnumerator Container_RaiseSequentialAsync_ExecutesInOrder()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            var startTimes = new List<float>();

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler1");
            }), 10);

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler2");
            }), 5);

            // Act
            var task = container.RaiseSequentialAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("Handler1", executionOrder[0]);
            Assert.AreEqual("Handler2", executionOrder[1]);
            
            // Verify sequential execution timing
            Assert.IsTrue(startTimes[1] >= startTimes[0] + 0.08f, "Second handler should start after first completes");
        }

        [UnityTest]
        public IEnumerator Container_RaiseConcurrentAsync_ExecutesConcurrently()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            var startTimes = new List<float>();

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler1");
            }), 10);

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler2");
            }), 5);

            // Act
            var task = container.RaiseConcurrentAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            
            // In concurrent execution, both should start at roughly the same time
            Assert.IsTrue(Mathf.Abs(startTimes[1] - startTimes[0]) < 0.05f, "Handlers should start concurrently");
        }

        [UnityTest]
        public IEnumerator EventBusT_RaiseConcurrentAsync_ExecutesConcurrently()
        {
            // Arrange
            var startTimes = new List<float>();
            var completionTimes = new List<float>();

            EventBus<TestEvent>.Register(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                completionTimes.Add(Time.time);
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                completionTimes.Add(Time.time);
            });

            // Act
            var task = EventBus<TestEvent>.RaiseConcurrentAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, startTimes.Count);
            Assert.AreEqual(2, completionTimes.Count);
            Assert.IsTrue(Mathf.Abs(startTimes[1] - startTimes[0]) < 0.05f, "Should start concurrently");
        }

        #endregion

        #region Async Priority Execution Tests

        [UnityTest]
        public IEnumerator Container_RaiseSequentialAsync_RespectsPriority()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("Low");
            }), 1);

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("High");
            }), 10);

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("Medium");
            }), 5);

            // Act
            var task = container.RaiseSequentialAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [UnityTest]
        public IEnumerator EventBusT_RaiseSequentialAsync_MixedPriorities_RespectsOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            EventBus<TestEvent>.Register(evt => executionOrder.Add("SyncMedium"), 5);
            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("AsyncHigh");
            }, 10);
            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("AsyncLow");
            }, 1);

            // Act
            var task = EventBus<TestEvent>.RaiseSequentialAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("AsyncHigh", executionOrder[0]);
            Assert.AreEqual("SyncMedium", executionOrder[1]);
            Assert.AreEqual("AsyncLow", executionOrder[2]);
        }

        #endregion

        #region Async Exception Handling Tests

        [UnityTest]
        public IEnumerator Container_RaiseAsync_AsyncHandlerThrows_PropagatesException()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                throw new InvalidOperationException("Async exception");
            }));

            // Act
            var task = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted);
            Assert.IsInstanceOf<InvalidOperationException>(task.Exception.InnerException);
            Assert.AreEqual("Async exception", task.Exception.InnerException.Message);
        }

        [UnityTest]
        public IEnumerator Container_RaiseSequentialAsync_FirstAsyncHandlerThrows_SubsequentNotCalled()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool secondHandlerCalled = false;

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                throw new InvalidOperationException("First async exception");
            }), 10);

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                secondHandlerCalled = true;
            }), 5);

            // Act
            var task = container.RaiseSequentialAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(secondHandlerCalled, "Second handler should not be called after first throws");
        }

        [UnityTest]
        public IEnumerator Container_RaiseConcurrentAsync_OneHandlerThrows_StillPropagatesException()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            bool normalHandlerCompleted = false;

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(50);
                throw new InvalidOperationException("Concurrent exception");
            }));

            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(100);
                normalHandlerCompleted = true;
            }));

            // Act
            var task = container.RaiseConcurrentAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted);
            // In concurrent execution, the normal handler might complete or not depending on timing
        }

        #endregion

        #region Async Deferred Removal Tests

        [UnityTest]
        public IEnumerator Container_DeregisterDuringAsyncRaise_DefersRemoval()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> handlerToRemove = evt => executionOrder.Add("HandlerToRemove");
            
            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("AsyncHandler");
                container.Deregister(handlerToRemove);
                Assert.AreEqual(2, container.Bindings.Count, "Should still have both bindings during async raise");
            }), 10);

            container.Register(handlerToRemove, 5);

            // Act
            var task = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("AsyncHandler", executionOrder[0]);
            Assert.AreEqual("HandlerToRemove", executionOrder[1]);
            Assert.AreEqual(1, container.Bindings.Count, "Handler should be removed after async raise completes");
        }

        [UnityTest]
        public IEnumerator EventBusT_DeregisterDuringAsyncRaise_DefersRemoval()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            Action<TestEvent> handlerToRemove = evt => executionOrder.Add("HandlerToRemove");
            
            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("AsyncHandler");
                EventBus<TestEvent>.Deregister(handlerToRemove);
                Assert.AreEqual(2, EventBus<TestEvent>.BindingsContainer.Count);
            }, 10);

            EventBus<TestEvent>.Register(handlerToRemove, 5);

            // Suppress expected UniTask exceptions
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            // Act
            var task = EventBus<TestEvent>.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Restore log assertions
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("AsyncHandler", executionOrder[0]);
            Assert.AreEqual("HandlerToRemove", executionOrder[1]);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingsContainer.Count);
        }

        #endregion

        #region Async Registration During Raising Tests

        [UnityTest]
        public IEnumerator Container_RegisterDuringAsyncRaise_HandlerNotCalledInCurrentRaise()
        {
            // Arrange
            var container = new EventBindingContainer<TestEvent>();
            var executionOrder = new List<string>();
            
            Action<TestEvent> newHandler = evt => executionOrder.Add("NewHandler");
            
            container.Register((Func<TestEvent, UniTask>)(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("AsyncHandler");
                container.Register(newHandler);
                Assert.AreEqual(2, container.Bindings.Count);
            }));

            // Act
            var task = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(1, executionOrder.Count, "New handler should not be called in current raise");
            Assert.AreEqual("AsyncHandler", executionOrder[0]);
            Assert.AreEqual(2, container.Bindings.Count);
            
            // Verify new handler called in subsequent raise
            var task2 = container.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("AsyncHandler", executionOrder[1]);
            Assert.AreEqual("NewHandler", executionOrder[2]);
        }

        #endregion
#endif
    }
}