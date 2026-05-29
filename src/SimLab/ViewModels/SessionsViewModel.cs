using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models.Telemetry;
using SimLab.Services;
using SimLab.Services.Telemetry;

namespace SimLab.ViewModels;

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
        _sessionRepository.IsRecordingChanged += SessionRepositoryOnIsRecordingChanged;
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;

        CanStartRecording = _telemetryService.ConnectionStatus == TelemetryConnectionStatus.Connected;
        IsMenuItem = true;
    }

    private void SessionRepositoryOnIsRecordingChanged(object? sender, bool e)
    {
        OnPropertyChanged(nameof(IsRecording));
        LoadedAsync().FireAndForgetSafe();
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        CanStartRecording = e == TelemetryConnectionStatus.Connected;
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        var sessions = await _sessionRepository.LoadSessionsAsync();
        Sessions = new ObservableCollection<SessionInfo>(sessions);
        OnPropertyChanged(nameof(Sessions));
        OnPropertyChanged(nameof(HasMultipleLapSessions));
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