using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using SimLab.Models;

namespace SimLab.Services;

public class SessionWriter
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _telemetryFolder;

    public SessionWriter(string telemetryFolder)
    {
        _telemetryFolder = telemetryFolder;
    }

    public static double ToUnixSeconds(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return (dt.ToUniversalTime() - UnixEpoch).TotalSeconds;
    }

    public static DateTime FromUnixSeconds(double seconds) => UnixEpoch.AddSeconds(seconds);

    public StreamWriter CreateFile(Session session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
        var writer = new StreamWriter(filePath);

        writer.WriteLine($"# Id: {session.Info.Id}");
        writer.WriteLine($"# Game: {session.Info.Game.AppId}");
        writer.WriteLine($"# Car: {session.Info.Car}");
        writer.WriteLine($"# Track: {session.Info.Track}");
        writer.WriteLine($"# Type: {(int)session.Info.Type}");
        writer.WriteLine($"# StartTime: {ToUnixSeconds(session.Info.StartTime).ToString(CultureInfo.InvariantCulture)}");
        writer.WriteLine("Timestamp,CurrentLap,SpeedKmh,Gas,Brake,Clutch,SteerAngle,Fuel,CurrentGear,EngineRpm,TireTemperatureFL,TireTemperatureFR,TireTemperatureRL,TireTemperatureRR,LapTime,Distance,MaxRpm,TirePressureFL,TirePressureFR,TirePressureRL,TirePressureRR,LastLapTime,BestLapTime,AbsSetting,Tc1Setting,Tc2Setting,DeltaLapTime,Position,EngineMap,BrakeBias,IsDeltaPositive,IsValidLap,PosX,PosY,PosZ");
        writer.Flush();
        return writer;
    }

    public void CloseFile(StreamWriter writer)
    {
        writer.Close();
    }

    public void AppendRecord(StreamWriter writer, TelemetryRecord r)
    {
        writer.Write($"{ToUnixSeconds(r.Timestamp).ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.CurrentLap},");
        writer.Write($"{r.SpeedKmh.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Gas.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Brake.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Clutch.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.SteerAngle.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Fuel.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{(int)r.CurrentGear},");
        writer.Write($"{r.EngineRpm.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TireTemperatures.FrontLeft.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TireTemperatures.FrontRight.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TireTemperatures.RearLeft.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TireTemperatures.RearRight.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.LapTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Distance.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.MaxRpm.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TirePressures.FrontLeft.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TirePressures.FrontRight.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TirePressures.RearLeft.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.TirePressures.RearRight.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.LastLapTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.BestLapTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.AbsSetting},");
        writer.Write($"{r.Tc1Setting},");
        writer.Write($"{r.Tc2Setting},");
        writer.Write($"{r.DeltaLapTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.Position},");
        writer.Write($"{r.EngineMap},");
        writer.Write($"{r.BrakeBias.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.IsDeltaPositive},");
        writer.Write($"{r.IsValidLap},");
        writer.Write($"{r.CarPosition.X.ToString(CultureInfo.InvariantCulture)},");
        writer.Write($"{r.CarPosition.Y.ToString(CultureInfo.InvariantCulture)},");
        writer.WriteLine($"{r.CarPosition.Z.ToString(CultureInfo.InvariantCulture)}");
        writer.Flush();
    }

    public void WriteFull(Session session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
        using var writer = new StreamWriter(filePath);

        writer.WriteLine($"# Id: {session.Info.Id}");
        writer.WriteLine($"# Game: {session.Info.Game.AppId}");
        writer.WriteLine($"# Car: {session.Info.Car}");
        writer.WriteLine($"# Track: {session.Info.Track?.Key}");
        writer.WriteLine($"# Type: {(int)session.Info.Type}");
        writer.WriteLine($"# StartTime: {ToUnixSeconds(session.Info.StartTime).ToString(CultureInfo.InvariantCulture)}");

        foreach (var r in session.Records)
        {
            AppendRecord(writer, r);
        }
    }

    public Session? LoadFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
        {
            return null;
        }

        Guid? id = null;
        int? gameAppId = null;
        SessionType? type = null;
        string? car = null;
        string? track = null;
        DateTime? startTime = null;

        var records = new List<TelemetryRecord>();
        var headerFound = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '#')
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex < 0)
                {
                    continue;
                }

                var key = line[1..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();

                switch (key)
                {
                    case "Id":
                        id = Guid.Parse(value);
                        break;
                    case "Game":
                        gameAppId = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "Type":
                        type = (SessionType)int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "Car":
                        car = value;
                        break;
                    case "Track":
                        track = value;
                        break;
                    case "StartTime":
                        startTime = FromUnixSeconds(double.Parse(value, CultureInfo.InvariantCulture));
                        break;
                }

                continue;
            }

            if (!headerFound)
            {
                headerFound = true;
                continue;
            }

            var values = line.Split(',');
            if (values.Length < 32)
            {
                continue;
            }

            var idx = 0;
            var record = new TelemetryRecord
            {
                Timestamp = FromUnixSeconds(double.Parse(values[idx++], CultureInfo.InvariantCulture)),
                CurrentLap = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                SpeedKmh = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                Gas = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                Brake = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                Clutch = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                SteerAngle = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                Fuel = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                CurrentGear = (Gear)int.Parse(values[idx++], CultureInfo.InvariantCulture),
                EngineRpm = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                TireTemperatures = new TireValues(
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture)),
                LapTime = TimeSpan.FromSeconds(double.Parse(values[idx++], CultureInfo.InvariantCulture)),
                Distance = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                MaxRpm = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                TirePressures = new TireValues(
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture)),
                LastLapTime = TimeSpan.FromSeconds(double.Parse(values[idx++], CultureInfo.InvariantCulture)),
                BestLapTime = TimeSpan.FromSeconds(double.Parse(values[idx++], CultureInfo.InvariantCulture)),
                AbsSetting = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                Tc1Setting = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                Tc2Setting = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                DeltaLapTime = TimeSpan.FromSeconds(double.Parse(values[idx++], CultureInfo.InvariantCulture)),
                Position = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                EngineMap = int.Parse(values[idx++], CultureInfo.InvariantCulture),
                BrakeBias = float.Parse(values[idx++], CultureInfo.InvariantCulture),
                IsDeltaPositive = bool.Parse(values[idx++]),
                IsValidLap = bool.Parse(values[idx++]),
            };

            if (values.Length >= 35)
            {
                record.CarPosition = new Vector3(
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture),
                    float.Parse(values[idx++], CultureInfo.InvariantCulture));
            }

            records.Add(record);
        }

        if (!headerFound || records.Count == 0)
        {
            return null;
        }

        var game = SteamGame.GetByAppId(gameAppId ?? 0);
        if (game is null)
        {
            return null;
        }

        return new Session(
            new SessionInfo(
                id ?? Guid.NewGuid(),
                game,
                type ?? SessionType.Unknown,
                Car.Create(car),
                Track.Create(track),
                startTime ?? DateTime.MinValue),
            records);
    }
}
