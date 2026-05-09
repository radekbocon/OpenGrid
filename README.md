# SimLab

A Linux desktop telemetry app for sim racing games. Includes and launches shared memory bridge. Veeery much work in progress. Tested only with ACC so far.

Built with [Avalonia UI](https://avaloniaui.net/) (.NET)

## Architecture

SimLab follows the **MVVM pattern** using `CommunityToolkit.Mvvm` with dependency injection via `Microsoft.Extensions.DependencyInjection`.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) - SimLab
- [.NET 8 SDK](https://dotnet.microsoft.com/download) - SimLabBridge
- [Protontricks](https://protontricks.com/) - To start the bidge in the correct prefix

### Build & Run

```bash
cd src
dotnet build
dotnet run --project SimLab
```

### Publishing

```bash
cd src
dotnet publish SimLab -c Release
```

The build automatically publishes `SimLabBridge` as a single-file Win-x64 executable alongside the main output, to reduce build times this can be disabled after `SimLabBridge` is published.

## TODO

- Map more fields to `TelemetryRecord`
- Support and test more games
- Add proper dashboards and overlays
- Send dashboards to other devices
- Improve session recording (currently has very naive implementation)
- much more!
