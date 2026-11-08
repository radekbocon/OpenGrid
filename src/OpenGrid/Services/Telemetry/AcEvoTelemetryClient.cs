using System.Runtime.InteropServices;
using OpenGrid.Models;
using OpenGrid.Models.Telemetry;
using Serilog;

namespace OpenGrid.Services.Telemetry;

public class AcEvoTelemetryClient : ITelemetryClient
{
    private bool IsConnected => File.Exists(AcConstants.ShmAcEvoPhysicsPath) &&
                                File.Exists(AcConstants.ShmAcEvoGraphicsPath) &&
                                File.Exists(AcConstants.ShmAcEvoStaticPath);

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
        TryDeleteFile(AcConstants.ShmAcEvoPhysicsPath);
        TryDeleteFile(AcConstants.ShmAcEvoGraphicsPath);
        TryDeleteFile(AcConstants.ShmAcEvoStaticPath);
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

            var physicsData = ReadStructFromFile<SPageFilePhysicsEvo>(AcConstants.ShmAcEvoPhysicsPath);
            var graphicsData = ReadStructFromFile<SPageFileGraphicEvo>(AcConstants.ShmAcEvoGraphicsPath);
            var staticData = ReadStructFromFile<SPageFileStaticEvo>(AcConstants.ShmAcEvoStaticPath);

            if (physicsData == null || graphicsData == null || staticData == null)
            {
                return null;
            }
            
            var playerCarIndex = graphicsData.Value.CarIds.IndexOf(
                new AcEvoCarId
                {
                    IdA = graphicsData.Value.PlayerCarIdA,
                    IdB = graphicsData.Value.PlayerCarIdB,
                });

            var carPosition = playerCarIndex >= 0 && playerCarIndex < graphicsData.Value.CarCoordinates.Length
                ? graphicsData.Value.CarCoordinates[playerCarIndex]
                : default;

            var session = staticData.Value.Session switch
            {
                AcEvoSessionType.Unknown => SessionType.Unknown,
                AcEvoSessionType.TimeAttack => SessionType.TimeAttack,
                AcEvoSessionType.Race => SessionType.Race,
                AcEvoSessionType.HotStint => SessionType.HotLap,
                AcEvoSessionType.Cruise => SessionType.Practice,
                _ => SessionType.Unknown,
            };

            // Create telemetry snapshot
            var snapshot = new TelemetryRecord
            {
                Timestamp = DateTime.UtcNow,
                Track = Track.Create(staticData.Value.Track),
                Car = Car.Create(graphicsData.Value.CarModel),
                SessionType = session,
                CurrentLap = graphicsData.Value.TotalLapCount,
                SpeedKmh = physicsData.Value.SpeedKmh,
                SteerAngle = physicsData.Value.SteerAngle,
                Fuel = physicsData.Value.Fuel,
                Gas = physicsData.Value.Gas,
                Brake = physicsData.Value.Brake,
                Clutch = physicsData.Value.Clutch,
                CurrentGear = (Gear)physicsData.Value.Gear,
                EngineRpm = physicsData.Value.Rpms,
                TireTemperatures = physicsData.Value.TyreTemp,
                LapTime = TimeSpan.FromMilliseconds(graphicsData.Value.CurrentLapTimeMs),
                Distance = graphicsData.Value.CurrentKm * 1000f,
                MaxRpm = physicsData.Value.CurrentMaxRpm,
                TirePressures = physicsData.Value.WheelsPressure,
                LastLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.LastLaptimeMs),
                BestLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.BestLaptimeMs),
                AbsSetting = graphicsData.Value.Electronics.AbsLevel,
                Tc1Setting = graphicsData.Value.Electronics.TcLevel,
                Tc2Setting = graphicsData.Value.Electronics.TcCutLevel,
                Abs = physicsData.Value.Abs,
                Tc = physicsData.Value.Tc,
                DeltaLapTime = TimeSpan.FromMilliseconds(graphicsData.Value.DeltaTimeMs),
                Position = (int)graphicsData.Value.CurrentPos,
                EngineMap = graphicsData.Value.Electronics.EngineMapLevel + 1,
                BrakeBias = physicsData.Value.BrakeBias,
                IsDeltaPositive = graphicsData.Value.DeltaTimeMs > 0,
                IsValidLap = graphicsData.Value.IsValidLap,
                // ACEvo shared memory does not expose sector timing.
                SectorIndex = 0,
                LastSectorTime = TimeSpan.Zero,
                GForceLat = physicsData.Value.AccG.X,
                GForceLon = physicsData.Value.AccG.Y,
                CarPosition = carPosition,
            };
            
            return snapshot;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading telemetry: {Message}", ex.Message);
            return null;
        }
    }
    
    private static T? ReadStructFromFile<T>(string filePath) where T : struct
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var size = Marshal.SizeOf<T>();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[size];
            var read = fs.Read(bytes, 0, size);
            return read < size ? null : BytesToStruct<T>(bytes, size);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading struct from file: {Message}", ex.Message);
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
            Log.Error(ex, "Error converting bytes to strMessagect: {Message}", ex.Message);
            return null;
        }
    }
}