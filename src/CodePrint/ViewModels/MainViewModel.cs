using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;
using CodePrint.Services;

namespace CodePrint.ViewModels;

public partial class MainViewModel : ObservableObject
{
    /// <summary>1 typographic point = 25.4/72 mm.</summary>
    private const double MmPerPoint = 25.4 / 72.0;

    private readonly UndoRedoService _undoRedo = new();
    private LabelElement? _clipboard;
    private string? _currentFilePath;

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

    public UndoRedoService UndoRedoService => _undoRedo;

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentage));
    }

    partial void OnCurrentDocumentChanged(LabelDocument value)
    {
        OnPropertyChanged(nameof(DocumentTitle));
    }

    /// <summary>Refreshes computed properties that depend on the current document.</summary>
    public void RefreshDocumentProperties()
    {
        OnPropertyChanged(nameof(DocumentTitle));
    }

    // ── File Operations ──

    [RelayCommand]
    private void Save()
    {
        CurrentDocument.ModifiedAt = DateTime.Now;

        // 始终保存到本地标签存储
        Services.LabelStorageService.Save(CurrentDocument);

        if (!string.IsNullOrEmpty(_currentFilePath) && !_currentFilePath.StartsWith("__storage__:"))
        {
            FileService.Save(CurrentDocument, _currentFilePath);
        }

        StatusText = $"已保存 {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>Saves the document to the specified file path.</summary>
    public void SaveToFile(string filePath)
    {
        CurrentDocument.ModifiedAt = DateTime.Now;
        FileService.Save(CurrentDocument, filePath);
        _currentFilePath = filePath;
        StatusText = $"已保存到 {System.IO.Path.GetFileName(filePath)}";
    }

    /// <summary>Loads a document from the specified file path.</summary>
    public void LoadFromFile(string filePath)
    {
        var doc = FileService.Load(filePath);
        CurrentDocument = doc;
        _currentFilePath = filePath;
        SelectedElement = null;
        SelectedElements.Clear();
        _undoRedo.Clear();
        StatusText = $"已加载 {System.IO.Path.GetFileName(filePath)}";
    }

    // ── Undo / Redo ──

    [RelayCommand]
    private void Undo()
    {
        _undoRedo.Undo();
        StatusText = "撤销";
    }

    [RelayCommand]
    private void Redo()
    {
        _undoRedo.Redo();
        StatusText = "恢复";
    }

    // ── Selection ──

    [RelayCommand]
    private void SelectAll()
    {
        SelectedElements.Clear();
        foreach (var element in CurrentDocument.Elements)
            SelectedElements.Add(element);
        if (SelectedElements.Count > 0)
            SelectedElement = SelectedElements[0];
    }

    // ── Clipboard Operations ──

    [RelayCommand]
    private void CopySelected()
    {
        if (SelectedElement != null)
        {
            _clipboard = ElementCloneService.Clone(SelectedElement);
            StatusText = "已复制";
        }
    }

    [RelayCommand]
    private void Paste()
    {
        if (_clipboard != null)
        {
            var clone = ElementCloneService.Clone(_clipboard);
            clone.X += 2;
            clone.Y += 2;
            clone.ZIndex = CurrentDocument.Elements.Count;

            var action = new AddElementAction(CurrentDocument.Elements, clone);
            _undoRedo.Execute(action);
            SelectedElement = clone;
            StatusText = "已粘贴";
        }
    }

    [RelayCommand]
    private void CutSelected()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
        {
            _clipboard = ElementCloneService.Clone(SelectedElement);
            var action = new RemoveElementAction(CurrentDocument.Elements, SelectedElement);
            _undoRedo.Execute(action);
            SelectedElement = null;
            StatusText = "已剪切";
        }
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (SelectedElement != null)
        {
            var clone = ElementCloneService.Clone(SelectedElement);
            clone.X += 2;
            clone.Y += 2;
            clone.ZIndex = CurrentDocument.Elements.Count;

            var action = new AddElementAction(CurrentDocument.Elements, clone);
            _undoRedo.Execute(action);
            SelectedElement = clone;
            StatusText = "已复制元素";
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
        {
            var action = new RemoveElementAction(CurrentDocument.Elements, SelectedElement);
            _undoRedo.Execute(action);
            SelectedElement = null;
        }
    }

    // ── Transform ──

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

    // ── Zoom ──

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.25, 10.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);

    [RelayCommand]
    private void FitToWindow() => ZoomLevel = 1.0;

    // ── Layer Panel ──

    [RelayCommand]
    private void ToggleLayerPanel() => IsLayerPanelVisible = !IsLayerPanelVisible;

    // ── Add Element ──

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
            "Pdf" => new PdfElement { X = 5, Y = 5 },
            "Warning" => new WarningElement { X = 5, Y = 5 },
            "Watermark" => new WatermarkElement { X = 0, Y = 0 },
            _ => null
        };

        if (element != null)
        {
            element.ZIndex = CurrentDocument.Elements.Count;
            var action = new AddElementAction(CurrentDocument.Elements, element);
            _undoRedo.Execute(action);
            SelectedElement = element;
        }
    }

    // ── Layer Ordering ──

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

    // ── Alignment ──

    [RelayCommand]
    private void AlignLeft()
    {
        if (SelectedElements.Count >= 2)
        {
            var minX = SelectedElements.Min(e => e.X);
            foreach (var e in SelectedElements) e.X = minX;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.X = 0;
        }
    }

    [RelayCommand]
    private void AlignRight()
    {
        if (SelectedElements.Count >= 2)
        {
            var maxRight = SelectedElements.Max(e => e.X + e.Width);
            foreach (var e in SelectedElements) e.X = maxRight - e.Width;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.X = Math.Max(0, CurrentDocument.WidthMm - SelectedElement.Width);
        }
    }

    [RelayCommand]
    private void AlignTop()
    {
        if (SelectedElements.Count >= 2)
        {
            var minY = SelectedElements.Min(e => e.Y);
            foreach (var e in SelectedElements) e.Y = minY;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.Y = 0;
        }
    }

    [RelayCommand]
    private void AlignBottom()
    {
        if (SelectedElements.Count >= 2)
        {
            var maxBottom = SelectedElements.Max(e => e.Y + e.Height);
            foreach (var e in SelectedElements) e.Y = maxBottom - e.Height;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.Y = Math.Max(0, CurrentDocument.HeightMm - SelectedElement.Height);
        }
    }

    [RelayCommand]
    private void AlignCenterH()
    {
        if (SelectedElements.Count >= 2)
        {
            var avgCenterX = SelectedElements.Average(e => e.X + e.Width / 2);
            foreach (var e in SelectedElements) e.X = avgCenterX - e.Width / 2;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.X = Math.Max(0, (CurrentDocument.WidthMm - SelectedElement.Width) / 2);
        }
    }

    [RelayCommand]
    private void AlignCenterV()
    {
        if (SelectedElements.Count >= 2)
        {
            var avgCenterY = SelectedElements.Average(e => e.Y + e.Height / 2);
            foreach (var e in SelectedElements) e.Y = avgCenterY - e.Height / 2;
        }
        else if (SelectedElement != null)
        {
            SelectedElement.Y = Math.Max(0, (CurrentDocument.HeightMm - SelectedElement.Height) / 2);
        }
    }

    // ── Distribution ──

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

    // ── Nudge (Arrow key movement) ──

    [RelayCommand]
    private void NudgeLeft()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.X -= Helpers.DesignConstants.NudgeDistance;
    }

    [RelayCommand]
    private void NudgeRight()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.X += Helpers.DesignConstants.NudgeDistance;
    }

    [RelayCommand]
    private void NudgeUp()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.Y -= Helpers.DesignConstants.NudgeDistance;
    }

    [RelayCommand]
    private void NudgeDown()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.Y += Helpers.DesignConstants.NudgeDistance;
    }

    [RelayCommand]
    private void LargeNudgeLeft()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.X -= Helpers.DesignConstants.LargeNudgeDistance;
    }

    [RelayCommand]
    private void LargeNudgeRight()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.X += Helpers.DesignConstants.LargeNudgeDistance;
    }

    [RelayCommand]
    private void LargeNudgeUp()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.Y -= Helpers.DesignConstants.LargeNudgeDistance;
    }

    [RelayCommand]
    private void LargeNudgeDown()
    {
        if (SelectedElement != null && !SelectedElement.IsLocked)
            SelectedElement.Y += Helpers.DesignConstants.LargeNudgeDistance;
    }

    // ── Print (command for keyboard shortcut; opens the print settings dialog) ──

    /// <summary>Raised when the print dialog should be shown.</summary>
    public event Action? RequestShowPrintDialog;

    [RelayCommand]
    private void Print()
    {
        StatusText = "打印...";
        RequestShowPrintDialog?.Invoke();
    }

    // ── Import Image as Text (OCR) ──

    /// <summary>Raised when the OCR import file picker should be shown.</summary>
    public event Action? RequestImportImageAsText;

    [RelayCommand]
    private void ImportImageAsText()
    {
        RequestImportImageAsText?.Invoke();
    }

    /// <summary>
    /// Runs OCR on the specified image and creates TextElement objects
    /// for each recognized line, positioned and sized to match the original layout.
    /// </summary>
    /// <param name="imagePath">Absolute path to the image file.</param>
    /// <param name="targetWidthMm">Target width in mm (usually document width).</param>
    /// <param name="targetHeightMm">Target height in mm (usually document height).</param>
    public async Task ImportImageAsTextAsync(string imagePath, double targetWidthMm, double targetHeightMm)
    {
        StatusText = "正在识别文字…";

        try
        {
            var (lines, imgWidth, imgHeight) = await Services.OcrService.RecognizeAsync(imagePath);

            if (lines.Count == 0)
            {
                StatusText = "未识别到文字";
                return;
            }

            // Scale factor: map image pixel coordinates to document mm coordinates.
            // Fit the image proportionally into the document area.
            double scaleX = targetWidthMm / imgWidth;
            double scaleY = targetHeightMm / imgHeight;
            double scale = Math.Min(scaleX, scaleY);

            foreach (var line in lines)
            {
                // Estimate font size in points from the line height.
                // line.Height is in image pixels; converting to mm via scale,
                // then mm to pt (1pt = 25.4/72 mm).
                double lineHeightMm = line.Height * scale;
                double fontSizePt = lineHeightMm / MmPerPoint;
                fontSizePt = Math.Max(6, Math.Round(fontSizePt, 1));

                var textElement = new TextElement
                {
                    X = line.X * scale,
                    Y = line.Y * scale,
                    Width = line.Width * scale,
                    Height = lineHeightMm,
                    Content = line.Text,
                    FontSize = fontSizePt,
                    ZIndex = CurrentDocument.Elements.Count
                };

                var action = new AddElementAction(CurrentDocument.Elements, textElement);
                _undoRedo.Execute(action);
            }

            SelectedElement = CurrentDocument.Elements.LastOrDefault();
            StatusText = $"已识别 {lines.Count} 行文字";
        }
        catch (Exception ex)
        {
            StatusText = $"识别失败: {ex.Message}";
        }
    }
}
