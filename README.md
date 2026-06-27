# OpenGrid

A Linux desktop telemetry app for sim racing games. Includes and launches shared memory bridge. Veeery much work in progress.

### Supported games:

- AC
- ACC
- AC Evo
- AC Rally
- Dirt Rally (limited data)
- Dirt Rally2 (limited data)

### Features:

- Save sessions in to .csv files, show and compare laps on  the charts
- Show live dashboards in the browser on any device on local network


## Architecture

Built with [Avalonia UI](https://avaloniaui.net/) (.NET)

Follows the **MVVM pattern** using `CommunityToolkit.Mvvm` with dependency injection via `Microsoft.Extensions.DependencyInjection`.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Avalonia UI](https://docs.avaloniaui.net/docs/get-started/)

### Build & Run

```bash
cd src
dotnet build
dotnet run --project OpenGrid
```

The build automatically publishes `OpenGridBridge` as a single-file Win-x64 executable alongside the main output.

## TODO

- Map more data
- Support and test more games
- Improve sesion analysis screen
- Add devices support
- much more!
