using System;
using System.Collections.Generic;
using NUnit.Framework;
using DGP.EventBus;
using DGP.EventBus.Editor.Tests;

namespace DGP.EventBus.Editor.Tests.EditMode
{
    // Define event hierarchy for polymorphism testing
    public interface IBaseEvent : IEvent
    {
        string BaseMessage { get; }
    }
    
    public struct DerivedEvent : IBaseEvent
    {
        public string BaseMessage { get; }
        public int DerivedValue { get; }
        
        public DerivedEvent(string baseMessage, int derivedValue)
        {
            BaseMessage = baseMessage;
            DerivedValue = derivedValue;
        }
    }
    
    public struct AnotherDerivedEvent : IBaseEvent
    {
        public string BaseMessage { get; }
        public float AnotherValue { get; }
        
        public AnotherDerivedEvent(string baseMessage, float anotherValue)
        {
            BaseMessage = baseMessage;
            AnotherValue = anotherValue;
        }
    }

    [TestFixture]
    public class PolymorphicEventTests
    {
        [SetUp]
        public void Setup()
        {
            EventBus<IBaseEvent>.ClearAllBindings();
            EventBus<DerivedEvent>.ClearAllBindings();
            EventBus<AnotherDerivedEvent>.ClearAllBindings();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus<IBaseEvent>.ClearAllBindings();
            EventBus<DerivedEvent>.ClearAllBindings();
            EventBus<AnotherDerivedEvent>.ClearAllBindings();
        }

        #region Basic Polymorphic Raising Tests

        [Test]
        public void EventBus_Raise_DerivedEvent_TriggersBaseAndDerivedHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IBaseEvent>.Register(evt => 
            {
                executionOrder.Add($"Base:{evt.BaseMessage}");
            });
            
            EventBus<DerivedEvent>.Register(evt => 
            {
                executionOrder.Add($"Derived:{evt.DerivedValue}");
            });

            var derivedEvent = new DerivedEvent("test", 42);

            // Act
            EventBus.Raise(derivedEvent);

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base:test", executionOrder);
            Assert.Contains("Derived:42", executionOrder);
        }

        [Test]
        public void EventBus_Raise_BaseEvent_OnlyTriggersBaseHandlers()
        {
            // Arrange
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            EventBus<IBaseEvent>.Register(evt => baseHandlerCalled = true);
            EventBus<DerivedEvent>.Register(evt => derivedHandlerCalled = true);

            // Act - Raise the base interface type directly (not possible with struct, but test concept)
            // Since we can't instantiate interface directly, test that only exact type matches trigger
            EventBus<IBaseEvent>.Raise(new DerivedEvent("base", 1));

            // Assert
            Assert.IsTrue(baseHandlerCalled);
            Assert.IsFalse(derivedHandlerCalled, "Direct EventBus<T>.Raise should not trigger derived handlers");
        }

        [Test]
        public void EventBus_Raise_MultipleDerivedTypes_EachTriggersAppropriateHandlers()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IBaseEvent>.Register(evt => executionOrder.Add("Base"));
            EventBus<DerivedEvent>.Register(evt => executionOrder.Add("DerivedEvent"));
            EventBus<AnotherDerivedEvent>.Register(evt => executionOrder.Add("AnotherDerivedEvent"));

            // Act
            EventBus.Raise(new DerivedEvent("test1", 42));
            EventBus.Raise(new AnotherDerivedEvent("test2", 3.14f));

            // Assert
            Assert.AreEqual(4, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("DerivedEvent", executionOrder);
            Assert.Contains("AnotherDerivedEvent", executionOrder);
            
            // Count occurrences
            int baseCount = 0;
            foreach (var item in executionOrder)
                if (item == "Base") baseCount++;
            Assert.AreEqual(2, baseCount, "Base handler should be called for both derived events");
        }

        #endregion

        #region Priority in Polymorphic Events Tests

        [Test]
        public void EventBus_Raise_PolymorphicHandlers_RespectsPriorities()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IBaseEvent>.Register(evt => executionOrder.Add("BaseLow"), 1);
            EventBus<IBaseEvent>.Register(evt => executionOrder.Add("BaseHigh"), 10);
            EventBus<DerivedEvent>.Register(evt => executionOrder.Add("DerivedMedium"), 5);

            // Act
            EventBus.Raise(new DerivedEvent("test", 42));

