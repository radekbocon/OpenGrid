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
    private bool IsConnected => File.Exists(AcConstants.ShmPhysicsPath) &&
                                File.Exists(AcConstants.ShmGraphicsPath) &&
                                File.Exists(AcConstants.ShmStaticPath);

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
            Log.Error(e, "Error connecting to telemetry: {0}", e.Message);
        }

        return false;
    }

    public void Stop()
    {
        TryDeleteFile(AcConstants.ShmPhysicsPath);
        TryDeleteFile(AcConstants.ShmGraphicsPath);
        TryDeleteFile(AcConstants.ShmStaticPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not delete file: {Path}", path);
        }
    }

    public TelemetryRecord? ReadTelemetry()
    {
        try
        {
            if (!IsConnected)
            {
                return null;
            }

            var physicsData = ReadStructFromFile<SPageFilePhysics>(AcConstants.ShmPhysicsPath, 2048);
            var graphicsData = ReadStructFromFile<SPageFileGraphic>(AcConstants.ShmGraphicsPath, 2048);
            var staticData = ReadStructFromFile<SPageFileStatic>(AcConstants.ShmStaticPath, 2048);

            if (physicsData == null || graphicsData == null || staticData == null)
            {
                return null;
            }

            if (graphicsData.Value.Status == GameStatus.OFF)
            {
                return null;
            }

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
                LastLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.LastTime),
                BestLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.BestTime),
                AbsSetting = graphicsData.Value.ABS,
                Tc1Setting = graphicsData.Value.TC,
                Tc2Setting = graphicsData.Value.TCCUT,
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
                CarPosition = graphicsData.Value.CarCoordinates[graphicsData.Value.PlayerCarID],
            };
            
            return snapshot;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading telemetry: {0}", ex.Message);
            return null;
        }
    }
    
    private static T? ReadStructFromFile<T>(string filePath, int size) where T : struct
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[size];
            var read = fs.Read(bytes, 0, size);
            return read < size ? null : BytesToStruct<T>(bytes, size);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading struct from file: {0}", ex.Message);
            return null;
        }
    }
    
    private static T? BytesToStruct<T>(byte[] data, int size) where T : struct
    {
        try
        {
            if (data.Length < size)
            {
                return null;
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject();
                var result = Marshal.PtrToStructure<T>(ptr);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error converting bytes to struct: {0}", ex.Message);
            return null;
        }
    }
}

