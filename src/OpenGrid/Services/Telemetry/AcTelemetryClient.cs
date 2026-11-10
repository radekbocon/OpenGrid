using System.Runtime.InteropServices;
using Serilog;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;

namespace OpenGrid.Services.Telemetry;

/// <summary>
/// Service for reading binary telemetry data from shared memory files
/// Deserializes AC struct data from /dev/shm/ files
/// </summary>
public class AcTelemetryClient : ITelemetryClient
{
    private bool IsConnected => File.Exists(AcConstants.ShmAcPhysicsPath) &&
                                File.Exists(AcConstants.ShmAcGraphicsPath) &&
                                File.Exists(AcConstants.ShmAcStaticPath);

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    return true;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error connecting to telemetry: {Message}", e.Message);
        }

        return false;
    }

    public void Stop()
    {
        ShmHelper.TryDeleteFile(AcConstants.ShmAcPhysicsPath);
        ShmHelper.TryDeleteFile(AcConstants.ShmAcGraphicsPath);
        ShmHelper.TryDeleteFile(AcConstants.ShmAcStaticPath);
    }

    public TelemetryRecord? ReadTelemetry()
    {
        try
        {
            if (!IsConnected)
            {
                return null;
            }

            var physicsData = ShmHelper.ReadStructFromFile<SPageFilePhysics>(AcConstants.ShmAcPhysicsPath);
            var graphicsData = ShmHelper.ReadStructFromFile<SPageFileGraphic>(AcConstants.ShmAcGraphicsPath);
            var staticData = ShmHelper.ReadStructFromFile<SPageFileStatic>(AcConstants.ShmAcStaticPath);

            if (physicsData == null || graphicsData == null || staticData == null)
            {
                return null;
            }
            
            var playerCarIndex = graphicsData.Value.CarIDs.IndexOf(graphicsData.Value.PlayerCarID);

            // Create telemetry snapshot
            var snapshot = new TelemetryRecord
            {
                Timestamp = DateTime.UtcNow,
                Track = Track.Create(staticData.Value.Track),
                Car = Car.Create(staticData.Value.CarModel),
                SessionType = graphicsData.Value.Session,
                CurrentLap = graphicsData.Value.CompletedLaps,
                SpeedKmh = physicsData.Value.SpeedKmh,
                SteerAngle = physicsData.Value.SteerAngle,
                Fuel = physicsData.Value.Fuel,
                Gas = physicsData.Value.Gas,
                Brake = physicsData.Value.Brake,
                Clutch = physicsData.Value.Clutch,
                CurrentGear = (Gear)physicsData.Value.Gear,
                EngineRpm = physicsData.Value.Rpms,
                TireTemperatures = physicsData.Value.TyreTemp,
                LapTime = TimeSpan.FromMilliseconds(graphicsData.Value.CurrentTime),
                Distance = graphicsData.Value.DistanceTraveled,
                MaxRpm = staticData.Value.MaxRpm,
                TirePressures = physicsData.Value.WheelsPressure,
                LastLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.LastTime == 2147483647 ? 0 : graphicsData.Value.LastTime),
                BestLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.BestTime == 2147483647 ? 0 : graphicsData.Value.BestTime),
                AbsSetting = graphicsData.Value.ABS,
                Tc1Setting = graphicsData.Value.TC,
                Tc2Setting = graphicsData.Value.TCCUT,
                Abs = physicsData.Value.Abs,
                Tc = physicsData.Value.TC,
                DeltaLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.DeltaLapTime),
                Position = graphicsData.Value.Position,
                EngineMap = graphicsData.Value.EngineMap + 1,
                BrakeBias = physicsData.Value.BrakeBias,
                IsDeltaPositive = graphicsData.Value.IsDeltaPositive == 1,
                IsValidLap = graphicsData.Value.IsValidLap == 1,
                SectorIndex = graphicsData.Value.CurrentSectorIndex,
                LastSectorTime = TimeSpan.FromMilliseconds(graphicsData.Value.LastSectorTime),
                GForceLat = physicsData.Value.AccG.X,
                GForceLon = physicsData.Value.AccG.Z,
                CarPosition = graphicsData.Value.CarCoordinates[playerCarIndex],
            };
            
            return snapshot;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading telemetry: {Message}", ex.Message);
            return null;
        }
    }
}