using System;
using Aictionary.ViewModels;
using Avalonia.Controls;

namespace Aictionary.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
            viewModel.OpenStatisticsRequested += OnOpenStatisticsRequested;
        }
    }

    private async void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        var settingsWindow = App.CreateSettingsWindow();
        await settingsWindow.ShowDialog(this);
    }

    private async void OnOpenStatisticsRequested(object? sender, EventArgs e)
    {
        var statisticsWindow = App.CreateStatisticsWindow();
        await statisticsWindow.ShowDialog(this);
    }
}
