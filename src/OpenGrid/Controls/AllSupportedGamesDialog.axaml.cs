using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using OpenGrid.Models;

namespace OpenGrid.Controls;

public partial class AllSupportedGamesDialog : UserControl
{
    public AllSupportedGamesDialog()
    {
        InitializeComponent();
        GameList.ItemsSource = SteamGame.GetAllSupported();
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close(null);
    }
}
