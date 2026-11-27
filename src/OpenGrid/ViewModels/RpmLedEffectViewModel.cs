using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Segmented RPM effect: each frame defines the exact colour of every LED at
/// a percentage of the car's redline.
/// </summary>
public sealed partial class RpmLedEffectViewModel : LedEffectViewModel
{
    private int _ledCount;

    public ObservableCollection<RpmLedSegmentViewModel> Segments { get; } = [];

    public RpmLedEffectViewModel(
        LedEffectSettings settings,
        int ledCount,
        Action onChanged,
        Action<LedEffectViewModel> onRemove)
        : base(settings, onChanged, onRemove)
    {
        _ledCount = ledCount;

        foreach (var segment in RpmLedSegment.Normalize(settings.RpmSegments, ledCount))
        {
            Segments.Add(new RpmLedSegmentViewModel(
                segment,
                ledCount,
                NotifyChanged,
                RemoveSegment,
                DuplicateSegment));
        }

        ReindexSegments();
        SaveSegments();
        CompleteInitialization();
    }

    public override void Resize(int ledCount)
    {
        _ledCount = ledCount;

        foreach (var segment in Segments)
        {
            segment.Resize(ledCount);
        }

        // The configuration view model raises the single change notification
        // after resizing every effect.
        Settings.RpmSegments = Segments.Select(static segment => segment.ToModel()).ToList();
    }

    [RelayCommand]
    private void AddSegment()
    {
        Segments.Add(new RpmLedSegmentViewModel(
            new RpmLedSegment(Enumerable.Repeat(RpmLedSegment.OffLed, _ledCount), 100),
            _ledCount,
            NotifyChanged,
            RemoveSegment,
            DuplicateSegment));

        ReindexSegments();
        SaveSegments();
    }

    private void DuplicateSegment(RpmLedSegmentViewModel segment)
    {
        var index = Segments.IndexOf(segment);
        if (index < 0)
        {
            return;
        }

        Segments.Insert(index + 1, new RpmLedSegmentViewModel(
            segment.ToModel(),
            _ledCount,
            NotifyChanged,
            RemoveSegment,
            DuplicateSegment));

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
        Settings.RpmSegments = Segments.Select(static segment => segment.ToModel()).ToList();
        NotifyChanged();
    }

    private void ReindexSegments()
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            Segments[i].Index = i + 1;
        }
    }
}
