using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private LabelDocument _currentDocument = new();

    [ObservableProperty]
    private LabelElement? _selectedElement;

    [ObservableProperty]
    private ObservableCollection<LabelElement> _selectedElements = new();

    /// <summary>Default zoom level matching ~308% as shown in the PRD reference UI.</summary>
    [ObservableProperty]
    private double _zoomLevel = 3.08;

    [ObservableProperty]
    private bool _isDesignMode = true;

    [ObservableProperty]
    private bool _isLayerPanelVisible;

    [ObservableProperty]
    private UserProfile _currentUser = new() { Nickname = "用户", Phone = "13400007122" };

    [ObservableProperty]
    private string _statusText = string.Empty;

    public string ZoomPercentage => $"{ZoomLevel * 100:F0}%";

    public string DocumentTitle => $"{CurrentDocument.Name} {CurrentDocument.WidthMm}×{CurrentDocument.HeightMm}mm";

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentage));
    }

    partial void OnCurrentDocumentChanged(LabelDocument value)
    {
        OnPropertyChanged(nameof(DocumentTitle));
    }

    [RelayCommand]
    private void Save()
    {
        CurrentDocument.ModifiedAt = DateTime.Now;
        StatusText = $"已保存 {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    private void Undo() => StatusText = "撤销";

    [RelayCommand]
    private void Redo() => StatusText = "恢复";

    [RelayCommand]
    private void SelectAll()
    {
        SelectedElements.Clear();
        foreach (var element in CurrentDocument.Elements)
            SelectedElements.Add(element);
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
        {
            CurrentDocument.Elements.Remove(SelectedElement);
            SelectedElement = null;
        }
    }

    [RelayCommand]
    private void CopySelected() => StatusText = "已复制";

    [RelayCommand]
    private void Paste() => StatusText = "已粘贴";

    [RelayCommand]
    private void RotateSelected()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.Rotation = (SelectedElement.Rotation + 90) % 360;
    }

    [RelayCommand]
    private void ToggleLock()
    {
        if (SelectedElement != null)
            SelectedElement.IsLocked = !SelectedElement.IsLocked;
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.25, 10.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);

    [RelayCommand]
    private void FitToWindow() => ZoomLevel = 1.0;

    [RelayCommand]
    private void ToggleLayerPanel() => IsLayerPanelVisible = !IsLayerPanelVisible;

    [RelayCommand]
    private void AddElement(string elementType)
    {
        LabelElement? element = elementType switch
        {
            "Text" => new TextElement { X = 5, Y = 5 },
            "Barcode" => new BarcodeElement { X = 5, Y = 5 },
            "QrCode" => new QrCodeElement { X = 5, Y = 5 },
            "LinkedQrCode" => new LinkedQrCodeElement { X = 5, Y = 5 },
            "Image" => new ImageElement { X = 5, Y = 5 },
            "Icon" => new IconElement { X = 5, Y = 5 },
            "Line" => new LineElement { X = 5, Y = 5 },
            "Rectangle" => new RectangleElement { X = 5, Y = 5 },
            "Date" => new DateElement { X = 5, Y = 5 },
            "Table" => new TableElement { X = 5, Y = 5 },
            "Warning" => new WarningElement { X = 5, Y = 5 },
            "Watermark" => new WatermarkElement { X = 0, Y = 0 },
            _ => null
        };

        if (element != null)
        {
            element.ZIndex = CurrentDocument.Elements.Count;
            CurrentDocument.Elements.Add(element);
            SelectedElement = element;
        }
    }

    [RelayCommand]
    private void BringToFront()
    {
        if (SelectedElement == null) return;
        var maxZ = CurrentDocument.Elements.Count > 0 ? CurrentDocument.Elements.Max(e => e.ZIndex) : 0;
        SelectedElement.ZIndex = maxZ + 1;
    }

    [RelayCommand]
    private void SendToBack()
    {
        if (SelectedElement == null) return;
        var minZ = CurrentDocument.Elements.Count > 0 ? CurrentDocument.Elements.Min(e => e.ZIndex) : 0;
        SelectedElement.ZIndex = minZ - 1;
    }

    [RelayCommand]
    private void MoveLayerUp()
    {
        if (SelectedElement != null) SelectedElement.ZIndex++;
    }

    [RelayCommand]
    private void MoveLayerDown()
    {
        if (SelectedElement != null) SelectedElement.ZIndex--;
    }

    [RelayCommand]
    private void AlignLeft()
    {
        if (SelectedElements.Count < 2) return;
        var minX = SelectedElements.Min(e => e.X);
        foreach (var e in SelectedElements) e.X = minX;
    }

    [RelayCommand]
    private void AlignRight()
    {
        if (SelectedElements.Count < 2) return;
        var maxRight = SelectedElements.Max(e => e.X + e.Width);
        foreach (var e in SelectedElements) e.X = maxRight - e.Width;
    }

    [RelayCommand]
    private void AlignTop()
    {
        if (SelectedElements.Count < 2) return;
        var minY = SelectedElements.Min(e => e.Y);
        foreach (var e in SelectedElements) e.Y = minY;
    }

    [RelayCommand]
    private void AlignBottom()
    {
        if (SelectedElements.Count < 2) return;
        var maxBottom = SelectedElements.Max(e => e.Y + e.Height);
        foreach (var e in SelectedElements) e.Y = maxBottom - e.Height;
    }

    [RelayCommand]
    private void AlignCenterH()
    {
        if (SelectedElements.Count < 2) return;
        var avgCenterX = SelectedElements.Average(e => e.X + e.Width / 2);
        foreach (var e in SelectedElements) e.X = avgCenterX - e.Width / 2;
    }

    [RelayCommand]
    private void AlignCenterV()
    {
        if (SelectedElements.Count < 2) return;
        var avgCenterY = SelectedElements.Average(e => e.Y + e.Height / 2);
        foreach (var e in SelectedElements) e.Y = avgCenterY - e.Height / 2;
    }

    [RelayCommand]
    private void DistributeH()
    {
        if (SelectedElements.Count < 3) return;
        var sorted = SelectedElements.OrderBy(e => e.X).ToList();
        var totalWidth = sorted.Sum(e => e.Width);
        var totalSpace = sorted.Last().X + sorted.Last().Width - sorted.First().X - totalWidth;
        var gap = totalSpace / (sorted.Count - 1);
        var currentX = sorted.First().X + sorted.First().Width + gap;
        for (int i = 1; i < sorted.Count - 1; i++)
        {
            sorted[i].X = currentX;
            currentX += sorted[i].Width + gap;
        }
    }

    [RelayCommand]
    private void DistributeV()
    {
        if (SelectedElements.Count < 3) return;
        var sorted = SelectedElements.OrderBy(e => e.Y).ToList();
        var totalHeight = sorted.Sum(e => e.Height);
        var totalSpace = sorted.Last().Y + sorted.Last().Height - sorted.First().Y - totalHeight;
        var gap = totalSpace / (sorted.Count - 1);
        var currentY = sorted.First().Y + sorted.First().Height + gap;
        for (int i = 1; i < sorted.Count - 1; i++)
        {
            sorted[i].Y = currentY;
            currentY += sorted[i].Height + gap;
        }
    }
}
