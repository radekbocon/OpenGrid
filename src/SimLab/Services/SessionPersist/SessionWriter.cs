using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using SimLab.Models.Telemetry;

namespace SimLab.Services.SessionPersist;

public class SessionWriter
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string _telemetryFolder;

    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        InjectionOptions = InjectionOptions.Exception,
        InjectionCharacters = ['=', '*', '"']
    };

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

    public CsvWriter CreateFile(SessionDetails session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
        var writer = new StreamWriter(filePath);
        var csv = new CsvWriter(writer, CsvConfig);

        csv.WriteHeader<SessionMetaRow>();
        csv.NextRecord();
        csv.WriteRecord(SessionMetaRow.FromSessionInfo(session.Info));
        csv.NextRecord();

        writer.WriteLine();
        csv.WriteHeader<LapInfoRow>();
        csv.NextRecord();

        writer.WriteLine();
        csv.WriteHeader<TelemetryCsvRow>();
        csv.NextRecord();
        csv.Flush();

        return csv;
    }

    public void CloseFile(CsvWriter csv)
    {
        csv.Flush();
        csv.Dispose();
    }

    public void AppendRecord(CsvWriter csv, TelemetryRecord r)
    {
        csv.WriteRecord(TelemetryCsvRow.FromRecord(r));
        csv.NextRecord();
        csv.Flush();
    }

    public void WriteFull(SessionDetails session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CsvConfig);

        csv.WriteHeader<SessionMetaRow>();
        csv.NextRecord();
        csv.WriteRecord(SessionMetaRow.FromSessionInfo(session.Info));
        csv.NextRecord();

        writer.WriteLine();
        csv.WriteHeader<LapInfoRow>();
        csv.NextRecord();
        foreach (var lap in session.Laps)
        {
            csv.WriteRecord(LapInfoRow.FromLap(lap));
            csv.NextRecord();
        }

        writer.WriteLine();
        csv.WriteHeader<TelemetryCsvRow>();
        csv.NextRecord();
        foreach (var r in session.Records)
        {
            csv.WriteRecord(TelemetryCsvRow.FromRecord(r));
            csv.NextRecord();
        }
    }

    public SessionInfo? LoadMetadata(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = false,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        SessionInfo? sessionInfo = null;
        var lapList = new List<LapInfo>();
        var isHeader = true;

        while (csv.Read())
        {
            if (isHeader)
            {
                csv.ReadHeader();
                isHeader = false;
                continue;
            }

            if (string.IsNullOrEmpty(csv.GetField(0)))
            {
                isHeader = true;
                continue;
            }

            if (csv.HeaderRecord?[0] == "Timestamp")
            {
                break;
            }

            switch (csv.HeaderRecord?[0])
            {
                case "Id":
                    var metaRow = csv.GetRecord<SessionMetaRow?>();
                    if (metaRow is not null)
                    {
                        sessionInfo = SessionMetaRow.ToSessionInfo(metaRow);
                    }

                    break;
                case "Number":
                    var lapRow = csv.GetRecord<LapInfoRow?>();
                    if (lapRow is not null)
                    {
                        lapList.Add(lapRow.ToLapInfo());
                    }

                    break;
            }
        }

        if (sessionInfo is null)
        {
            return null;
        }

        sessionInfo.LapInfo = lapList;
        return sessionInfo;
    }

    public SessionDetails LoadDetails(string filePath, SessionInfo session)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = false,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        var records = new List<TelemetryRecord>();
        var isHeader = true;

        while (csv.Read())
        {
            if (isHeader)
            {
                csv.ReadHeader();
                isHeader = false;
                continue;
            }

            if (string.IsNullOrEmpty(csv.GetField(0)))
            {
                isHeader = true;
                continue;
            }

            if (csv.HeaderRecord?[0] != "Timestamp")
            {
                continue;
            }

            var telemetryRow = csv.GetRecord<TelemetryCsvRow?>();
            if (telemetryRow is not null)
            {
                records.Add(telemetryRow.ToRecord());
            }
        }

        return new SessionDetails(session, records);
    }
}
