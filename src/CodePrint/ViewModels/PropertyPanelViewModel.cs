using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class PropertyPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private LabelElement? _selectedElement;

    [ObservableProperty]
    private double _elementX;

    [ObservableProperty]
    private double _elementY;

    [ObservableProperty]
    private double _elementWidth;

    [ObservableProperty]
    private double _elementHeight;

    [ObservableProperty]
    private double _elementRotation;

    [ObservableProperty]
    private double _elementOpacity = 1.0;

    [ObservableProperty]
    private bool _isAspectRatioLocked;

    private double _aspectRatio = 1.0;

    partial void OnSelectedElementChanged(LabelElement? value)
    {
        if (value == null) return;
        ElementX = value.X;
        ElementY = value.Y;
        ElementWidth = value.Width;
        ElementHeight = value.Height;
        ElementRotation = value.Rotation;
        ElementOpacity = value.Opacity;
        if (value.Width > 0 && value.Height > 0)
            _aspectRatio = value.Width / value.Height;
    }

    partial void OnElementXChanged(double value)
    {
        if (SelectedElement != null) SelectedElement.X = value;
    }

    partial void OnElementYChanged(double value)
    {
        if (SelectedElement != null) SelectedElement.Y = value;
    }

    partial void OnElementWidthChanged(double value)
    {
        if (SelectedElement != null)
        {
            SelectedElement.Width = value;
            if (IsAspectRatioLocked && _aspectRatio > 0)
                ElementHeight = value / _aspectRatio;
        }
    }

    partial void OnElementHeightChanged(double value)
    {
        if (SelectedElement != null)
        {
            SelectedElement.Height = value;
            if (IsAspectRatioLocked && _aspectRatio > 0)
                ElementWidth = value * _aspectRatio;
        }
    }

    partial void OnElementRotationChanged(double value)
    {
        if (SelectedElement != null) SelectedElement.Rotation = value;
    }

    partial void OnElementOpacityChanged(double value)
    {
        if (SelectedElement != null) SelectedElement.Opacity = value;
    }

    [RelayCommand]
    private void ToggleAspectRatio() => IsAspectRatioLocked = !IsAspectRatioLocked;
}
