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
    
    private DateTime _lastRecordTimestamp;
    private CsvWriter? _currentFileWriter;
    private TelemetryRecord? _lastReceived;

    private TimeSpan RecordInterval => TimeSpan.FromMilliseconds(1000.0 / _settingsService.RecordingRateHz);
    
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
            StopRecording();
        }
    }

    public void StartRecording()
    {
        IsRecording = true;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;
    }

    public void StopRecording()
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
            StartNewSession(e);
            return;
        }

        if (IsNewSession(e.Telemetry))
        {
            FinalizeCurrentSession();
            StartNewSession(e);
            return;
        }

        var now = e.Telemetry.Timestamp;
        
        // Make sure last record of the lap is not skipped 
        if (_lastReceived != null && e.Telemetry.CurrentLap != _lastReceived.CurrentLap)
        {
            CurrentSession.AddRecord(_lastReceived);
            _sessionWriter.AppendRecord(_currentFileWriter!, _lastReceived);
            CurrentSession.AddRecord(e.Telemetry);
            _sessionWriter.AppendRecord(_currentFileWriter!, e.Telemetry);
            _lastRecordTimestamp = now;
            _lastReceived = null;
            return;
        }

        if (now - _lastRecordTimestamp >= RecordInterval)
        {
            CurrentSession.AddRecord(e.Telemetry);
            _sessionWriter.AppendRecord(_currentFileWriter!, e.Telemetry);
            _lastRecordTimestamp = now;
            _lastReceived = null;
            return;
        }

        _lastReceived = e.Telemetry;
    }

    private void StartNewSession(TelemetryEventArgs e)
    {
        CurrentSession = new SessionDetails(e.Game, e.Telemetry);
        _currentFileWriter = _sessionWriter.CreateFile(CurrentSession);
        _lastRecordTimestamp = e.Telemetry.Timestamp;
        _lastReceived = null;
        Log.Information("Started recording {Game} on {Track}", e.Game, e.Telemetry.Track);
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

        var session = CurrentSession!;
        var totalLaps = session.Records.GroupBy(r => r.CurrentLap).Count();
        var validLaps = session.Records
            .GroupBy(r => r.CurrentLap)
            .Where(g =>
            {
                var distance = g.Max(r => r.Distance) - g.Min(r => r.Distance);
                var time = g.Max(r => r.LapTime);
                return distance >= 100f && time >= TimeSpan.FromSeconds(10);
            })
            .Select(g => g.Key)
            .ToHashSet();

        var removedLaps = totalLaps - validLaps.Count;
        if (removedLaps > 0)
        {
            Log.Information("Removed {Count} partial lap(s) from {Game} on {Track}", removedLaps, session.Info.Game, session.Info.Track);
        }

        session.Records.RemoveAll(r => !validLaps.Contains(r.CurrentLap));

        if (session.Records.Count == 0)
        {
            var filePath = Path.Combine(_telemetryFolder, session.Info.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            Log.Information("Discarded empty session from {Game} on {Track} with no valid laps", session.Info.Game, session.Info.Track);
            return;
        }

        Log.Information("Saved {Game} on {Track} with {Count} valid lap(s)", session.Info.Game, session.Info.Track, validLaps.Count);
        _sessionWriter.WriteFull(session);
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
