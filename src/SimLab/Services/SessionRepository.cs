using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using SimLab.Models;
using SimLab.Services.Telemetry;

namespace SimLab.Services;

public class SessionRepository
{
    private const int RecordHz = 30;

    private readonly string _telemetryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimLab", "Telemetry");

    private readonly ITelemetryService _telemetryService;
    private readonly SessionWriter _sessionWriter;
    private readonly TimeSpan _recordInterval = TimeSpan.FromMilliseconds(1000.0 / RecordHz);
    private DateTime _lastRecordTimestamp;
    private StreamWriter? _currentFileWriter;

    public Session? CurrentSession { get; private set; }

    public ObservableCollection<Session> Sessions { get; private set; } = [];

    public bool IsRecording
    {
        get;
        private set
        {
            field = value;
            IsRecordingChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? IsRecordingChanged;

    public SessionRepository(ITelemetryService telemetryService)
    {
        Directory.CreateDirectory(_telemetryFolder);
        _sessionWriter = new SessionWriter(_telemetryFolder);
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
    }

    public void LoadSessions()
    {
        var sessions = new List<Session>();
        var files = Directory.GetFiles(_telemetryFolder, "*.csv");

        foreach (var file in files)
        {
            try
            {
                var session = _sessionWriter.LoadFile(file);
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

        Sessions = new ObservableCollection<Session>(sessions);
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
        _telemetryService.TelemetryReceived -= TelemetryServiceOnTelemetryReceived;
        IsRecording = false;

        if (CurrentSession is not null)
        {
            CloseCurrentFile();
            Sessions.Add(CurrentSession);
            CurrentSession = null;
        }
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (CurrentSession is null)
        {
            CurrentSession = new Session(e.Game, e.Telemetry);
            _currentFileWriter = _sessionWriter.CreateFile(CurrentSession);
            _lastRecordTimestamp = e.Telemetry.Timestamp;
            return;
        }

        if (IsNewSession(e.Telemetry))
        {
            CloseCurrentFile();
            Sessions.Add(CurrentSession);
            CurrentSession = new Session(e.Game, e.Telemetry);
            _currentFileWriter = _sessionWriter.CreateFile(CurrentSession);
            _lastRecordTimestamp = e.Telemetry.Timestamp;
            return;
        }

        var now = e.Telemetry.Timestamp;
        if (now - _lastRecordTimestamp >= _recordInterval)
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

    public void DeleteLap(Session session, int lapNumber)
    {
        session.Records.RemoveAll(r => r.CurrentLap == lapNumber);
        session.Laps.RemoveAll(l => l.Number == lapNumber);
        _sessionWriter.WriteFull(session);
    }

    public void DeleteSession(Session session)
    {
        var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        Sessions.Remove(session);
    }

    private bool IsNewSession(TelemetryRecord record)
    {
        return record.SessionType != CurrentSession?.Info.Type || record.Track != CurrentSession?.Info.Track || record.Car != CurrentSession?.Info.Car;
    }
}
