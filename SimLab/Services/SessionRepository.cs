using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SimLab.Models;

namespace SimLab.Services;

public class SessionRepository
{
    private readonly ITelemetryService _telemetryService;
    
    private Session? _currentSession;
    private bool _isRecording;

    public ObservableCollection<Session> Sessions { get; private set; } = [];
    
    public bool CanStartRecording => _telemetryService.ConnectionStatus == TelemetryConnectionStatus.Connected;

    public SessionRepository(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        if (_isRecording)
        {
            _ = StopRecordingAsync();
        }
    }

    public async Task StartRecordingAsync()
    {
        if (!CanStartRecording)
        {
            //return;
        }
        
        _isRecording = true;
        _telemetryService.TelemetryReceived += TelemetryServiceOnTelemetryReceived;
    }
    
    public async Task StopRecordingAsync()
    {
        _telemetryService.TelemetryReceived -= TelemetryServiceOnTelemetryReceived;
        _isRecording = false;

        if (_currentSession is not null)
        {
            Sessions.Add(_currentSession);
            _currentSession = null;
        }
        
    }

    private void TelemetryServiceOnTelemetryReceived(object? sender, TelemetryEventArgs e)
    {
        if (_currentSession is null)
        {
            _currentSession = new Session(e.Game, e.Telemetry);
        }

        if (_currentSession.IsNewSession(e.Telemetry))
        {
            Sessions.Add(_currentSession);
            _currentSession = new Session(e.Game, e.Telemetry);
        }
        
        if (_currentSession.IsNewLap(e.Telemetry))
        {
            _currentSession.Laps.Add(new Lap(e.Telemetry.CurrentLap, e.Telemetry));
        }
        
        _currentSession.CurrentLap.Records.Add(e.Telemetry);
    }
}