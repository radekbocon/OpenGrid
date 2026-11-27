using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using OpenGrid.Controls;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

/// <summary>
/// Shared shell for an LED effect: name, enable toggle and remove action.
/// Effect-specific settings live in the derived view model and its matching
/// view, resolved by <see cref="ViewLocator"/>.
/// </summary>
public abstract partial class LedEffectViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private readonly Action<LedEffectViewModel> _onRemove;
    private bool _isLoading = true;

    public LedEffectSettings Settings { get; }
    public LedEffectType EffectType => Settings.Type;
    public string EffectName => Settings.Type.GetDisplayName();

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    protected LedEffectViewModel(
        LedEffectSettings settings,
        Action onChanged,
        Action<LedEffectViewModel> onRemove)
    {
        Settings = settings;
        _onChanged = onChanged;
        _onRemove = onRemove;
        IsEnabled = settings.IsEnabled;
    }

    /// <summary>Call after a derived view model has loaded its settings.</summary>
    protected void CompleteInitialization() => _isLoading = false;

    protected void NotifyChanged()
    {
        if (!_isLoading)
        {
            _onChanged();
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        Settings.IsEnabled = value;
        NotifyChanged();
    }

    /// <summary>Adapts frames to a new preset-level LED count.</summary>
    public virtual void Resize(int ledCount)
    {
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        var confirmDialog = new ConfirmDialog("Remove this effect?");
        await DialogHost.Show(confirmDialog);

        if (!confirmDialog.Result)
        {
            return;
        }

        _onRemove(this);
    }
}
