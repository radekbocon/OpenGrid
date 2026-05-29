using System;
using SimLab.Models;
using SimLab.Models.Telemetry;

namespace SimLab.Services.Telemetry;

public class TelemetryEventArgs : EventArgs
{
    public TelemetryEventArgs(SteamGame game, TelemetryRecord telemetry)
    {
        Game = game;
        Telemetry = telemetry;
    }

    public SteamGame Game { get; }
    public TelemetryRecord Telemetry { get; }
}