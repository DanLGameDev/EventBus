using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            _eventContainer = new EventContainer();
        }

        [UnityTest]
        public IEnumerator TestPriorityOrderInvocation()
        {
            string invocationOrder = "";
            
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Low"; }, 0);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "Medium"; }, 5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "High"; }, 10);
            
            var task = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
        
        [UnityTest]
        public IEnumerator TestPriorityOrderForNoArgHandlers()
        {
            string invocationOrder = "";
            
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "Low"; }, 0);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "Medium"; }, 5);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "High"; }, 10);
            
            var task = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual("HighMediumLow", invocationOrder);
        }
        
        [UnityTest]
        public IEnumerator TestMixedHandlerTypes()
        {
            string invocationOrder = "";
            
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "ArgHigh"; }, 10);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "NoArgMedium"; }, 5);
            _eventContainer.Register<TestEvent>(_ => { invocationOrder += "ArgLow"; }, 0);
            _eventContainer.Register<TestEvent>(() => { invocationOrder += "NoArgVeryHigh"; }, 15);
            
            var task = _eventContainer.RaiseAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual("NoArgVeryHighArgHighNoArgMediumArgLow", invocationOrder);
        }

        [UnityTest]
        public IEnumerator TestAsyncPriorityOrder()
        {
            var executionOrder = new System.Collections.Generic.List<string>();
            
            _eventContainer.Register<TestEvent>(async _ => {
                await UniTask.Delay(10);
                executionOrder.Add("Low");
            }, 0);
            
            _eventContainer.Register<TestEvent>(async _ => {
                await UniTask.Delay(20);
                executionOrder.Add("Medium");
            }, 5);
            
            _eventContainer.Register<TestEvent>(async _ => {
                await UniTask.Delay(30);
                executionOrder.Add("High");
            }, 10);
            
            var task = _eventContainer.RaiseSequentialAsync(new TestEvent()).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsFaulted) throw task.Exception;
            
            Assert.AreEqual(3, executionOrder.Count);
            Assert.AreEqual("High", executionOrder[0]);
            Assert.AreEqual("Medium", executionOrder[1]);
            Assert.AreEqual("Low", executionOrder[2]);
        }
    }
}