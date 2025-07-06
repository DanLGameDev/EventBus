# Unity EventBus Library

A flexible, high-performance event system for Unity with support for synchronous and asynchronous event handling, priority-based execution, and polymorphic event dispatch.

## Features

- **Multiple Handler Types**: Action delegates, no-args delegates, and UniTask async handlers
- **Priority System**: Control execution order with priority values (higher priority = earlier execution)
- **Polymorphic Events**: Events can trigger handlers for base types and interfaces
- **Async Support**: Sequential and concurrent async event execution (requires UniTask)
- **Stoppable Events**: Events can halt propagation mid-execution
- **Thread-Safe Modifications**: Safe registration/deregistration during event raising
- **Multiple Usage Patterns**: Static EventBus, typed EventBus<T>, or instanced EventContainer

## Quick Start

### Define Events

```csharp
public struct PlayerDiedEvent : IEvent
{
    public string PlayerName;
    public float Score;
}

public struct GamePausedEvent : IEvent, IStoppableEvent
{
    public bool StopPropagation { get; set; }
}
```

### Register Handlers

```csharp
// Typed handlers
EventBus<PlayerDiedEvent>.Register(OnPlayerDied);
EventBus<PlayerDiedEvent>.Register(OnPlayerDiedAsync, priority: 10);

// No-args handlers
EventBus<GamePausedEvent>.Register(OnAnyGameEvent);

void OnPlayerDied(PlayerDiedEvent evt)
{
    Debug.Log($"{evt.PlayerName} died with score {evt.Score}");
}

async UniTask OnPlayerDiedAsync(PlayerDiedEvent evt)
{
    await ShowDeathAnimation();
}

void OnAnyGameEvent()
{
    Debug.Log("Some game event occurred");
}
```

### Raise Events

```csharp
// Simple raise
EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent 
{ 
    PlayerName = "Alice", 
    Score = 1500 
});

// Async sequential
await EventBus<PlayerDiedEvent>.RaiseSequentialAsync(playerEvent);

// Async concurrent
await EventBus<PlayerDiedEvent>.RaiseConcurrentAsync(playerEvent);
```

## Usage Patterns

### 1. Static EventBus<T> (Recommended)

Best for global game events with compile-time type safety:

```csharp
// Registration
var binding = EventBus<PlayerLevelUpEvent>.Register(OnPlayerLevelUp, priority: 5);

// Deregistration
EventBus<PlayerLevelUpEvent>.Deregister(OnPlayerLevelUp);
// or
EventBus<PlayerLevelUpEvent>.Deregister(binding);

// Raising
EventBus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent { NewLevel = 5 });
```

### 2. EventContainer (Instanced)

Best for scoped events (UI systems, game modes, etc.):

```csharp
public class GameModeManager
{
    private readonly EventContainer _events = new();
    
    public void Initialize()
    {
        _events.Register<RoundStartEvent>(OnRoundStart);
        _events.Register<RoundEndEvent>(OnRoundEnd);
    }
    
    public void StartRound()
    {
        _events.Raise(new RoundStartEvent { RoundNumber = currentRound });
    }
}
```

### 3. Fluent EventRaiser API

For conditional or configurable event raising:

```csharp
// Basic usage
RaiseEvent.Event(new PlayerDiedEvent { PlayerName = "Bob" })
    .RaiseSync();

// With conditions
RaiseEvent.Event(new ScoreUpdateEvent { Score = newScore })
    .When(() => gameActive && !paused)
    .RaiseSync();

// With specific container
RaiseEvent.Event(new UIEvent())
    .WithContainer(uiEventContainer)
    .WithPolymorphic(false)
    .RaiseSync();

// Async variants
await RaiseEvent.Event(new SaveGameEvent())
    .RaiseSequentialAsync();
```

## Advanced Features

### Priority System

Higher priority handlers execute first:

```csharp
EventBus<DamageEvent>.Register(ApplyArmor, priority: 10);     // Runs first
EventBus<DamageEvent>.Register(ApplyDamage, priority: 5);    // Runs second
EventBus<DamageEvent>.Register(UpdateUI, priority: 0);       // Runs last
```

### Polymorphic Events

Handlers for base types and interfaces are automatically triggered:

```csharp
public interface IGameEvent : IEvent { }
public struct SpecificEvent : IGameEvent { }

// This handler will receive ALL IGameEvent implementations
EventBus<IGameEvent>.Register(OnAnyGameEvent);

// This raises to both IGameEvent and SpecificEvent handlers
EventBus<SpecificEvent>.Raise(new SpecificEvent());
```

### Stoppable Events

Events can halt propagation to remaining handlers:

```csharp
public struct InputEvent : IEvent, IStoppableEvent
{
    public bool StopPropagation { get; set; }
    public KeyCode Key;
}

void OnInputHandler(InputEvent evt)
{
    if (evt.Key == KeyCode.Escape && menuOpen)
    {
        evt.StopPropagation = true; // Stop other handlers
        CloseMenu();
    }
}
```

### Async Execution Modes

```csharp
// Sequential: Handlers execute one after another
await EventBus<SaveEvent>.RaiseSequentialAsync(saveEvent);

// Concurrent: All handlers start simultaneously
await EventBus<EffectEvent>.RaiseConcurrentAsync(effectEvent);
```

## Best Practices

1. **Use structs for events** - Better performance and prevents unintended mutations
2. **Prefer EventBus<T>** - Type-safe and performant for global events
3. **Use EventContainer** - For scoped systems that need isolation
4. **Set priorities wisely** - Critical handlers (like damage armor) should run first
5. **Implement IStoppableEvent** - For events that might need to halt propagation
6. **Clean up bindings** - Always deregister handlers when objects are destroyed

## Installation Requirements

- Unity 2021.3 or higher
- UniTask package (optional, for async support)

To enable UniTask support, add `UNITASK_SUPPORT` to your project's Scripting Define Symbols.