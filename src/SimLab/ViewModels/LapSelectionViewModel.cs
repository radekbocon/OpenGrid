using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    private readonly Session? _initialSession;

    [ObservableProperty]
    public partial ObservableCollection<SelectableSession> Sessions { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    public partial int SelectedCount { get; set; }

    public bool CanCompare => SelectedCount >= 2;

    public LapSelectionViewModel(SessionRepository sessionRepository, INavigationService navigationService, Session? initialSession = null)
    {
        _sessionRepository = sessionRepository;
        _navigationService = navigationService;
        _initialSession = initialSession;
        
        LoadSessions();
    }

    private void LoadSessions()
    {
        Sessions.Clear();
        
        var sessions = _sessionRepository.Sessions
            .Where(s => s.Laps.Count > 0)
            .OrderByDescending(s => s.Info.StartTime);

        foreach (var session in sessions)
        {
            var selectableSession = new SelectableSession(session);
            
            if (_initialSession != null && session.Info.Id == _initialSession.Info.Id)
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
    private void Compare()
    {
        var selectedLaps = Sessions
            .SelectMany(s => s.Laps.Where(l => l.IsSelected)
                .Select(l => (Lap: l.Lap, Session: l.Session)))
            .OrderBy(x => x.Lap.Time)
            .ToList();

        var unsortedItems = new List<(Models.Telemetry.Lap Lap, SessionInfo Info)>();
        var detailsCache = new Dictionary<Session, SessionDetails>();

        foreach (var (lap, session) in selectedLaps)
        {
            if (!detailsCache.TryGetValue(session, out var details))
            {
                details = _sessionRepository.LoadSessionDetails(session);
                detailsCache[session] = details;
            }

            var freshLap = details.Laps.FirstOrDefault(l => l.Number == lap.Number) ?? lap;
            unsortedItems.Add((freshLap, session.Info));
        }

        var sortedItems = unsortedItems.OrderBy(x => x.Lap.Time).ToList();
        var comparisonItems = sortedItems
            .Select((item, i) => new LapComparisonItem(item.Lap, item.Info, i))
            .ToList();

        _navigationService.NavigateTo<LapComparisonViewModel>(comparisonItems);
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
    public Lap Lap { get; }
    public Session Session { get; }
    public string DisplayName => $"Lap {Lap.Number} ({LapTimeConverter.Format(Lap.Time)})";
    public bool IsValid => Lap.IsValid;
    public bool IsFastest => Lap.IsFastest;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SelectableLap(Lap lap, Session session)
    {
        Lap = lap;
        Session = session;
    }
}

public partial class SelectableSession : ObservableObject
{
    public Session Session { get; }
    public ObservableCollection<SelectableLap> Laps { get; }
    public string DisplayName => $"{Session.Info.Car} at {Session.Info.Track} - {Session.Info.StartTime:g}";
    public int LapCount => Laps.Count;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public SelectableSession(Session session)
    {
        Session = session;
        Laps = new ObservableCollection<SelectableLap>(
            session.Laps.Select(l => new SelectableLap(l, session)));
    }
}
