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