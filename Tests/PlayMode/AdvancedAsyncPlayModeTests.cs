using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DGP.EventBus.Editor.Tests.PlayMode
{
    public class AdvancedAsyncPlayModeTests
    {
        private struct TestEvent : IEvent
        {
            public int TestValue;
        }

        private struct NestedEvent : IEvent
        {
            public int NestedValue;
        }

        [SetUp]
        public void Setup()
        {
            // Clear all bindings before each test to ensure test isolation
            EventBus<TestEvent>.ClearAllBindings();
            EventBus<NestedEvent>.ClearAllBindings();
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
    }
}