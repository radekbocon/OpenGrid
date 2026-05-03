using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class SessionsViewModel : ViewModelBase
{
    private readonly SessionRepository _sessionRepository;
    private readonly ITelemetryService _telemetryService;
    private readonly INavigationService _navigationService;
    
    [ObservableProperty]
    public partial bool CanStartRecording { get; private set; } 
    
    public ObservableCollection<Session> Sessions => _sessionRepository.Sessions;
    
    public string CurrentSession => _sessionRepository.CurrentSession is null 
        ? "No session" 
        : $"{_sessionRepository.CurrentSession.Info.Type} | {_sessionRepository.CurrentSession.Info.Car} at {_sessionRepository.CurrentSession.Info.Track}";
    
    public bool IsRecording => _sessionRepository.IsRecording;

    public SessionsViewModel(SessionRepository sessionRepository, ITelemetryService telemetryService, INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _telemetryService = telemetryService;
        _navigationService = navigationService;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
        
        CanStartRecording = _telemetryService.ConnectionStatus == TelemetryConnectionStatus.Connected;
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        CanStartRecording = e == TelemetryConnectionStatus.Connected;
    }
    
    [RelayCommand]
    private void Loaded()
    {
        _sessionRepository.LoadSessions();
        OnPropertyChanged(nameof(Sessions));
    }

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartSessionAsync()
    {
        await _sessionRepository.StartRecordingAsync();
        OnPropertyChanged(nameof(IsRecording));
    }
    
    [RelayCommand]
    private async Task StopSessionAsync()
    {
        await _sessionRepository.StopRecordingAsync();
        OnPropertyChanged(nameof(IsRecording));
    }
    
    [RelayCommand]
    private void SessionSelected(Session session)
    {
        _navigationService.NavigateTo<SessionDetailsViewModel>(session);
    }
}
