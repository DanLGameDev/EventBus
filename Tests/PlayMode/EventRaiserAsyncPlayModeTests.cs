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
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.PlayMode
{
    public class EventRaiserAsyncPlayModeTests
    {
        private EventContainer _testContainer;

        [SetUp]
        public void Setup()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
            _testContainer = new EventContainer();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<EmptyEvent>.ClearAllBindings();
        }

#if UNITASK_SUPPORT
        #region Basic Async Tests

        [UnityTest]
        public IEnumerator RaiseAsync_WithGlobalBus_CallsAsyncHandler()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(42, "async");

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
                Assert.AreEqual(42, evt.Value);
                Assert.AreEqual("async", evt.Message);
            });

            // Act
            var task = EventRaise.Event(testEvent).RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [UnityTest]
        public IEnumerator RaiseAsync_WithContainer_CallsAsyncHandler()
        {
            // Arrange
            bool handlerCalled = false;
            var testEvent = new TestEvent(99, "container");

            _testContainer.Register<TestEvent>(async evt => 
            {
                await UniTask.Delay(50);
                handlerCalled = true;
                Assert.AreEqual(99, evt.Value);
            });

            // Act
            var task = EventRaise.Event(testEvent)
                .WithContainer(_testContainer)
                .RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [UnityTest]
        public IEnumerator RaiseSequentialAsync_WithMixedHandlers_CallsBoth()
        {
            // Arrange
            bool syncHandlerCalled = false;
            bool asyncHandlerCalled = false;
            var testEvent = new TestEvent(77, "mixed");

            EventBus<TestEvent>.Register(evt => 
            {
                syncHandlerCalled = true;
                Assert.AreEqual(77, evt.Value);
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(50);
                asyncHandlerCalled = true;
                Assert.AreEqual(77, evt.Value);
            });

            // Act
            var task = EventRaise.Event(testEvent).RaiseSequentialAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(syncHandlerCalled);
            Assert.IsTrue(asyncHandlerCalled);
        }

        [UnityTest]
        public IEnumerator RaiseConcurrentAsync_ExecutesConcurrently()
        {
            // Arrange
            var startTimes = new List<float>();
            var testEvent = new TestEvent(55, "concurrent");

            EventBus<TestEvent>.Register(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
            });

            // Act
            var task = EventRaise.Event(testEvent).RaiseConcurrentAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, startTimes.Count);
            Assert.IsTrue(Mathf.Abs(startTimes[1] - startTimes[0]) < 0.05f, "Should start concurrently");
        }

        #endregion

        #region Conditional Async Tests

        [UnityTest]
        public IEnumerator RaiseAsync_ConditionFalse_DoesNotCallHandler()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = false;

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                handlerCalled = true;
            });

            // Act
            var task = EventRaise.Event(new TestEvent())
                .When(() => condition)
                .RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsFalse(handlerCalled);
        }

        [UnityTest]
        public IEnumerator RaiseAsync_ConditionTrue_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;
            bool condition = true;

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                handlerCalled = true;
            });

            // Act
            var task = EventRaise.Event(new TestEvent())
                .When(() => condition)
                .RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion

        #region Priority and Execution Order Tests

        [UnityTest]
        public IEnumerator RaiseSequentialAsync_WithPriorities_ExecutesInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("Low");
            }, 1);

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("High");
            }, 10);

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                executionOrder.Add("Medium");
            }, 5);

            // Act
            var task = EventRaise.Event(new TestEvent()).RaiseSequentialAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [UnityTest]
        public IEnumerator RaiseSequentialAsync_WithContainer_ExecutesSequentially()
        {
            // Arrange
            var executionOrder = new List<string>();
            var startTimes = new List<float>();

            _testContainer.Register<TestEvent>(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler1");
            }, 10);

            _testContainer.Register<TestEvent>(async evt => 
            {
                startTimes.Add(Time.time);
                await UniTask.Delay(100);
                executionOrder.Add("Handler2");
            }, 5);

            // Act
            var task = EventRaise.Event(new TestEvent())
                .WithContainer(_testContainer)
                .RaiseSequentialAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.AreEqual("Handler1", executionOrder[0]);
            Assert.AreEqual("Handler2", executionOrder[1]);
            Assert.IsTrue(startTimes[1] >= startTimes[0] + 0.08f, "Should execute sequentially");
        }

        #endregion

        #region Exception Handling Tests

        [UnityTest]
        public IEnumerator RaiseAsync_AsyncHandlerThrows_PropagatesException()
        {
            // Arrange
            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                throw new InvalidOperationException("Async test exception");
            });

            // Act
            var task = EventRaise.Event(new TestEvent()).RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted);
            Assert.IsInstanceOf<InvalidOperationException>(task.Exception.InnerException);
            Assert.AreEqual("Async test exception", task.Exception.InnerException.Message);
        }

        [UnityTest]
        public IEnumerator RaiseSequentialAsync_FirstHandlerThrows_SubsequentNotCalled()
        {
            // Arrange
            bool secondHandlerCalled = false;

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                throw new InvalidOperationException("First handler exception");
            }, 10);

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                secondHandlerCalled = true;
            }, 5);

            // Act
            var task = EventRaise.Event(new TestEvent()).RaiseSequentialAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(secondHandlerCalled, "Second handler should not be called");
        }

        #endregion

        #region Complex Fluent Interface Tests

        [UnityTest]
        public IEnumerator ComplexFluentChain_WithAsyncExecution_WorksCorrectly()
        {
            // Arrange
            bool containerHandlerCalled = false;
            bool globalHandlerCalled = false;
            bool condition = true;

            _testContainer.Register<TestEvent>(async evt => 
            {
                await UniTask.Delay(50);
                containerHandlerCalled = true;
                Assert.AreEqual(123, evt.Value);
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(50);
                globalHandlerCalled = true;
            });

            // Act
            var task = EventRaise.Event(new TestEvent(123, "fluent"))
                .WithContainer(_testContainer)
                .WithPolymorphic(false)
                .When(() => condition)
                .RaiseConcurrentAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(containerHandlerCalled);
            Assert.IsFalse(globalHandlerCalled);
        }

        [UnityTest]
        public IEnumerator SwitchingBetweenContainerAndGlobal_AsyncExecution_WorksCorrectly()
        {
            // Arrange
            bool containerHandlerCalled = false;
            bool globalHandlerCalled = false;

            _testContainer.Register<TestEvent>(async evt => 
            {
                await UniTask.Delay(10);
                containerHandlerCalled = true;
            });

            EventBus<TestEvent>.Register(async evt => 
            {
                await UniTask.Delay(10);
                globalHandlerCalled = true;
            });

            // Act - Start with container, switch to global
            var task = EventRaise.Event(new TestEvent())
                .WithContainer(_testContainer)
                .WithGlobalBus()
                .RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsFalse(containerHandlerCalled);
            Assert.IsTrue(globalHandlerCalled);
        }

        #endregion

        #region Edge Cases

        [UnityTest]
        public IEnumerator RaiseAsync_NoHandlers_CompletesSuccessfully()
        {
            // Act
            var task = EventRaise.Event(new TestEvent()).RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(task.IsCompletedSuccessfully);
        }

        [UnityTest]
        public IEnumerator RaiseAsync_EmptyEventNoArgs_CallsHandler()
        {
            // Arrange
            bool handlerCalled = false;

            EventBus<EmptyEvent>.Register(async () => 
            {
                await UniTask.Delay(10);
                handlerCalled = true;
            });

            // Act
            var task = EventRaise.Event<EmptyEvent>().RaiseAsync().AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        #endregion
#endif
    }
}