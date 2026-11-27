using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Editor for an LED preset. The preset holds an ordered list of effects;
/// currently the segmented RPM effect is implemented.
/// </summary>
public sealed partial class LedConfigurationViewModel : ViewModelBase
{
    public const int MaxLeds = 16;

    private readonly Action _onChanged;
    private bool _isLoading = true;

    public LedPresetConfiguration Configuration { get; }
    public ObservableCollection<LedEffectViewModel> Effects { get; } = [];

    public IReadOnlyList<int> LedCountOptions { get; } =
        Enumerable.Range(1, MaxLeds).ToArray();

    public IReadOnlyList<LedEffectType> AvailableEffectTypes { get; } =
        Enum.GetValues<LedEffectType>();

    [ObservableProperty]
    private int _ledCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEffectType))]
    [NotifyCanExecuteChangedFor(nameof(AddEffectCommand))]
    private LedEffectType? _selectedEffectType;

    public bool HasSelectedEffectType => SelectedEffectType is not null;
    public bool HasEffects => Effects.Count > 0;

    public LedConfigurationViewModel(LedPresetConfiguration configuration, Action onChanged)
    {
        Configuration = configuration;
        _onChanged = onChanged;

        LedCount = Math.Clamp(configuration.LedCount, 1, MaxLeds);
        configuration.LedCount = LedCount;

        foreach (var effect in configuration.Effects.Where(static effect => effect is not null))
        {
            Effects.Add(CreateEffectViewModel(effect));
        }

        _isLoading = false;
    }

    partial void OnLedCountChanged(int value)
    {
        if (_isLoading)
        {
            return;
        }

        Configuration.LedCount = Math.Clamp(value, 1, MaxLeds);
        foreach (var effect in Effects)
        {
            effect.Resize(Configuration.LedCount);
        }

        _onChanged();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedEffectType))]
    private void AddEffect()
    {
        if (SelectedEffectType is not { } type || !Enum.IsDefined(type))
        {
            return;
        }

        var settings = new LedEffectSettings { Type = type };
        if (type == LedEffectType.Rpm)
        {
            settings.RpmSegments = RpmLedSegment.CreateDefault(LedCount);
        }

        Configuration.Effects.Add(settings);
        Effects.Add(CreateEffectViewModel(settings));

        SelectedEffectType = null;
        OnPropertyChanged(nameof(HasEffects));
        _onChanged();
    }

    private LedEffectViewModel CreateEffectViewModel(LedEffectSettings settings) => settings.Type switch
    {
        LedEffectType.Rpm => new RpmLedEffectViewModel(
            settings,
            LedCount,
            OnEffectChanged,
            OnEffectRemoved),
        _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Type, "Unsupported LED effect type."),
    };

    private void OnEffectRemoved(LedEffectViewModel effect)
    {
        Configuration.Effects.Remove(effect.Settings);
        Effects.Remove(effect);
        OnPropertyChanged(nameof(HasEffects));
        _onChanged();
    }

    private void OnEffectChanged() => _onChanged();
}
