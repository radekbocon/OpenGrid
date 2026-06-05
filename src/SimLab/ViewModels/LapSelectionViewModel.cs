using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using SimLab.Converters;
using SimLab.Models;
using SimLab.Models.Telemetry;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class LapSelectionViewModel : ObservableObject
{
    private readonly SessionRepository _sessionRepository;
    private readonly INavigationService _navigationService;
    private readonly SessionDetails? _initialSession;

    [ObservableProperty]
    public partial ObservableCollection<SelectableSession> Sessions { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    public partial int SelectedCount { get; set; }

    public bool CanCompare => SelectedCount >= 2;

    public LapSelectionViewModel(SessionRepository sessionRepository, INavigationService navigationService, SessionDetails? initialSession = null)
    {
        _sessionRepository = sessionRepository;
        _navigationService = navigationService;
        _initialSession = initialSession;
        
        _ = LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        Sessions.Clear();

        var sessions = await _sessionRepository.LoadSessionsAsync();

        foreach (var session in sessions)
        {
            var selectableSession = new SelectableSession(session);
            
            if (_initialSession != null && session.Id == _initialSession.Info.Id)
            {
                selectableSession.IsExpanded = true;
            }

            foreach (var lap in selectableSession.Laps)
            {
                lap.PropertyChanged += LapOnPropertyChanged;
            }
            
            Sessions.Add(selectableSession);
        }
    }

    private void LapOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableLap.IsSelected))
        {
            UpdateSelectedCount();
        }
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Sessions
            .SelectMany(s => s.Laps)
            .Count(l => l.IsSelected);
        
        OnPropertyChanged(nameof(CanCompare));
    }

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync()
    {
        var selectedLaps = Sessions
            .SelectMany(s => s.Laps.Where(l => l.IsSelected)
                .Select(l => (l.Lap, l.Session)))
            .OrderBy(x => x.Lap.Time)
            .ToList();

        var unsortedItems = new List<(Lap Lap, SessionInfo Info)>();

        foreach (var (lap, session) in selectedLaps)
        {
            var details = _sessionRepository.LoadSessionDetails(session);
            var freshLap = details.Laps.First(l => l.Number == lap.Number);
            unsortedItems.Add((freshLap, session));
        }

        var sortedItems = unsortedItems.OrderBy(x => x.Lap.Time).ToList();
        var comparisonItems = sortedItems
            .Select((item, i) => new LapComparisonItem(item.Lap, item.Info, i))
            .ToList();

        _navigationService.NavigateTo<SessionDetailsViewModel>(comparisonItems);
        DialogHost.Close(null);
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogHost.Close(null);
    }
}

public partial class SelectableLap : ObservableObject
{
    public LapInfo Lap { get; }
    public SessionInfo Session { get; }
    public string DisplayName => $"Lap {Lap.Number} ({LapTimeConverter.Format(Lap.Time)})";
    public bool IsValid => Lap.IsValid;
    public bool IsFastest => Lap.IsFastest;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SelectableLap(LapInfo lap, SessionInfo session)
    {
        Lap = lap;
        Session = session;
    }
}

public partial class SelectableSession : ObservableObject
{
    public SessionInfo Session { get; }
    public ObservableCollection<SelectableLap> Laps { get; }
    public string DisplayName => $"{Session.Car} at {Session.Track} - {Session.StartTime:g}";
    public int LapCount => Laps.Count;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public SelectableSession(SessionInfo session)
    {
        Session = session;
        Laps = new ObservableCollection<SelectableLap>(
            session.LapInfo.Select(l => new SelectableLap(l, session)));
    }
}
