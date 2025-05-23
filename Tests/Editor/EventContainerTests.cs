using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGP.EventBus.Editor.Tests
{
    public class EventContainerTests
    {
        private struct TestEvent : IEvent
        {
            public int TestValue;
        }

        private EventContainer _eventContainer;

        [SetUp]
        public void Setup()
        {
            _eventContainer = new EventContainer();
        }

        private class MockHandler
        {
            public int InvokeCount;
            public int LastTestValue;
            private readonly Action<TestEvent> _onTestEvent;
            private readonly EventContainer _container;

            public MockHandler(EventContainer container)
            {
                _container = container;
                _onTestEvent = OnTestEvent;
                _container.Register<TestEvent>(_onTestEvent);
            }

            public void Deregister()
            {
                _container.Deregister<TestEvent>(_onTestEvent);
            }

            private void OnTestEvent(TestEvent @event)
            {
                InvokeCount++;
                LastTestValue = @event.TestValue;
            }
        }

        private class MockHandlerEmptyArgs
        {
            public int InvokeCount;
            private readonly Action _onTestEvent;
            private readonly EventContainer _container;

            public MockHandlerEmptyArgs(EventContainer container)
            {
                _container = container;
                _onTestEvent = OnTestEvent;
                _container.Register<TestEvent>(_onTestEvent);
            }

            public void Deregister()
            {
                _container.Deregister<TestEvent>(_onTestEvent);
            }

            private void OnTestEvent() => InvokeCount++;
        }

        [UnityTest]
        public IEnumerator TestArgEvent()
        {
            var handler = new MockHandler(_eventContainer);
            var task = _eventContainer.RaiseAsync(new TestEvent { TestValue = 42 }).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.AreEqual(1, handler.InvokeCount);
            Assert.AreEqual(42, handler.LastTestValue);
        }

        [UnityTest]
        public IEnumerator TestNoArgEvent()
        {
            var handler = new MockHandlerEmptyArgs(_eventContainer);
            var task = _eventContainer.RaiseAsync(new TestEvent { TestValue = 42 }).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;

            Assert.AreEqual(1, handler.InvokeCount);
        }

        [UnityTest]
        public IEnumerator TestDeregisterArgHandler()
        {
            var handler = new MockHandler(_eventContainer);

            var task1 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 42 }).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler.InvokeCount);

            handler.Deregister();

            var task2 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 100 }).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
            Assert.AreEqual(42, handler.LastTestValue, "LastTestValue should not change after deregistration");
        }

        [UnityTest]
        public IEnumerator TestDeregisterNoArgHandler()
        {
            var handler = new MockHandlerEmptyArgs(_eventContainer);

            var task1 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler.InvokeCount);

            handler.Deregister();

            var task2 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler.InvokeCount, "Handler should not receive events after deregistration");
        }

        [UnityTest]
        public IEnumerator TestClearBindings()
        {
            var handler1 = new MockHandler(_eventContainer);
            var handler2 = new MockHandlerEmptyArgs(_eventContainer);

            var task1 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 42 }).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);

            _eventContainer.ClearBindings<TestEvent>();

            var task2 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 100 }).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler1.InvokeCount, "Handler1 should not receive events after clearing bindings");
            Assert.AreEqual(1, handler2.InvokeCount, "Handler2 should not receive events after clearing bindings");
        }

        [UnityTest]
        public IEnumerator TestClearAllBindings()
        {
            var handler1 = new MockHandler(_eventContainer);
            var handler2 = new MockHandlerEmptyArgs(_eventContainer);

            var task1 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 42 }).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler1.InvokeCount);
            Assert.AreEqual(1, handler2.InvokeCount);

            _eventContainer.ClearAllBindings();

            var task2 = _eventContainer.RaiseAsync(new TestEvent { TestValue = 100 }).AsTask();
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

            _eventContainer.Register<TestEvent>(handler1);
            _eventContainer.Register<TestEvent>(handler2);

            var task1 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);

            _eventContainer.Deregister<TestEvent>(handler1);

            var task2 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, handler1Count, "Handler1 should not receive events after deregistration");
            Assert.AreEqual(2, handler2Count);

            _eventContainer.Deregister<TestEvent>(handler2);

            var task3 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
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

            _eventContainer.Register<TestEvent>(binding);

            var task1 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;
            Assert.AreEqual(1, eventCount);

            _eventContainer.Deregister<TestEvent>(binding);

            var task2 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;
            Assert.AreEqual(1, eventCount, "Handler should not receive events after deregistration");
        }

        [UnityTest]
        public IEnumerator TestMultipleContainers()
        {
            var secondContainer = new EventContainer();

            int container1Count = 0;
            int container2Count = 0;

            _eventContainer.Register<TestEvent>(_ => container1Count++);
            secondContainer.Register<TestEvent>(_ => container2Count++);

            var task1 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task1.IsCompleted);
            if (task1.IsFaulted) throw task1.Exception;

            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(0, container2Count);

            var task2 = secondContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task2.IsCompleted);
            if (task2.IsFaulted) throw task2.Exception;

            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(1, container2Count);

            _eventContainer.ClearAllBindings();

            var task3 = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task3.IsCompleted);
            if (task3.IsFaulted) throw task3.Exception;

            var task4 = secondContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task4.IsCompleted);
            if (task4.IsFaulted) throw task4.Exception;

            Assert.AreEqual(1, container1Count);
            Assert.AreEqual(2, container2Count);
        }

    }
}