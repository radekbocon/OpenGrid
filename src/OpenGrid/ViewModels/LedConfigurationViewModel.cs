using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Editor for an LED preset. The preset holds an ordered list of effects;
/// currently the segmented RPM effect is implemented.
/// </summary>
public sealed partial class LedConfigurationViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Action _onChanged;
    private readonly Dictionary<string, string> _errors = [];
    private bool _isLoading = true;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public LedPresetConfiguration Configuration { get; }
    public ObservableCollection<LedEffectViewModel> Effects { get; } = [];

    public IReadOnlyList<LedEffectType> AvailableEffectTypes { get; } =
        Enum.GetValues<LedEffectType>();

    [ObservableProperty]
    private string _ledCountText = string.Empty;

    public int LedCount => ParseLedCount(LedCountText, Configuration.LedCount);

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

        LedCountText = Math.Max(1, configuration.LedCount).ToString();
        configuration.LedCount = LedCount;

        foreach (var effect in configuration.Effects.Where(static effect => effect is not null))
        {
            Effects.Add(CreateEffectViewModel(effect));
        }

        _isLoading = false;
    }

    public IEnumerable GetErrors(string? propertyName) =>
        propertyName is not null && _errors.TryGetValue(propertyName, out var error)
            ? new[] { error }
            : Array.Empty<string>();

    partial void OnLedCountTextChanged(string value)
    {
        if (_isLoading)
        {
            return;
        }

        if (!int.TryParse(value, out var ledCount) || ledCount < 1)
        {
            SetError(nameof(LedCountText), "Enter a whole number greater than 0");
            return;
        }

        SetError(nameof(LedCountText), null);
        Configuration.LedCount = ledCount;
        foreach (var effect in Effects)
        {
            effect.Resize(ledCount);
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

    private static int ParseLedCount(string? value, int fallback) =>
        int.TryParse(value, out var count) && count > 0 ? count : Math.Max(1, fallback);

    private void SetError(string propertyName, string? error)
    {
        if (error is null)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }

            return;
        }

        if (_errors.TryGetValue(propertyName, out var current) && current == error)
        {
            return;
        }

        _errors[propertyName] = error;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
