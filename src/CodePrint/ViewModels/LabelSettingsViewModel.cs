using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class LabelSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _widthMm = 50;

    [ObservableProperty]
    private double _heightMm = 30;

    [ObservableProperty]
    private PrintOrientation _orientation = PrintOrientation.Portrait;

    [ObservableProperty]
    private double _rowSpacing;

    [ObservableProperty]
    private double _columnSpacing;

    [ObservableProperty]
    private double _marginTop;

    [ObservableProperty]
    private double _marginBottom;

    [ObservableProperty]
    private double _marginLeft;

    [ObservableProperty]
    private double _marginRight;

    [ObservableProperty]
    private int _columnsPerRow = 1;

    [ObservableProperty]
    private string _backgroundColor = "#FFFFFF";

    public void LoadFromDocument(LabelDocument doc)
    {
        WidthMm = doc.WidthMm;
        HeightMm = doc.HeightMm;
        Orientation = doc.Orientation;
        RowSpacing = doc.RowSpacing;
        ColumnSpacing = doc.ColumnSpacing;
        MarginTop = doc.MarginTop;
        MarginBottom = doc.MarginBottom;
        MarginLeft = doc.MarginLeft;
        MarginRight = doc.MarginRight;
        ColumnsPerRow = doc.ColumnsPerRow;
        BackgroundColor = doc.BackgroundColor;
    }

    public void ApplyToDocument(LabelDocument doc)
    {
        doc.WidthMm = WidthMm;
        doc.HeightMm = HeightMm;
        doc.Orientation = Orientation;
        doc.RowSpacing = RowSpacing;
        doc.ColumnSpacing = ColumnSpacing;
        doc.MarginTop = MarginTop;
        doc.MarginBottom = MarginBottom;
        doc.MarginLeft = MarginLeft;
        doc.MarginRight = MarginRight;
        doc.ColumnsPerRow = ColumnsPerRow;
        doc.BackgroundColor = BackgroundColor;
    }

    [RelayCommand]
    private void SwapDimensions()
    {
        (WidthMm, HeightMm) = (HeightMm, WidthMm);
    }
}
