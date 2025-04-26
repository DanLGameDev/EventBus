using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace DGP.EventBus.Editor.Tests
{
    public class AsyncEventPlayModeTests
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

        [UnityTest]
        public IEnumerator TestAsyncHandler()
        {
            int invokeCount = 0;
            int receivedValue = 0;

            // Register an async handler with explicit delegate type
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async (evt) => {
                await Task.Delay(100); // Simulate async work
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously
            Task task = EventBus<TestEvent>.RaiseAsync(new TestEvent { TestValue = 42 });
            
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
            EventBus<TestEvent>.Register((Func<Task>)(async () => {
                await Task.Delay(100); // Simulate async work
                invokeCount++;
            }));

            // Raise the event asynchronously
            Task task = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            
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
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(100);
                asyncCount++;
            }));

            // Raise the event asynchronously
            Task task = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            
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
            Func<TestEvent, Task> handler = async evt => {
                await Task.Delay(50);
                invokeCount++;
            };
            
            EventBus<TestEvent>.Register(handler);
            
            // Raise once - should be handled
            Task task1 = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            
            Assert.AreEqual(1, invokeCount);
            
            // Deregister
            EventBus<TestEvent>.Deregister(handler);
            
            // Raise again - should not be handled
            Task task2 = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            
            Assert.AreEqual(1, invokeCount, "Handler should not be called after deregistration");
        }

        [UnityTest]
        public IEnumerator TestAsyncPriorityOrder()
        {
            // Track order of execution
            var executionOrder = new List<string>();
            
            // Register handlers with different priorities
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(10);
                executionOrder.Add("Low");
            }), 0);
            
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(20);
                executionOrder.Add("Medium");
            }), 5);
            
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(30);
                executionOrder.Add("High");
            }), 10);
            
            // Raise the event
            Task task = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            // Verify the order (high to low)
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }

        [UnityTest]
        public IEnumerator TestEventContainerAsyncHandler()
        {
            var container = new EventContainer();
            int invokeCount = 0;
            int receivedValue = 0;

            // Register an async handler
            container.Register<TestEvent>((Func<TestEvent, Task>)(async (evt) => {
                await Task.Delay(100); // Simulate async work
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously
            Task task = container.RaiseAsync(new TestEvent { TestValue = 42 });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(42, receivedValue);
        }
        
        struct NestedEvent : IEvent
        {
            public int NestedValue;
        }

        [UnityTest]
        public IEnumerator TestNestedAsyncEventRaises()
        {
            var container = new EventContainer();
            
            var results = new List<string>();
            
            // First handler raises another event
            container.Register<TestEvent>((Func<TestEvent, Task>)(async evt => {
                results.Add($"TestEvent:{evt.TestValue}");
                await container.RaiseAsync(new NestedEvent { NestedValue = evt.TestValue * 2 });
                results.Add("TestEvent:After");
            }));
            
            // Handler for the nested event
            container.Register<NestedEvent>((Func<NestedEvent, Task>)(async evt => {
                await Task.Delay(100);
                results.Add($"NestedEvent:{evt.NestedValue}");
            }));
            
            // Raise the first event
            Task task = container.RaiseAsync(new TestEvent { TestValue = 42 });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            // Verify the sequence of operations
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("TestEvent:42", results[0]);
            Assert.AreEqual("NestedEvent:84", results[1]);
            Assert.AreEqual("TestEvent:After", results[2]);
        }

        [UnityTest]
        public IEnumerator TestExceptionHandling()
        {
            bool exceptionThrown = false;
            bool handlerAfterExceptionCalled = false;
            
            // Register a handler that will throw
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(10);
                throw new InvalidOperationException("Test exception");
            }));
            
            // Register another handler after the one that throws
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(10);
                handlerAfterExceptionCalled = true;
            }));
            
            // Raise the event and catch the exception
            Task task = EventBus<TestEvent>.RaiseAsync(new TestEvent());
            
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Verify the exception was thrown
            Assert.IsTrue(task.IsFaulted);
            if (task.Exception != null)
            {
                foreach (var ex in task.Exception.InnerExceptions)
                {
                    if (ex is InvalidOperationException opEx && opEx.Message == "Test exception")
                    {
                        exceptionThrown = true;
                        break;
                    }
                }
            }
            
            Assert.IsTrue(exceptionThrown, "Expected exception was not thrown");
            Assert.IsFalse(handlerAfterExceptionCalled, "Handlers after an exception should not be called");
        }

        [UnityTest]
        public IEnumerator TestBulkAsyncPerformance()
        {
            var container = new EventContainer();
            int processedCount = 0;
            
            // Register a handler that does minimal work
            container.Register<TestEvent>((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(1); // Minimal delay
                processedCount++;
            }));
            
            // Process multiple events
            const int eventCount = 10;
            var tasks = new List<Task>();
            
            for (int i = 0; i < eventCount; i++)
            {
                tasks.Add(container.RaiseAsync(new TestEvent { TestValue = i }));
            }
            
            // Wait for all tasks to complete
            Task allTasks = Task.WhenAll(tasks);
            yield return new WaitUntil(() => allTasks.IsCompleted);
            
            if (allTasks.IsFaulted)
                throw allTasks.Exception;
            
            // Verify all events were processed
            Assert.AreEqual(eventCount, processedCount);
        }

        #if UNITASK_SUPPORT
        // UniTask Tests

        [UnityTest]
        public IEnumerator TestUniTaskHandler()
        {
            int invokeCount = 0;
            int receivedValue = 0;

            // Register a UniTask handler with explicit delegate type
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async (evt) => {
                await UniTask.Delay(100); // Simulate async work
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously with UniTask
            UniTask uniTask = EventBus<TestEvent>.RaiseUniAsync(new TestEvent { TestValue = 42 });
            
            // Convert to Task for Unity test framework
            Task task = uniTask.AsTask();
            
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
        public IEnumerator TestUniTaskNoArgHandler()
        {
            int invokeCount = 0;

            // Register a UniTask handler with no args with explicit delegate type
            EventBus<TestEvent>.Register((Func<UniTask>)(async () => {
                await UniTask.Delay(100); // Simulate async work
                invokeCount++;
            }));

            // Raise the event asynchronously with UniTask
            UniTask uniTask = EventBus<TestEvent>.RaiseUniAsync(new TestEvent());
            
            // Convert to Task for Unity test framework
            Task task = uniTask.AsTask();
            
            // Wait for the task to complete
            yield return new WaitUntil(() => task.IsCompleted);
            
            // Check for errors
            if (task.IsFaulted)
                throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
        }

        [UnityTest]
        public IEnumerator TestMixedTaskAndUniTaskHandlers()
        {
            int taskCount = 0;
            int uniTaskCount = 0;
            int syncCount = 0;

            // Register different types of handlers with explicit delegate types
            EventBus<TestEvent>.Register((Action<TestEvent>)(_ => syncCount++));
            
            EventBus<TestEvent>.Register((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(100);
                taskCount++;
            }));
            
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(100);
                uniTaskCount++;
            }));

            // Raise with UniTask
            UniTask uniTask = EventBus<TestEvent>.RaiseUniAsync(new TestEvent());
            Task task = uniTask.AsTask();
            
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Verify all handlers were called
            Assert.AreEqual(1, syncCount);
            Assert.AreEqual(1, taskCount);
            Assert.AreEqual(1, uniTaskCount);
        }

        [UnityTest]
        public IEnumerator TestUniTaskHandlerDeregistration()
        {
            int invokeCount = 0;
            
            // Define and register the UniTask handler
            Func<TestEvent, UniTask> handler = async evt => {
                await UniTask.Delay(50);
                invokeCount++;
            };
            
            EventBus<TestEvent>.Register(handler);
            
            // Raise once - should be handled
            UniTask uniTask1 = EventBus<TestEvent>.RaiseUniAsync(new TestEvent());
            Task task1 = uniTask1.AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            
            Assert.AreEqual(1, invokeCount);
            
            // Deregister
            EventBus<TestEvent>.Deregister(handler);
            
            // Raise again - should not be handled
            UniTask uniTask2 = EventBus<TestEvent>.RaiseUniAsync(new TestEvent());
            Task task2 = uniTask2.AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            
            Assert.AreEqual(1, invokeCount, "Handler should not be called after deregistration");
        }

        [UnityTest]
        public IEnumerator TestEventContainerUniTaskHandler()
        {
            var container = new EventContainer();
            int invokeCount = 0;
            int receivedValue = 0;

            // Register a UniTask handler with explicit delegate type
            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async (evt) => {
                await UniTask.Delay(100);
                invokeCount++;
                receivedValue = evt.TestValue;
            }));

            // Raise the event asynchronously with UniTask
            UniTask uniTask = container.RaiseUniAsync(new TestEvent { TestValue = 42 });
            Task task = uniTask.AsTask();
            
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            // Verify the handler was called
            Assert.AreEqual(1, invokeCount);
            Assert.AreEqual(42, receivedValue);
        }

        [UnityTest]
        public IEnumerator TestNestedUniTaskEventRaises()
        {
            var container = new EventContainer();
            var results = new List<string>();
            
            // First handler raises another event with explicit delegate type
            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async evt => {
                results.Add($"TestEvent:{evt.TestValue}");
                await container.RaiseUniAsync(new NestedEvent { NestedValue = evt.TestValue * 2 });
                results.Add("TestEvent:After");
            }));
            
            // Handler for the nested event with explicit delegate type
            container.Register<NestedEvent>((Func<NestedEvent, UniTask>)(async evt => {
                await UniTask.Delay(100);
                results.Add($"NestedEvent:{evt.NestedValue}");
            }));
            
            // Raise the first event
            UniTask uniTask = container.RaiseUniAsync(new TestEvent { TestValue = 42 });
            Task task = uniTask.AsTask();
            
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            // Verify the sequence of operations
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("TestEvent:42", results[0]);
            Assert.AreEqual("NestedEvent:84", results[1]);
            Assert.AreEqual("TestEvent:After", results[2]);
        }

        [UnityTest]
        public IEnumerator TestUniTaskExceptionHandling()
        {
            bool exceptionThrown = false;
            bool handlerAfterExceptionCalled = false;
            
            // Register a handler that will throw with explicit delegate type
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(10);
                throw new InvalidOperationException("Test UniTask exception");
            }));
            
            // Register another handler after the one that throws
            EventBus<TestEvent>.Register((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(10);
                handlerAfterExceptionCalled = true;
            }));
            
            // Raise the event
            UniTask raiseTask = EventBus<TestEvent>.RaiseUniAsync(new TestEvent());
            
            // Convert to Task to check for exceptions in a coroutine-friendly way
            Task task = raiseTask.AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            
            if (task.IsFaulted && task.Exception != null)
            {
                foreach (var ex in task.Exception.InnerExceptions)
                {
                    if (ex is InvalidOperationException opEx && 
                        opEx.Message == "Test UniTask exception")
                    {
                        exceptionThrown = true;
                        break;
                    }
                }
            }
            
            // Need to wait a moment to ensure all potential handlers would have run
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(exceptionThrown, "Expected exception was not thrown");
            Assert.IsFalse(handlerAfterExceptionCalled, "Handlers after an exception should not be called");
        }

        [UnityTest]
        public IEnumerator TestBulkUniTaskPerformance()
        {
            var container = new EventContainer();
            int processedCount = 0;
            
            // Register a handler that does minimal work with explicit delegate type
            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(1); // Minimal delay
                processedCount++;
            }));
            
            // Process multiple events
            const int eventCount = 10;
            var tasks = new List<UniTask>();
            
            for (int i = 0; i < eventCount; i++)
            {
                tasks.Add(container.RaiseUniAsync(new TestEvent { TestValue = i }));
            }
            
            // Wait for all tasks to complete
            UniTask allTasks = UniTask.WhenAll(tasks);
            Task task = allTasks.AsTask();
            
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            // Verify all events were processed
            Assert.AreEqual(eventCount, processedCount);
        }

        [UnityTest]
        public IEnumerator TestUniTaskPerformanceComparison()
        {
            var container = new EventContainer();
            
            // For measuring execution time
            float taskTime = 0;
            float uniTaskTime = 0;
            
            // Task version with explicit delegate type
            container.Register<TestEvent>((Func<TestEvent, Task>)(async _ => {
                await Task.Delay(1);
            }));
            
            // Measure Task performance
            float startTime = Time.realtimeSinceStartup;
            
            const int eventCount = 100;
            var tasks = new List<Task>();
            
            for (int i = 0; i < eventCount; i++)
            {
                tasks.Add(container.RaiseAsync(new TestEvent()));
            }
            
            Task allTasks = Task.WhenAll(tasks);
            yield return new WaitUntil(() => allTasks.IsCompleted);
             
            taskTime = Time.realtimeSinceStartup - startTime;
            
            // Clear and set up UniTask version
            container.ClearAllBindings();
            
            container.Register<TestEvent>((Func<TestEvent, UniTask>)(async _ => {
                await UniTask.Delay(1);
            }));
            
            // Measure UniTask performance
            startTime = Time.realtimeSinceStartup;
            
            var uniTasks = new List<UniTask>();
            
            for (int i = 0; i < eventCount; i++)
            {
                uniTasks.Add(container.RaiseUniAsync(new TestEvent()));
            }
            
            UniTask allUniTasks = UniTask.WhenAll(uniTasks);
            Task task = allUniTasks.AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            
            uniTaskTime = Time.realtimeSinceStartup - startTime;
            
            // Log the results (no specific assertion, just for information)
            Debug.Log($"Performance comparison - Task: {taskTime:F5}s, UniTask: {uniTaskTime:F5}s");
            
            // We expect UniTask to be faster, but we don't make a hard assertion as
            // performance can vary based on the environment
            if (uniTaskTime < taskTime)
            {
                Debug.Log($"UniTask was {taskTime / uniTaskTime:F2}x faster");
            }
            else
            {
                Debug.Log($"Task was {uniTaskTime / taskTime:F2}x faster");
            }
        }
        #endif
    }
}