using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using OpenGrid.Models;
using OpenGrid.ViewModels;

namespace OpenGrid.Controls;

public partial class AddPresetOverrideDialog : UserControl
{
    private static readonly SteamGame AnyGame = SteamGame.Debug with { Name = "Any game", AppId = -1 };
    private static readonly CarProfile AnyCar = new() { Name = "Any car" };

    private readonly Func<string, PresetOverride> _createOverride;

    public IReadOnlyList<SteamGame> Games { get; }
    public IReadOnlyList<CarPickerOption> Cars { get; }
    public IReadOnlyList<PresetOption> Presets { get; }

    public SteamGame? SelectedGame { get; set; }
    public CarPickerOption? SelectedCar { get; set; }
    public PresetOption? SelectedPreset { get; set; }

    public PresetOverride? Result { get; private set; }

    public AddPresetOverrideDialog(
        IEnumerable<SteamGame> games,
        IEnumerable<CarProfile> cars,
        IEnumerable<PresetOption> presets,
        Func<string, PresetOverride> createOverride)
    {
        InitializeComponent();

        _createOverride = createOverride;

        var gameOptions = new List<SteamGame> { AnyGame };
        gameOptions.AddRange(games);
        Games = gameOptions;
        Cars = BuildCarOptions(cars);
        Presets = presets.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        SelectedGame = Games.FirstOrDefault();
        SelectedCar = Cars.FirstOrDefault();
        SelectedPreset = Presets.FirstOrDefault();

        DataContext = this;
        UpdateAddButton();
    }

    private static IReadOnlyList<CarPickerOption> BuildCarOptions(IEnumerable<CarProfile> cars)
    {
        var options = new List<CarPickerOption> { new(AnyCar) };
        options.AddRange(cars.Select(car => new CarPickerOption(car)));
        return options;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == GameComboBox)
        {
            SelectedGame = GameComboBox.SelectedItem as SteamGame;
        }
        else if (sender == CarComboBox)
        {
            SelectedCar = CarComboBox.SelectedItem as CarPickerOption;
        }
        else if (sender == PresetComboBox)
        {
            SelectedPreset = PresetComboBox.SelectedItem as PresetOption;
        }

        UpdateAddButton();
    }

    private void UpdateAddButton()
    {
        if (AddButton is null)
        {
            return;
        }

        var selectedGame = GameComboBox?.SelectedItem as SteamGame ?? SelectedGame;
        var selectedCar = CarComboBox?.SelectedItem as CarPickerOption ?? SelectedCar;
        var selectedPreset = PresetComboBox?.SelectedItem as PresetOption ?? SelectedPreset;

        var hasTarget = selectedGame is { AppId: >= 0 } ||
                        !string.IsNullOrWhiteSpace(selectedCar?.DisplayName);
        AddButton.IsEnabled = hasTarget && selectedPreset is not null;
    }

    private void AddClick(object? sender, RoutedEventArgs e)
    {
        var selectedPreset = PresetComboBox?.SelectedItem as PresetOption ?? SelectedPreset;
        if (selectedPreset is null)
        {
            return;
        }

        var selectedGame = GameComboBox?.SelectedItem as SteamGame ?? SelectedGame;
        var selectedCar = CarComboBox?.SelectedItem as CarPickerOption ?? SelectedCar;

        var gameAppId = selectedGame is { AppId: >= 0 } ? selectedGame.AppId : (int?)null;

        Result = _createOverride(selectedPreset.Id);
        Result.GameAppId = gameAppId;

        // "Any car" is only a picker placeholder. Persisting it as a car name
        // would make the override match nothing, so store null instead.
        var car = selectedCar?.Car;
        Result.Car = ReferenceEquals(car, AnyCar) ? null : car;

        DialogHost.Close(null);
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        DialogHost.Close(null);
    }
}
