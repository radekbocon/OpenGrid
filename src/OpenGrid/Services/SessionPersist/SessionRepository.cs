using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Serilog;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.Services.SessionPersist;

public class SessionRepository
{
    private readonly string _telemetryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OpenGrid", "Telemetry");

    private readonly ITelemetryService _telemetryService;
    private readonly ISettingsService _settingsService;
    private readonly SessionWriter _sessionWriter;
    private TimeSpan RecordInterval => TimeSpan.FromMilliseconds(1000.0 / _settingsService.RecordingRateHz);
    private DateTime _lastRecordTimestamp;
    private CsvWriter? _currentFileWriter;

    public SessionDetails? CurrentSession { get; private set; }

    public bool IsRecording
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }
            
            field = value;
            IsRecordingChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsRecordingChanged;

    public SessionRepository(ITelemetryService telemetryService, ISettingsService settingsService)
    {
        Directory.CreateDirectory(_telemetryFolder);
        _sessionWriter = new SessionWriter(_telemetryFolder);
        _telemetryService = telemetryService;
        _settingsService = settingsService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
    }

    public async Task<List<SessionInfo>> LoadSessionsAsync()
    {
        var sessions = new List<SessionInfo>();

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(_telemetryFolder, "*.csv");
            foreach (var file in files)
            {
                try
                {
                    var session = _sessionWriter.LoadMetadata(file);
                    session?.LapInfo.MinBy(x => x.Time)?.IsFastest = true;
                    if (session is not null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to load session from {File}", file);
                }
            }
        });


        return sessions.OrderByDescending(x => x.StartTime).ToList();
    }

    public SessionDetails LoadSessionDetails(SessionInfo session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.FileName);
        var details = _sessionWriter.LoadDetails(filePath, session);
        return details;
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (IsRecording && e == TelemetryConnectionStatus.Disconnected)
        {
            _ = StopRecordingAsync();
        }
    }

    public async Task StartRecordingAsync()
    {
        IsRecording = true;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;
    }

    public async Task StopRecordingAsync()
    {
        try
        {
            _telemetryService.TelemetryReceived -= TelemetryServiceOnTelemetryReceived;

            if (CurrentSession is not null)
            {
                FinalizeCurrentSession();
                CurrentSession = null;
            }
        }
        finally
        {
            IsRecording = false;
        }
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (CurrentSession is null)
        {
            CurrentSession = new SessionDetails(e.Game, e.Telemetry);
            _currentFileWriter = _sessionWriter.CreateFile(CurrentSession);
            _lastRecordTimestamp = e.Telemetry.Timestamp;
            return;
        }

        if (IsNewSession(e.Telemetry))
        {
            FinalizeCurrentSession();
            CurrentSession = new SessionDetails(e.Game, e.Telemetry);
            _currentFileWriter = _sessionWriter.CreateFile(CurrentSession);
            _lastRecordTimestamp = e.Telemetry.Timestamp;
            return;
        }

        var now = e.Telemetry.Timestamp;
        if (now - _lastRecordTimestamp >= RecordInterval)
        {
            CurrentSession.AddRecord(e.Telemetry);
            _sessionWriter.AppendRecord(_currentFileWriter!, e.Telemetry);
            _lastRecordTimestamp = now;
        }
    }

    private void CloseCurrentFile()
    {
        if (_currentFileWriter is null)
        {
            return;
        }

        _sessionWriter.CloseFile(_currentFileWriter);
        _currentFileWriter = null;
    }

    private void FinalizeCurrentSession()
    {
        CloseCurrentFile();
        _sessionWriter.WriteFull(CurrentSession!);
    }

    public void DeleteLap(SessionDetails details, int lapNumber)
    {
        details.DeleteLap(lapNumber);
        _sessionWriter.WriteFull(details);
    }

    public void DeleteSession(string fileName)
    {
        var filePath = Path.Combine(_telemetryFolder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private bool IsNewSession(TelemetryRecord record)
    {
        return record.SessionType != CurrentSession?.Info.Type || record.Track != CurrentSession?.Info.Track || record.Car != CurrentSession?.Info.Car;
    }
}
