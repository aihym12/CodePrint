using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class PrintViewModel : ObservableObject
{
    [ObservableProperty]
    private PrintSettings _settings = new();

    [ObservableProperty]
    private ObservableCollection<string> _availablePrinters = new();

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private LabelDocument? _document;

    [ObservableProperty]
    private bool _isPreviewMode;

    public int TotalLabels => Settings.LabelsPerRow * Settings.LabelsPerColumn;

    [RelayCommand]
    private void RefreshPrinters()
    {
        AvailablePrinters.Clear();
        AvailablePrinters.Add("Microsoft Print to PDF");
    }

    [RelayCommand]
    private void Print()
    {
        if (Document == null || string.IsNullOrEmpty(SelectedPrinter)) return;
        Settings.PrinterName = SelectedPrinter;
    }

    [RelayCommand]
    private void Preview() => IsPreviewMode = !IsPreviewMode;

    [RelayCommand]
    private void Cancel() { }
}
