using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class SessionsViewModel : ViewModelBase
{
    private readonly SessionRepository _sessionRepository;
    
    public ObservableCollection<Session> Sessions => _sessionRepository.Sessions;

    public SessionsViewModel(SessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    [RelayCommand]
    private async Task StartSessionAsync()
    {
        await _sessionRepository.StartRecordingAsync();
    }
    
    [RelayCommand]
    private async Task StopSessionAsync()
    {
        await _sessionRepository.StopRecordingAsync();
    }
}
