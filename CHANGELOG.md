# Changelog

All notable changes to this package will be documented in this file.

## [1.1.0] - 2026-10-17
### Added
- Integrated Multi-Platform AutoBuilder framework (`AutoBuilder`) supporting:
  - Steam & Windows Standalone builds with SteamCMD upload, VDF generation, and error handling.
  - Android APK and Google Play App Bundle (.aab) builds with automated keystore management and optional ADB device deployment.
  - iOS Xcode project export with macOS host environment enforcement (`OSXEditor`).
  - WebGL builds with compression options, embedded C# `HttpListener` local test server, and static/API deployment to Penta (`C:\WebProjects\Penta`).
- Platform Strategy architecture (`IPlatformBuilder`) with host support validation.
- Interactive platform toolbar with manual "Switch Platform" button in `AutoBuilderWindow`.

## [1.0.4] - 2026-10-07
### Removed
- Removed obsolete Google Sheets synchronization tools (`DataSyncEditorWindow` and `LocalizationSyncEditorWindow`) in favor of PentaCore backend services.

## [1.0.3] - 2026-10-05
### Changed
- Modernized `MonoSingleton<T>` with `OnDestroy` reference cleanup and `HasInstance` check.
- Consolidated Editor tools (`GitToolbarButton`, `ScreenCapture`) into root `Editor/` folder under `Core.Editor` assembly.
- Removed obsolete `TMNLibraryEditor.asmdef`.
- Moved `EasyMethods` to `Core.Utils` with backward compatibility support for `TMNLib`.
- Cleaned up unused external assembly references from `TMNLibrary.asmdef`.

## [1.0.2] - 2026-10-04
### Removed
- Removed legacy `TMNLibrary.PoolManager` to eliminate type ambiguity with `Core.Pooling.PoolManager`.

## [1.0.1] - 2026-10-04
### Added
- `CorePackageUpdaterWindow`: Unity Editor Window and Quick Update menu item for one-click updates to latest Core package from GitHub or local mode.

## [1.0.0] - 2026-10-04
### Added
- Type-safe, decoupled `EventBus` architecture.
- Modular `SaveManager` with multi-slot and save migration support.
- Comprehensive `LocalizationManager` supporting JSON localization files.
- `AudioManager` and `AudioCueSO` for audio feedback.
- `InputManager` integrated with Unity Input System.
- High-performance `PoolManager` with `IPoolable` interface.
- Complete `SettingsUIController` and input rebinding UI.
- Thread-safe `Singleton` and `MonoSingleton` implementations.
- Editor utilities: Google Sheets Localization sync, Data sync, Screen capture.
- Gameplay utilities: `OutOfBoundsTrigger`, `RayTest`, and `SortingLayer` tools.
