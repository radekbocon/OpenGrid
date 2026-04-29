using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services.SharedMemory;

/// <summary>
/// Service for reading binary telemetry data from shared memory files
/// Deserializes AC struct data from /dev/shm/ files
/// </summary>
public class AcTelemetryClient : ITelemetryClient
{
    private const string ShmPhysicsPath = "/dev/shm/simlab_physics";
    private const string ShmGraphicsPath = "/dev/shm/simlab_graphics";
    private const string ShmStaticPath = "/dev/shm/simlab_static";
    private const int AcPhysicsSize = 2048;
    private const int AcGraphicSize = 2048;
    private const int AcStaticSize = 2048;

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
            Debug.WriteLine($"{nameof(AcTelemetryClient)}.{nameof(ConnectAsync)}: {e.Message}");
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

            var physicsData = ReadStructFromFile<SPageFilePhysics>(ShmPhysicsPath, AcPhysicsSize);
            var graphicsData = ReadStructFromFile<SPageFileGraphic>(ShmGraphicsPath, AcGraphicSize);
            var staticData = ReadStructFromFile<SPageFileStatic>(ShmStaticPath, AcStaticSize);

            if (physicsData == null || graphicsData == null || staticData == null)
            {
                return null;
            }

            // Create telemetry snapshot
            var snapshot = new TelemetryRecord
            {
                RecordedAt = DateTime.UtcNow,
                Track = staticData.Value.Track,
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
                TireTemperatures = TireTemperatures.FromArray(physicsData.Value.TyreCoreTemperature),
                LapTime = ParseTime(graphicsData.Value.CurrentTime)
            };

            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading telemetry data: {ex.Message}");
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
            Debug.WriteLine($"Error reading struct from {filePath}: {ex.Message}");
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
            Debug.WriteLine($"Error converting bytes to struct: {ex.Message}");
            return null;
        }
    }
    
    private static TimeSpan ParseTime(string timeString)
    {
        var parts = timeString.Split(':');
        var minutes = int.Parse(parts[0]);
        var seconds = int.Parse(parts[1]);
        var milliseconds = int.Parse(parts[2]);
        return new TimeSpan(0, minutes, seconds, milliseconds);
    }
}

