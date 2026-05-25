using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimLab.Models;
using SimLab.Services;

namespace SimLab.ViewModels;

public partial class SelectableLap : ObservableObject
{
    private bool _isSelected;

    public Lap Lap { get; }
    public Session Session { get; }
    public string DisplayName => $"Lap {Lap.Number} ({Lap.Time:mm\\:ss\\.fff})";
    public bool IsValid => Lap.IsValid;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SelectableLap(Lap lap, Session session)
    {
        Lap = lap;
        Session = session;
    }
}

public partial class SelectableSession : ObservableObject
{
    private bool _isExpanded = true;

    public Session Session { get; }
    public ObservableCollection<SelectableLap> Laps { get; }
    public string DisplayName => $"{Session.Info.Car} at {Session.Info.Track} - {Session.Info.StartTime:g}";
    public int LapCount => Laps.Count;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public SelectableSession(Session session)
    {
        Session = session;
        Laps = new ObservableCollection<SelectableLap>(
            session.Laps.Select(l => new SelectableLap(l, session)));
    }
}

public partial class LapSelectionViewModel : ViewModelBase
{
    private readonly SessionRepository _sessionRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    public partial ObservableCollection<SelectableSession> Sessions { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    public bool CanCompare => SelectedCount >= 2;

    public LapSelectionViewModel(SessionRepository sessionRepository, INavigationService navigationService)
    {
        _sessionRepository = sessionRepository;
        _navigationService = navigationService;
    }

    public override void SetParameters(params object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Session session)
        {
            LoadSessions(session);
        }
        else
        {
            LoadSessions();
        }
    }

    private void LoadSessions(Session? selectedSession = null)
    {
        Sessions.Clear();
        
        var sessions = _sessionRepository.Sessions
            .Where(s => s.Laps.Count > 0)
            .OrderByDescending(s => s.Info.StartTime);

        foreach (var session in sessions)
        {
            var selectableSession = new SelectableSession(session);
            
            if (selectedSession != null && session.Info.Id == selectedSession.Info.Id)
            {
                selectableSession.IsExpanded = true;
            }

            foreach (var lap in selectableSession.Laps)
            {
                lap.PropertyChanged += Lap_PropertyChanged;
            }
            
            Sessions.Add(selectableSession);
        }
    }

    private void Lap_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        CompareSelectedCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Loaded()
    {
        if (Sessions.Count == 0)
        {
            _sessionRepository.LoadSessions();
            LoadSessions();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private void CompareSelected()
    {
        var selectedLaps = Sessions
            .SelectMany(s => s.Laps.Where(l => l.IsSelected)
                .Select(l => (l.Lap, l.Session.Info)))
            .OrderBy(x => x.Lap.Time)
            .ToList();

        var comparisonItems = new List<LapComparisonItem>();
        for (var i = 0; i < selectedLaps.Count; i++)
        {
            comparisonItems.Add(new LapComparisonItem(selectedLaps[i].Lap, selectedLaps[i].Info, i));
        }

        _navigationService.NavigateTo<LapComparisonViewModel>(comparisonItems);
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
