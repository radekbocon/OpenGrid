using System;
using System.IO;
using System.Runtime.InteropServices;
using SimLab.Models;

namespace SimLab.Services;

/// <summary>
/// Service for reading binary telemetry data from shared memory files
/// Deserializes ACC struct data from /dev/shm/ files
/// </summary>
public class AccSharedMemoryReader : ISharedMemoryReader
{
    private const string ShmPhysicsPath = "/dev/shm/simlab_physics";
    private const string ShmGraphicsPath = "/dev/shm/simlab_graphics";
    private const string ShmStaticPath = "/dev/shm/simlab_static";
    private const int AcPhysicsSize = 2048;
    private const int AcGraphicSize = 2048;
    private const int AcStaticSize = 2048;
    
    public TelemetrySnapshot? ReadTelemetryData()
    {
        try
        {
            if (!File.Exists(ShmPhysicsPath) || 
                !File.Exists(ShmGraphicsPath) || 
                !File.Exists(ShmStaticPath))
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
            var snapshot = new TelemetrySnapshot
            {
                RecordedAt = DateTime.UtcNow,
                Track = staticData.Value.track,
                SessionType = (SessionType)graphicsData.Value.session,
                CurrentLap = graphicsData.Value.completedLaps,
                SpeedKmh = physicsData.Value.speedKmh,
                Gas = physicsData.Value.gas,
                Brake = physicsData.Value.brake,
                Clutch = physicsData.Value.clutch,
                CurrentGear = (Gear)physicsData.Value.gear,
                EngineRpm = physicsData.Value.rpms,
                FuelRemaining = physicsData.Value.fuel,
                TireTemperatures = physicsData.Value.tyreTempM,
                IsInPit = graphicsData.Value.isInPit == 1,
                IsValidLap = graphicsData.Value.IsValidLap == 1,
                LapTime = ParseTime(graphicsData.Value.currentTime)
            };

            return snapshot;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading telemetry data: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Error reading struct from {filePath}: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Error converting bytes to struct: {ex.Message}");
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

