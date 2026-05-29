using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Controls;
using SimLab.Models;
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

    public ObservableCollection<Session> Sessions => _sessionRepository.Sessions;

    public bool HasMultipleLapSessions => Sessions.Any(s => s.Laps.Count > 0);

    public bool IsRecording => _sessionRepository.IsRecording;

    public SessionsViewModel(SessionRepository sessionRepository, ITelemetryService telemetryService,
        INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _telemetryService = telemetryService;
        _navigationService = navigationService;
        _sessionRepository.IsRecordingChanged += (_, _) => OnPropertyChanged(nameof(IsRecording));
        _telemetryService.TelemetryStatusChanged += TelemetryServiceOnTelemetryStatusChanged;

        CanStartRecording = _telemetryService.ConnectionStatus == TelemetryConnectionStatus.Connected;
        IsMenuItem = true;
    }

    private void TelemetryServiceOnTelemetryStatusChanged(object? sender, TelemetryConnectionStatus e)
    {
        CanStartRecording = e == TelemetryConnectionStatus.Connected;
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await _sessionRepository.LoadSessionsAsync();
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
    private void SessionSelected(Session session)
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
    private async Task DeleteSessionAsync(Session session)
    {
        var confirmDialog = new ConfirmDialog("Are you sure you want to delete this session?");
        await DialogHost.Show(confirmDialog);

        if (confirmDialog.Result)
        {
            _sessionRepository.DeleteSession(session);
            OnPropertyChanged(nameof(HasMultipleLapSessions));
        }
    }
}