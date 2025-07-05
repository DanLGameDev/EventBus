namespace DGP.EventBus.Editor.Tests
{
    /// <summary>
    /// Test event with data for comprehensive testing
    /// </summary>
    public struct TestEvent : IEvent
    {
        public int Value { get; }
        public string Message { get; }
        
        public TestEvent(int value, string message = "")
        {
            Value = value;
            Message = message;
        }
    }
    
    /// <summary>
    /// Simple empty event for basic testing scenarios
    /// </summary>
    public struct EmptyEvent : IEvent
    {
    }
    
    /// <summary>
    /// Event with different data type for inheritance testing
    /// </summary>
    public struct InheritedEvent : IEvent
    {
        public float FloatValue { get; }
        
        public InheritedEvent(float floatValue)
        {
            FloatValue = floatValue;
        }
    }
    
    /// <summary>
    /// Event for priority testing
    /// </summary>
    public struct PriorityTestEvent : IEvent
    {
        public int ExecutionOrder { get; }
        
        public PriorityTestEvent(int executionOrder)
        {
            ExecutionOrder = executionOrder;
        }
    }
}