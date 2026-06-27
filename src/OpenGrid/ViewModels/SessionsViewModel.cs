using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models.Telemetry;
using OpenGrid.Services;
using OpenGrid.Services.SessionPersist;
using OpenGrid.Services.Telemetry;

namespace OpenGrid.ViewModels;

public partial class SessionsViewModel : ViewModelBase
{
    private readonly SessionRepository _sessionRepository;
    private readonly ITelemetryService _telemetryService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] public partial bool CanStartRecording { get; private set; }

    public ObservableCollection<SessionInfo> Sessions { get; private set; } = [];

    public bool HasMultipleLapSessions => Sessions.Any(s => s.LapInfo.Count > 0);

    public bool IsRecording => _sessionRepository.IsRecording;

    public SessionsViewModel(SessionRepository sessionRepository, ITelemetryService telemetryService,
        INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _telemetryService = telemetryService;
        _navigationService = navigationService;

        CanStartRecording = _telemetryService.ConnectionStatus == TelemetryConnectionStatus.Connected;
        IsMenuItem = true;
    }

    private void SessionRepositoryOnIsRecordingChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsRecording));
        
        // Reload sessions when recording stops
        if (!IsRecording)
        {
            GetSessionsAsync().FireAndForgetSafe();
        }
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        CanStartRecording = e == TelemetryConnectionStatus.Connected;
    }

    protected override async Task OnLoadedAsync()
    {
        _sessionRepository.IsRecordingChanged += SessionRepositoryOnIsRecordingChanged;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;
        await GetSessionsAsync();
        
        await base.OnLoadedAsync();
    }
    
    protected override Task OnUnloadedAsync()
    {
        _sessionRepository.IsRecordingChanged -= SessionRepositoryOnIsRecordingChanged;
        _telemetryService.TelemetryStatusChanged -= TelemetryServiceOnTelemetryStatusChanged;
        
        return base.OnUnloadedAsync();
    }
    
    private async Task GetSessionsAsync()
    {
        var sessions = await _sessionRepository.LoadSessionsAsync();
        Sessions = new ObservableCollection<SessionInfo>(sessions);
        OnPropertyChanged(nameof(Sessions));
        OnPropertyChanged(nameof(HasMultipleLapSessions));
    }

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private void StartSession()
    {
        _sessionRepository.StartRecording();
        OnPropertyChanged(nameof(IsRecording));
    }

    [RelayCommand]
    private void StopSession()
    {
        _sessionRepository.StopRecording();
        OnPropertyChanged(nameof(IsRecording));
    }

    [RelayCommand]
    private void SessionSelected(SessionInfo session)
    {
        _navigationService.NavigateTo<SessionDetailsViewModel>(session);
    }

    [RelayCommand]
    private void OpenLapComparison()
    {
        var viewModel = new LapSelectionViewModel(_sessionRepository, _navigationService);
        var dialog = new LapSelectionDialog { DataContext = viewModel };
        DialogHost.Show(dialog);
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(SessionInfo session)
    {
        var confirmDialog = new ConfirmDialog("Are you sure you want to delete this session?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            _sessionRepository.DeleteSession(session.FileName);
            OnPropertyChanged(nameof(HasMultipleLapSessions));
            Sessions.Remove(session);
        }
    }
}