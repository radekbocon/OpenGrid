using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGrid.Models;

namespace OpenGrid.ViewModels;

public partial class BassShakerConfigurationViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private bool _isLoading;

    public BassShakerConfiguration Configuration { get; }
    public ObservableCollection<BassShakerInputViewModel> BassShakerInputs { get; } = [];
    public ObservableCollection<TelemetryInput> AvailableTelemetryInputs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTelemetryInput))]
    [NotifyCanExecuteChangedFor(nameof(AddBassShakerInputCommand))]
    public partial TelemetryInput? SelectedTelemetryInput { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    public bool HasSelectedTelemetryInput => SelectedTelemetryInput is not null;

    public bool HasBassShakerInputs => BassShakerInputs.Count > 0;

    public BassShakerConfigurationViewModel(BassShakerConfiguration configuration, Action onChanged)
    {
        Configuration = configuration;
        _onChanged = onChanged;
        _isLoading = true;

        foreach (var inputSettings in configuration.Inputs)
        {
            if (inputSettings is not null)
            {
                AddBassShakerInputViewModel(inputSettings);
            }
        }

        Volume = configuration.Volume * 100;
        RefreshAvailableTelemetryInputs();
        _isLoading = false;
    }

    partial void OnVolumeChanged(double value)
    {
        Configuration.Volume = value / 100.0;
        if (_isLoading)
        {
            return;
        }

        _onChanged();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTelemetryInput))]
    private void AddBassShakerInput()
    {
        if (SelectedTelemetryInput is not { } input)
        {
            return;
        }

        var settings = new BassShakerInputSettings { Input = input };
        settings.Mappings.Add(new BassShakerMapping());
        Configuration.Inputs.Add(settings);
        AddBassShakerInputViewModel(settings);

        SelectedTelemetryInput = null;
        RefreshAvailableTelemetryInputs();
        OnPropertyChanged(nameof(HasBassShakerInputs));
        _onChanged();
    }

    private void AddBassShakerInputViewModel(BassShakerInputSettings settings)
    {
        BassShakerInputs.Add(new BassShakerInputViewModel(settings, OnBassShakerInputChanged, OnBassShakerInputRemoved));
    }

    private void OnBassShakerInputRemoved(BassShakerInputViewModel inputViewModel)
    {
        Configuration.Inputs.Remove(inputViewModel.Settings);
        BassShakerInputs.Remove(inputViewModel);
        RefreshAvailableTelemetryInputs();
        OnPropertyChanged(nameof(HasBassShakerInputs));
        _onChanged();
    }

    private void OnBassShakerInputChanged()
    {
        _onChanged();
    }

    private void RefreshAvailableTelemetryInputs()
    {
        AvailableTelemetryInputs.Clear();
        foreach (var input in Enum.GetValues<TelemetryInput>())
        {
            AvailableTelemetryInputs.Add(input);
        }
    }
}
