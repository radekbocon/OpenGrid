using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Editor for an LED preset. RPM frames are the first supported effect; new
/// effect types can be added here without changing the preset flow.
/// </summary>
public sealed partial class LedConfigurationViewModel : ViewModelBase
{
    public const int MaxLeds = 16;

    private readonly Action _onChanged;
    private bool _isLoading;

    public LedPresetConfiguration Configuration { get; }
    public ObservableCollection<RpmLedSegmentViewModel> Segments { get; } = [];

    public IReadOnlyList<int> LedCountOptions { get; } =
        Enumerable.Range(1, MaxLeds).ToArray();

    [ObservableProperty]
    private int _ledCount;

    public LedConfigurationViewModel(LedPresetConfiguration configuration, Action onChanged)
    {
        Configuration = configuration;
        _onChanged = onChanged;
        _isLoading = true;

        LedCount = Math.Clamp(configuration.LedCount, 1, MaxLeds);
        configuration.LedCount = LedCount;

        foreach (var segment in RpmLedSegment.Normalize(configuration.RpmSegments, LedCount))
        {
            Segments.Add(new RpmLedSegmentViewModel(segment, LedCount, SaveSegments, RemoveSegment));
        }

        ReindexSegments();
        SaveSegments();
        _isLoading = false;
    }

    partial void OnLedCountChanged(int value)
    {
        if (_isLoading)
        {
            return;
        }

        Configuration.LedCount = Math.Clamp(value, 1, MaxLeds);
        foreach (var segment in Segments)
        {
            segment.Resize(LedCount);
        }

        SaveSegments();
    }

    [RelayCommand]
    private void AddSegment()
    {
        Segments.Add(new RpmLedSegmentViewModel(
            new RpmLedSegment(Enumerable.Repeat(RpmLedSegment.OffLed, LedCount), 100),
            LedCount,
            SaveSegments,
            RemoveSegment));

        ReindexSegments();
        SaveSegments();
    }

    private void RemoveSegment(RpmLedSegmentViewModel segment)
    {
        if (Segments.Count <= 1 || !Segments.Remove(segment))
        {
            return;
        }

        ReindexSegments();
        SaveSegments();
    }

    private void SaveSegments()
    {
        Configuration.RpmSegments = Segments.Select(x => x.ToModel()).ToList();
        if (!_isLoading)
        {
            _onChanged();
        }
    }

    private void ReindexSegments()
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            Segments[i].Index = i + 1;
        }
    }
}