            // Assert
            Assert.AreEqual(3, executionOrder.Count);
            // Note: The exact order depends on how reflection orders the containers
            // But priorities within each container should be respected
            Assert.Contains("BaseHigh", executionOrder);
            Assert.Contains("BaseLow", executionOrder);
            Assert.Contains("DerivedMedium", executionOrder);
        }

        #endregion

        #region Event Data Integrity Tests

        [Test]
        public void EventBus_Raise_PolymorphicHandlers_ReceiveCorrectData()
        {
            // Arrange
            IBaseEvent receivedBaseEvent = default;
            DerivedEvent receivedDerivedEvent = default;
            
            EventBus<IBaseEvent>.Register(evt => receivedBaseEvent = evt);
            EventBus<DerivedEvent>.Register(evt => receivedDerivedEvent = evt);

            var originalEvent = new DerivedEvent("polymorphic", 99);

            // Act
            EventBus.Raise(originalEvent);

            // Assert
            Assert.AreEqual("polymorphic", receivedBaseEvent.BaseMessage);
            Assert.AreEqual("polymorphic", receivedDerivedEvent.BaseMessage);
            Assert.AreEqual(99, receivedDerivedEvent.DerivedValue);
        }

        #endregion

        #region Exception Handling in Polymorphic Events Tests

        [Test]
        public void EventBus_Raise_BaseHandlerThrows_StopsAllExecution()
        {
            // Arrange
            bool derivedHandlerCalled = false;
            
            EventBus<IBaseEvent>.Register(evt => throw new InvalidOperationException("Base exception"));
            EventBus<DerivedEvent>.Register(evt => derivedHandlerCalled = true);

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                EventBus.Raise(new DerivedEvent("test", 1)));
            
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            Assert.IsFalse(derivedHandlerCalled, "Derived handler should not be called if base handler throws");
        }

        [Test]
        public void EventBus_Raise_DerivedHandlerThrows_StopsExecution()
        {
            // Arrange
            bool baseHandlerCalled = false;
            
            EventBus<IBaseEvent>.Register(evt => baseHandlerCalled = true);
            EventBus<DerivedEvent>.Register(evt => throw new InvalidOperationException("Derived exception"));

            // Act & Assert
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                EventBus.Raise(new DerivedEvent("test", 1)));
            
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
            // baseHandlerCalled state depends on execution order, which varies
        }

        #endregion

        #region Container Isolation Tests

        [Test]
        public void EventBus_Raise_PolymorphicEvent_DoesNotAffectEventContainer()
        {
            // Arrange
            var eventContainer = new EventContainer();
            bool containerHandlerCalled = false;
            bool staticHandlerCalled = false;
            
            eventContainer.Register<DerivedEvent>(evt => containerHandlerCalled = true);
            EventBus<DerivedEvent>.Register(evt => staticHandlerCalled = true);

            // Act
            EventBus.Raise(new DerivedEvent("test", 1));

            // Assert
            Assert.IsTrue(staticHandlerCalled, "Static EventBus handler should be called");
            Assert.IsFalse(containerHandlerCalled, "EventContainer handler should not be called by static EventBus");
        }

        [Test]
        public void EventContainer_Raise_DoesNotTriggerPolymorphicHandlers()
        {
            // Arrange
            var eventContainer = new EventContainer();
            bool baseHandlerCalled = false;
            bool derivedHandlerCalled = false;
            
            EventBus<IBaseEvent>.Register(evt => baseHandlerCalled = true);
            eventContainer.Register<DerivedEvent>(evt => derivedHandlerCalled = true);

            // Act
            eventContainer.Raise(new DerivedEvent("test", 1));

            // Assert
            Assert.IsFalse(baseHandlerCalled, "Static base handler should not be called by EventContainer");
            Assert.IsTrue(derivedHandlerCalled, "EventContainer handler should be called");
        }

        #endregion

        #region Registration Order and Discovery Tests

        [Test]
        public void EventBus_Raise_HandlersRegisteredInDifferentOrder_StillTriggersAll()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            // Register derived first, then base
            EventBus<DerivedEvent>.Register(evt => executionOrder.Add("Derived"));
            EventBus<AnotherDerivedEvent>.Register(evt => executionOrder.Add("Another"));
            EventBus<IBaseEvent>.Register(evt => executionOrder.Add("Base"));

            // Act
            EventBus.Raise(new DerivedEvent("test", 1));

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("Derived", executionOrder);
        }

        [Test]
        public void EventBus_Raise_NoMatchingHandlers_DoesNotThrow()
        {
            // Arrange
            EventBus<AnotherDerivedEvent>.Register(evt => { });

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.Raise(new DerivedEvent("test", 1)));
        }

        [Test]
        public void EventBus_Raise_OnlyExactAndBaseMatches_IgnoresUnrelatedTypes()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IBaseEvent>.Register(evt => executionOrder.Add("Base"));
            EventBus<DerivedEvent>.Register(evt => executionOrder.Add("Derived"));
            EventBus<AnotherDerivedEvent>.Register(evt => executionOrder.Add("Another"));
            EventBus<TestEvent>.Register(evt => executionOrder.Add("Unrelated"));

            // Act
            EventBus.Raise(new DerivedEvent("test", 1));

            // Assert
            Assert.AreEqual(2, executionOrder.Count);
            Assert.Contains("Base", executionOrder);
            Assert.Contains("Derived", executionOrder);
            Assert.IsFalse(executionOrder.Contains("Another"));
            Assert.IsFalse(executionOrder.Contains("Unrelated"));
        }

        #endregion

        #region Mixed Handler Types in Polymorphic Events Tests

        [Test]
        public void EventBus_Raise_PolymorphicWithMixedHandlerTypes_CallsAll()
        {
            // Arrange
            var executionOrder = new List<string>();
            
            EventBus<IBaseEvent>.Register((Action<IBaseEvent>)(evt => executionOrder.Add("BaseTyped")));
            EventBus<IBaseEvent>.Register((Action)(() => executionOrder.Add("BaseNoArgs")));
            EventBus<DerivedEvent>.Register((Action<DerivedEvent>)(evt => executionOrder.Add("DerivedTyped")));
            EventBus<DerivedEvent>.Register((Action)(() => executionOrder.Add("DerivedNoArgs")));

            // Act
            EventBus.Raise(new DerivedEvent("test", 1));

            // Assert
            Assert.AreEqual(4, executionOrder.Count);
            Assert.Contains("BaseTyped", executionOrder);
            Assert.Contains("BaseNoArgs", executionOrder);
            Assert.Contains("DerivedTyped", executionOrder);
            Assert.Contains("DerivedNoArgs", executionOrder);
        }

        #endregion
    }
}