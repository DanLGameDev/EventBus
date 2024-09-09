Implement an event from `IEvent`:
```csharp
public struct MyEvent : IEvent
{
    public string Message;
}
```

Create an event binding and register to receive events:
```csharp
public class MyClass
{
    private EventBinding<MyEvent> _eventBinding;
    
    public MyClass()
    {
        _eventBinding = new EventBinding<MyEvent>(OnMyEvent);
        EventBus<MyEvent>.Register(_eventBinding);
    }
    
    ~MyClass()
    {
        EventBus<MyEvent>.Deregister(_eventBinding);
    }
    
    private void OnMyEvent(MyEvent myEvent)
    {
        Debug.Log(myEvent.Message);
    }
}
```

Handlers can also have empty event arguments:
```csharp
public class MyClass
{
    private EventBinding _eventBinding;
    
    public MyClass()
    {
        _eventBinding = new EventBinding<MyEvent>(OnMyEvent);
        EventBus<MyEvent>.Register(_eventBinding );
    }
    
    ~MyClass()
    {
        EventBus<MyEvent>.Deregister(_eventBinding);
    }
    
    private void OnEmptyEvent()
    {
        Debug.Log("Empty event received");
    }
}
```

Raise an event:
```csharp
//No args
EventBus<MyEvent>.Raise();

//With Args
EventBus<MyEvent>.Raise(new MyEvent() { Message = "Event Data"; });
```
