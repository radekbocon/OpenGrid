using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using ProtoBuf;
using SimLab.Models;

namespace SimLab.Services;

public class SessionRepository
{
    private readonly string _telemetryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimLab", "Telemetry");

    private readonly ITelemetryService _telemetryService;
    
    public Session? CurrentSession { get; private set; }

    public ObservableCollection<Session> Sessions { get; private set; } = [];
    
    public bool IsRecording { get; private set; }

    public SessionRepository(ITelemetryService telemetryService)
    {
        Directory.CreateDirectory(_telemetryFolder);
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
    }

    public void LoadSessions()
    {
        var sessions = new List<Session>();
        
        var files = Directory.GetFiles(_telemetryFolder, "*.bin");

        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var session = Serializer.Deserialize<Session>(stream);
            sessions.Add(session);
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
            Sessions.Add(CurrentSession);
            WriteSession(CurrentSession);
            CurrentSession = null;
        }
        
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (CurrentSession is null)
        {
            CurrentSession = new Session(e.Game, e.Telemetry);
        }

        if (IsNewSession(e.Telemetry))
        {
            WriteSession(CurrentSession);
            Sessions.Add(CurrentSession);
            CurrentSession = new Session(e.Game, e.Telemetry);
            return;
        }
        
        CurrentSession.AddRecord(e.Telemetry);
    }
    
    private void WriteSession(Session session)
    {
        using var file = File.Create(Path.Combine(_telemetryFolder,
            $"{session.Info.Car}-{session.Info.Track}-{session.Info.Type}-{session.Info.Id}.bin"));

        Serializer.Serialize(file, session);
    }
    
    public bool IsNewSession(TelemetryRecord record)
    {
        return record.SessionType != CurrentSession?.Info.Type || record.Track != CurrentSession?.Info.Track || record.Car != CurrentSession?.Info.Car;
    }
}