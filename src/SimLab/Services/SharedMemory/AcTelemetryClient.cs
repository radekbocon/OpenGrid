using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

/// <summary>
/// Service for reading binary telemetry data from shared memory files
/// Deserializes AC struct data from /dev/shm/ files
/// </summary>
public class AcTelemetryClient : ITelemetryClient
{
    private const string ShmPhysicsPath = "/dev/shm/acpmf_physics";
    private const string ShmGraphicsPath = "/dev/shm/acpmf_graphics";
    private const string ShmStaticPath = "/dev/shm/acpmf_static";

    private bool IsConnected => File.Exists(ShmPhysicsPath) &&
                                File.Exists(ShmGraphicsPath) &&
                                File.Exists(ShmStaticPath);

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
        if (!IsConnected)
        {
            return;
        }

        File.Delete(ShmPhysicsPath);
        File.Delete(ShmGraphicsPath);
        File.Delete(ShmStaticPath);
    }

    public TelemetryRecord? ReadTelemetry()
    {
        try
        {
            if (!IsConnected)
            {
                return null;
            }

            var physicsData = ReadStructFromFile<SPageFilePhysics>(ShmPhysicsPath, 2048);
            var graphicsData = ReadStructFromFile<SPageFileGraphic>(ShmGraphicsPath, 2048);
            var staticData = ReadStructFromFile<SPageFileStatic>(ShmStaticPath, 2048);

            if (physicsData == null || graphicsData == null || staticData == null)
            {
                return null;
            }

            // Create telemetry snapshot
            var snapshot = new TelemetryRecord
            {
                RecordedAt = DateTime.UtcNow,
                Track = staticData.Value.Track,
                Car = staticData.Value.CarModel,
                SessionType = (SessionType)graphicsData.Value.Session,
                CurrentLap = graphicsData.Value.CompletedLaps,
                SpeedKmh = physicsData.Value.SpeedKmh,
                SteerAngle = physicsData.Value.SteerAngle,
                Fuel = physicsData.Value.Fuel,
                Gas = physicsData.Value.Gas,
                Brake = physicsData.Value.Brake,
                Clutch = physicsData.Value.Clutch,
                CurrentGear = (Gear)physicsData.Value.Gear,
                EngineRpm = physicsData.Value.Rpms,
                TireTemperatures = TireValues.FromArray(physicsData.Value.TyreTemp.ToArray()),
                LapTime = TimeSpan.FromMilliseconds(graphicsData.Value.CurrentTime),
                Distance = graphicsData.Value.DistanceTraveled,
                MaxRpm = staticData.Value.MaxRpm,
                TirePressures = TireValues.FromArray(physicsData.Value.WheelsPressure.ToArray()),
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

