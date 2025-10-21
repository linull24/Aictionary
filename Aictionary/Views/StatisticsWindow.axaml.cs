using System;
using Aictionary.ViewModels;
using Avalonia.Controls;

namespace Aictionary.Views;

public partial class StatisticsWindow : Window
{
    private bool _isInitialized;

    public StatisticsWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_isInitialized)
        {
            return;
        }

        if (DataContext is StatisticsViewModel viewModel)
        {
            try
            {
                await viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[StatisticsWindow] LoadAsync ERROR: {ex.Message}");
            }
        }

        _isInitialized = true;
    }
}
