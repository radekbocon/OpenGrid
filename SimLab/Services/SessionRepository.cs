using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

public class SessionRepository
{
    private readonly ITelemetryService _telemetryService;
    
    public Session? CurrentSession { get; private set; }

    public ObservableCollection<Session> Sessions { get; private set; } = [];
    
    public bool IsRecording { get; private set; }

    public SessionRepository(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (IsRecording)
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
            CurrentSession = null;
        }
        
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (CurrentSession is null)
        {
            CurrentSession = new Session(e.Game, e.Telemetry);
        }

        if (CurrentSession.IsNewSession(e.Telemetry))
        {
            Sessions.Add(CurrentSession);
            CurrentSession = new Session(e.Game, e.Telemetry);
        }
        
        if (CurrentSession.IsNewLap(e.Telemetry))
        {
            CurrentSession.Laps.Add(new Lap(e.Telemetry.CurrentLap, e.Telemetry));
        }
        
        CurrentSession.CurrentLap.Records.Add(e.Telemetry);
    }
}