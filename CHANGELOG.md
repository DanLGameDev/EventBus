## [2.1.0] - 2026-11-15
### Changes
- Polymorphic raises (`EventBus.Raise*`, `EventContainer.Raise*`) dispatch on the event's runtime type, so an event raised through a base-typed variable also reaches handlers registered for its concrete type

## [2.0.0] - 2026-10-30
### Breaking
- Removed the `EventBus.Bridges` assembly and `EventBusBridge`; editor tooling reads `EventBusDiagnostics` instead
- Polymorphic raises (`EventBus.Raise`, `EventContainer.Raise`) now throw the handler's exception directly instead of wrapping it in `TargetInvocationException`
- `Register(binding)` throws `ArgumentException` for a binding that cannot be invoked for the container's event type
### Changes
- `EventBus` gains `RaiseAsync`, `RaiseSequentialAsync` and `RaiseConcurrentAsync` for polymorphic async raising on the global bus
- The global bus is cleared on entering play mode, so handlers no longer survive between sessions with Domain Reload disabled
- Bindings with equal priority are invoked in registration order
- `TypedEventBinding<T>` and `NoArgsEventBinding` share an `EventBinding` base exposing `Handler` and `IsAsync`
### Fixes
- A handler throwing during a raise no longer leaves later deregistrations deferred indefinitely
- Nested raises of the same event no longer end deferred removal early
- Registering a handler for a new event type during a polymorphic raise no longer throws

## [1.9.0] - 2025-07-04
### Changes
- Refactored binding implementation
- Now supports polymorphic event raising on all implemented types via `EventBus` static
- Clearing handlers during invoke is now safe

## [1.8.5] - 2025-06-11
### Changes
- Raise method is now blocking

## [1.8.0] - 2025-05-23
### Changes
- UniTask now a required dependency
- Removed support for native Task based aync
- Raising events now uses UniTask
- RaiseConcurrent now raises all handlers in parallel

## [1.3.2] - 2024-10-18
### Changes
- EventBindingContainers can now be created to act as local eventbuses

## [1.3.0] - 2024-10-08
### Changes
- EventBus Monitor no longer dependent on Odin Inspector
- Updated member naming conventions to be more inline with C# standards
- Added XML documentation to public members

## [1.2.0] - 2024-09-25
### Changes
- Should now be safe to use with Domain Reload turned off as event busses are cleared when exiting play mode


## [1.1.1] - 2024-09-02
### Changes
- Editor script now in separate assembly
- Some scripts moved to runtime assemble and separate files

## [1.0.7] - 2024-09-01
### Bug Fixes
- Fixed issue where compilation error would occur if Odin Inspector wasn't present


## [1.0.0] - 2024-08-30
### First Release
- Initial Commit