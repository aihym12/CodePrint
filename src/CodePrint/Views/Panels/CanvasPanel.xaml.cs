using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.ViewModels;

namespace CodePrint.Views.Panels;

public partial class CanvasPanel : UserControl
{
    private static readonly double MmToPx = DesignConstants.MmToPixel;
    private bool _isDragging;
    private Point _dragStart;
    private double _elementStartX;
    private double _elementStartY;

    // Multi-element drag: stores original positions of all selected elements
    private readonly Dictionary<string, (double X, double Y)> _multiDragStartPositions = new();

    // Resize state
    private bool _isResizing;
    private int _resizeHandleIndex = -1; // 0=TL, 1=TR, 2=BL, 3=BR
    private Point _resizeStart;
    private double _resizeStartX;
    private double _resizeStartY;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    // Maps element Id to the rendered visual
    private readonly Dictionary<string, FrameworkElement> _elementVisuals = new();

    // Selection adorner rectangles
    private readonly List<Rectangle> _selectionHandles = new();

    // Inline editing state
    private TextBox? _inlineEditor;
    private LabelElement? _editingElement;

    // Rubber band (marquee) selection state (works with both left-click on blank area and right-click)
    private bool _isRubberBandSelecting;
    private MouseButton _rubberBandButton; // which button started the rubber band
    private Point _rubberBandStart;
    private Rectangle? _rubberBandRect;
    private bool _didRubberBandDrag;
    private const double RubberBandDragThreshold = 3.0;

    public CanvasPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.CurrentDocument.Elements.CollectionChanged -= Elements_CollectionChanged;
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            // Use ObservableCollection for Elements
            if (newVm.CurrentDocument.Elements is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += Elements_CollectionChanged;
            }

            newVm.PropertyChanged += ViewModel_PropertyChanged;
            RefreshAllElements();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedElement))
        {
            UpdateSelectionVisuals();
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentDocument))
        {
            if (ViewModel != null)
            {
                if (ViewModel.CurrentDocument.Elements is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += Elements_CollectionChanged;
                }
            }
            RefreshAllElements();
        }
    }

    private void Elements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (LabelElement element in e.NewItems)
                        AddElementVisual(element);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (LabelElement element in e.OldItems)
                        RemoveElementVisual(element);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                RefreshAllElements();
                break;
        }

        UpdatePlaceholderVisibility();
    }

    private void RefreshAllElements()
    {
        // Unsubscribe from old elements and remove visuals
        if (ViewModel?.CurrentDocument != null)
        {
            foreach (var element in ViewModel.CurrentDocument.Elements)
                element.PropertyChanged -= OnElementPropertyChanged;
        }

        foreach (var visual in _elementVisuals.Values)
            DesignCanvas.Children.Remove(visual);
        _elementVisuals.Clear();

        if (ViewModel?.CurrentDocument == null) return;

        foreach (var element in ViewModel.CurrentDocument.Elements)
            AddElementVisual(element);

        UpdatePlaceholderVisibility();
    }

    private void AddElementVisual(LabelElement element)
    {
        var visual = CanvasRendererHelper.RenderElement(DesignCanvas, element);
        visual.Cursor = Cursors.SizeAll;
        visual.MouseLeftButtonDown += ElementVisual_MouseLeftButtonDown;
        _elementVisuals[element.Id] = visual;

        // Subscribe to property changes so PropertyPanel edits refresh the canvas
        element.PropertyChanged += OnElementPropertyChanged;
    }

    private void RemoveElementVisual(LabelElement element)
    {
        element.PropertyChanged -= OnElementPropertyChanged;

        if (_elementVisuals.TryGetValue(element.Id, out var visual))
        {
            DesignCanvas.Children.Remove(visual);
            _elementVisuals.Remove(element.Id);
        }

        ClearSelectionHandles();
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not LabelElement element) return;
        if (!_elementVisuals.ContainsKey(element.Id)) return;

        var propName = e.PropertyName;

        // During canvas drag we already update the visual manually, skip to avoid loops
        if (_isDragging && (propName == nameof(LabelElement.X) || propName == nameof(LabelElement.Y)))
            return;

        switch (propName)
        {
            case nameof(LabelElement.X):
            case nameof(LabelElement.Y):
                // Position-only change: just move the visual
                if (_elementVisuals.TryGetValue(element.Id, out var posVisual))
                {
                    Canvas.SetLeft(posVisual, element.X * MmToPx);
                    Canvas.SetTop(posVisual, element.Y * MmToPx);
                }
                UpdateSelectionVisuals();
                break;

            case nameof(LabelElement.Opacity):
                if (_elementVisuals.TryGetValue(element.Id, out var opVisual))
                    opVisual.Opacity = element.Opacity;
                break;

            case nameof(LabelElement.ZIndex):
                if (_elementVisuals.TryGetValue(element.Id, out var ziVisual))
                    Panel.SetZIndex(ziVisual, element.ZIndex);
                break;

            case nameof(LabelElement.IsVisible):
                if (_elementVisuals.TryGetValue(element.Id, out var visVisual))
                    visVisual.Visibility = element.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                break;

            default:
                // For all other properties (content, font, colors, size, rotation, etc.)
                // do a full re-render of the element visual
                RefreshElementVisual(element);
                break;
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        PlaceholderText.Visibility = _elementVisuals.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ── Selection & Double-Click ──

    private void ElementVisual_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || ViewModel == null) return;

        var elementId = fe.Tag as string;
        if (elementId == null) return;

        var element = ViewModel.CurrentDocument.Elements.FirstOrDefault(el => el.Id == elementId);
        if (element == null) return;

        // Double-click on text element → enter inline editing
        if (e.ClickCount == 2 && element is TextElement textEl)
        {
            StartInlineEditing(textEl);
            e.Handled = true;
            return;
        }

        // Ctrl+Click → toggle element in multi-selection
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (ViewModel.SelectedElements.Contains(element))
            {
                ViewModel.SelectedElements.Remove(element);
                if (ViewModel.SelectedElement == element)
                    ViewModel.SelectedElement = ViewModel.SelectedElements.Count > 0 ? ViewModel.SelectedElements[0] : null;
            }
            else
            {
                ViewModel.SelectedElements.Add(element);
                ViewModel.SelectedElement = element;
            }
            UpdateSelectionVisuals();
            e.Handled = true;
            return;
        }

        // Click on an element that is already part of multi-selection → keep multi-selection, set primary
        if (ViewModel.SelectedElements.Count > 1 && ViewModel.SelectedElements.Contains(element))
        {
            ViewModel.SelectedElement = element;
            UpdateSelectionVisuals();
        }
        else
        {
            // Normal click → single selection (clear multi-selection)
            ViewModel.SelectedElements.Clear();
            ViewModel.SelectedElements.Add(element);
            ViewModel.SelectedElement = element;
            UpdateSelectionVisuals();
        }

        if (element.IsLocked)
        {
            e.Handled = true;
            return;
        }

        // Start drag (move all selected elements together)
        _isDragging = true;
        _dragStart = e.GetPosition(DesignCanvas);
        _elementStartX = element.X;
        _elementStartY = element.Y;

        // Record start positions for all selected elements (multi-drag)
        _multiDragStartPositions.Clear();
        foreach (var sel in ViewModel.SelectedElements)
        {
            if (!sel.IsLocked)
                _multiDragStartPositions[sel.Id] = (sel.X, sel.Y);
        }

        fe.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Commit any active inline editing first
        CommitInlineEditing();

        if (ViewModel == null) return;

        // Double-click on empty canvas → add text element at click position
        if (e.ClickCount == 2)
        {
            var pos = e.GetPosition(DesignCanvas);
            var xMm = pos.X / MmToPx;
            var yMm = pos.Y / MmToPx;

            ViewModel.AddElementCommand.Execute("Text");
            // Move the newly created element to click position
            if (ViewModel.SelectedElement != null)
            {
                ViewModel.SelectedElement.X = xMm;
                ViewModel.SelectedElement.Y = yMm;

                // Update visual position
                if (_elementVisuals.TryGetValue(ViewModel.SelectedElement.Id, out var visual))
                {
                    Canvas.SetLeft(visual, xMm * MmToPx);
                    Canvas.SetTop(visual, yMm * MmToPx);
                }
                UpdateSelectionVisuals();

                // Immediately start inline editing on the new text element
                if (ViewModel.SelectedElement is TextElement newTextEl)
                {
                    StartInlineEditing(newTextEl);
                }
            }
            e.Handled = true;
            return;
        }

        // Single click on empty area → start left-click rubber band selection
        StartRubberBand(e.GetPosition(DesignCanvas), MouseButton.Left);
        DesignCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizing && ViewModel?.SelectedElement != null)
        {
            HandleResizeMove(e);
            return;
        }

        // Rubber band selection: update rectangle and highlight elements
        if (_isRubberBandSelecting && ViewModel != null)
        {
            UpdateRubberBand(e.GetPosition(DesignCanvas));
            return;
        }

        if (!_isDragging || ViewModel?.SelectedElement == null) return;

        var selectedElement = ViewModel.SelectedElement;
        var pos = e.GetPosition(DesignCanvas);
        var dx = (pos.X - _dragStart.X) / MmToPx;
        var dy = (pos.Y - _dragStart.Y) / MmToPx;

        // Move all selected elements together (multi-drag)
        foreach (var kvp in _multiDragStartPositions)
        {
            var el = ViewModel.CurrentDocument.Elements.FirstOrDefault(e2 => e2.Id == kvp.Key);
            if (el == null) continue;

            el.X = kvp.Value.X + dx;
            el.Y = kvp.Value.Y + dy;

            if (_elementVisuals.TryGetValue(el.Id, out var visual))
            {
                Canvas.SetLeft(visual, el.X * MmToPx);
                Canvas.SetTop(visual, el.Y * MmToPx);
            }
        }

        // Fallback: if primary element was not in multi-drag (e.g. single selection)
        if (!_multiDragStartPositions.ContainsKey(selectedElement.Id))
        {
            selectedElement.X = _elementStartX + dx;
            selectedElement.Y = _elementStartY + dy;

            if (_elementVisuals.TryGetValue(selectedElement.Id, out var visual))
            {
                Canvas.SetLeft(visual, selectedElement.X * MmToPx);
                Canvas.SetTop(visual, selectedElement.Y * MmToPx);
            }
        }

        UpdateSelectionVisuals();
    }

    private void HandleResizeMove(MouseEventArgs e)
    {
        var element = ViewModel!.SelectedElement!;
        var pos = e.GetPosition(DesignCanvas);
        var dx = (pos.X - _resizeStart.X) / MmToPx;
        var dy = (pos.Y - _resizeStart.Y) / MmToPx;

        double newX = _resizeStartX;
        double newY = _resizeStartY;
        double newW = _resizeStartWidth;
        double newH = _resizeStartHeight;

        switch (_resizeHandleIndex)
        {
            case 0: // Top-Left
                newX = _resizeStartX + dx;
                newY = _resizeStartY + dy;
                newW = _resizeStartWidth - dx;
                newH = _resizeStartHeight - dy;
                break;
            case 1: // Top-Right
                newY = _resizeStartY + dy;
                newW = _resizeStartWidth + dx;
                newH = _resizeStartHeight - dy;
                break;
            case 2: // Bottom-Left
                newX = _resizeStartX + dx;
                newW = _resizeStartWidth - dx;
                newH = _resizeStartHeight + dy;
                break;
            case 3: // Bottom-Right
                newW = _resizeStartWidth + dx;
                newH = _resizeStartHeight + dy;
                break;
        }

        // Enforce minimum size (1mm)
        const double minSize = 1.0;
        if (newW < minSize)
        {
            if (_resizeHandleIndex == 0 || _resizeHandleIndex == 2)
                newX = _resizeStartX + _resizeStartWidth - minSize;
            newW = minSize;
        }
        if (newH < minSize)
        {
            if (_resizeHandleIndex == 0 || _resizeHandleIndex == 1)
                newY = _resizeStartY + _resizeStartHeight - minSize;
            newH = minSize;
        }

        element.X = newX;
        element.Y = newY;
        element.Width = newW;
        element.Height = newH;

        // Update visual
        if (_elementVisuals.TryGetValue(element.Id, out var resizeVisual))
        {
            Canvas.SetLeft(resizeVisual, element.X * MmToPx);
            Canvas.SetTop(resizeVisual, element.Y * MmToPx);
        }
        RefreshElementVisual(element);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            _resizeHandleIndex = -1;
            DesignCanvas.ReleaseMouseCapture();
            UpdateSelectionVisuals();
            return;
        }

        // Finish left-click rubber band selection
        if (_isRubberBandSelecting && _rubberBandButton == MouseButton.Left)
        {
            FinishRubberBand();

            // If no drag happened (just a click), deselect everything
            if (!_didRubberBandDrag)
            {
                ViewModel!.SelectedElement = null;
                ViewModel.SelectedElements.Clear();
                ClearSelectionHandles();
            }

            e.Handled = true;
            return;
        }

        if (_isDragging && ViewModel?.SelectedElement != null)
        {
            // Find the element visual and release mouse
            if (_elementVisuals.TryGetValue(ViewModel.SelectedElement.Id, out var visual))
                visual.ReleaseMouseCapture();
        }

        _isDragging = false;
        _multiDragStartPositions.Clear();
    }

    // ── Rubber Band (Marquee) Selection ──
    // Works with both left-click on blank canvas and right-click drag.

    /// <summary>Starts a rubber band selection with the given start position and mouse button.</summary>
    private void StartRubberBand(Point startPosition, MouseButton button)
    {
        _isRubberBandSelecting = true;
        _rubberBandButton = button;
        _didRubberBandDrag = false;
        _rubberBandStart = startPosition;

        // Create rubber band visual
        _rubberBandRect = new Rectangle
        {
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(30, 25, 118, 210)), // semi-transparent blue
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_rubberBandRect, _rubberBandStart.X);
        Canvas.SetTop(_rubberBandRect, _rubberBandStart.Y);
        _rubberBandRect.Width = 0;
        _rubberBandRect.Height = 0;
        Panel.SetZIndex(_rubberBandRect, int.MaxValue);
        DesignCanvas.Children.Add(_rubberBandRect);
    }

    /// <summary>Finishes the rubber band selection, removes the visual, and releases mouse capture.</summary>
    private void FinishRubberBand()
    {
        _isRubberBandSelecting = false;
        DesignCanvas.ReleaseMouseCapture();

        // Remove rubber band visual
        if (_rubberBandRect != null)
        {
            DesignCanvas.Children.Remove(_rubberBandRect);
            _rubberBandRect = null;
        }

        UpdateSelectionVisuals();
    }

    private void Canvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;

        StartRubberBand(e.GetPosition(DesignCanvas), MouseButton.Right);
        DesignCanvas.CaptureMouse();
        e.Handled = true; // Prevent default right-click processing
    }

    private void UpdateRubberBand(Point current)
    {
        if (_rubberBandRect == null) return;

        var x = Math.Min(_rubberBandStart.X, current.X);
        var y = Math.Min(_rubberBandStart.Y, current.Y);
        var w = Math.Abs(current.X - _rubberBandStart.X);
        var h = Math.Abs(current.Y - _rubberBandStart.Y);

        // Mark as drag if moved more than a tiny threshold
        if (w > RubberBandDragThreshold || h > RubberBandDragThreshold)
            _didRubberBandDrag = true;

        Canvas.SetLeft(_rubberBandRect, x);
        Canvas.SetTop(_rubberBandRect, y);
        _rubberBandRect.Width = w;
        _rubberBandRect.Height = h;

        // Build the selection rectangle in mm for hit-testing elements
        var selRect = new Rect(x / MmToPx, y / MmToPx, w / MmToPx, h / MmToPx);

        // Update selected elements in real-time
        ViewModel!.SelectedElements.Clear();
        foreach (var el in ViewModel.CurrentDocument.Elements)
        {
            var elRect = new Rect(el.X, el.Y, el.Width, el.Height);
            if (selRect.IntersectsWith(elRect))
                ViewModel.SelectedElements.Add(el);
        }

        ViewModel.SelectedElement = ViewModel.SelectedElements.Count > 0
            ? ViewModel.SelectedElements[0]
            : null;

        UpdateSelectionVisuals();
    }

    private void Canvas_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isRubberBandSelecting || _rubberBandButton != MouseButton.Right) return;

        FinishRubberBand();

        // Suppress the context menu if the user performed a rubber band drag
        if (_didRubberBandDrag)
        {
            e.Handled = true; // Preview handler: prevents MouseRightButtonUp → no ContextMenu
        }
        // If no drag happened, allow context menu to open normally
    }

    private void Canvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // Suppress the context menu if the user just performed a rubber band drag
        // (safety net – PreviewMouseRightButtonUp already handles this for right-click drags)
        if (_didRubberBandDrag)
        {
            e.Handled = true;
            _didRubberBandDrag = false;
        }
    }

    // ── Drag & Drop from ElementPanel ──

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ElementType"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel == null) return;

        if (e.Data.GetDataPresent("ElementType"))
        {
            var elementType = (string)e.Data.GetData("ElementType");
            var pos = e.GetPosition(DesignCanvas);
            var xMm = pos.X / MmToPx;
            var yMm = pos.Y / MmToPx;

            ViewModel.AddElementCommand.Execute(elementType);

            // Move the newly created element to the drop position
            if (ViewModel.SelectedElement != null)
            {
                ViewModel.SelectedElement.X = xMm;
                ViewModel.SelectedElement.Y = yMm;

                if (_elementVisuals.TryGetValue(ViewModel.SelectedElement.Id, out var visual))
                {
                    Canvas.SetLeft(visual, xMm * MmToPx);
                    Canvas.SetTop(visual, yMm * MmToPx);
                }
                UpdateSelectionVisuals();
            }
        }
    }

    // ── Inline Text Editing ──

    private void StartInlineEditing(TextElement textElement)
    {
        // If already editing, commit first
        CommitInlineEditing();

        _editingElement = textElement;

        // Find and hide the element visual
        if (!_elementVisuals.TryGetValue(textElement.Id, out var visual)) return;
        visual.Visibility = Visibility.Collapsed;

        // Create an editable TextBox overlay at the same position
        _inlineEditor = new TextBox
        {
            Text = textElement.Content,
            FontFamily = new FontFamily(textElement.FontFamily),
            FontSize = textElement.FontSize,
            FontWeight = textElement.IsBold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = textElement.IsItalic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = BrushFromHex(textElement.ForegroundColor),
            Background = textElement.BackgroundColor == "Transparent"
                ? Brushes.Transparent
                : BrushFromHex(textElement.BackgroundColor),
            Width = textElement.Width * MmToPx,
            MinHeight = textElement.Height * MmToPx,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A90D9")),
            Padding = new Thickness(2),
            VerticalContentAlignment = VerticalAlignment.Top
        };

        Canvas.SetLeft(_inlineEditor, textElement.X * MmToPx);
        Canvas.SetTop(_inlineEditor, textElement.Y * MmToPx);
        Panel.SetZIndex(_inlineEditor, int.MaxValue - 1);

        _inlineEditor.LostFocus += InlineEditor_LostFocus;
        _inlineEditor.KeyDown += InlineEditor_KeyDown;

        DesignCanvas.Children.Add(_inlineEditor);

        // Focus and select all text
        _inlineEditor.Focus();
        _inlineEditor.SelectAll();
    }

    private void InlineEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel editing (discard changes)
            CancelInlineEditing();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            // Commit editing on Enter (Shift+Enter for newline)
            CommitInlineEditing();
            e.Handled = true;
        }
    }

    private void InlineEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitInlineEditing();
    }

    private void CommitInlineEditing()
    {
        if (_inlineEditor == null || _editingElement is not TextElement textEl) return;

        // Save the text back to the element model
        var newText = _inlineEditor.Text;
        if (!string.IsNullOrEmpty(newText))
        {
            textEl.Content = newText;
        }

        CleanupInlineEditor();

        // Re-render the element visual with updated text
        RefreshElementVisual(textEl);
    }

    private void CancelInlineEditing()
    {
        if (_inlineEditor == null || _editingElement == null) return;

        CleanupInlineEditor();

        // Show the original visual again without changes
        if (_elementVisuals.TryGetValue(_editingElement.Id, out var visual))
        {
            visual.Visibility = Visibility.Visible;
        }

        _editingElement = null;
    }

    private void CleanupInlineEditor()
    {
        if (_inlineEditor != null)
        {
            _inlineEditor.LostFocus -= InlineEditor_LostFocus;
            _inlineEditor.KeyDown -= InlineEditor_KeyDown;
            DesignCanvas.Children.Remove(_inlineEditor);
            _inlineEditor = null;
        }
        _editingElement = null;
    }

    private void RefreshElementVisual(LabelElement element)
    {
        // Unsubscribe first to prevent double-subscription when AddElementVisual re-subscribes
        element.PropertyChanged -= OnElementPropertyChanged;

        // Remove old visual
        if (_elementVisuals.TryGetValue(element.Id, out var oldVisual))
        {
            oldVisual.MouseLeftButtonDown -= ElementVisual_MouseLeftButtonDown;
            DesignCanvas.Children.Remove(oldVisual);
            _elementVisuals.Remove(element.Id);
        }

        // Re-add with updated data (will re-subscribe to PropertyChanged)
        AddElementVisual(element);
        UpdateSelectionVisuals();
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Black;
        }
    }

    // ── Selection Handles ──

    private void UpdateSelectionVisuals()
    {
        ClearSelectionHandles();

        if (ViewModel == null) return;

        // Draw selection border for all selected elements
        foreach (var sel in ViewModel.SelectedElements)
        {
            if (sel == ViewModel.SelectedElement) continue; // primary element drawn separately with handles

            var sx = sel.X * MmToPx;
            var sy = sel.Y * MmToPx;
            var sw = sel.Width * MmToPx;
            var sh = sel.Height * MmToPx;

            var selBorder = new Rectangle
            {
                Width = sw + 4,
                Height = sh + 4,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(selBorder, sx - 2);
            Canvas.SetTop(selBorder, sy - 2);
            Panel.SetZIndex(selBorder, int.MaxValue - 1);
            DesignCanvas.Children.Add(selBorder);
            _selectionHandles.Add(selBorder);
        }

        // Draw primary selection with resize handles
        if (ViewModel.SelectedElement == null) return;

        var element = ViewModel.SelectedElement;
        var x = element.X * MmToPx;
        var y = element.Y * MmToPx;
        var w = element.Width * MmToPx;
        var h = element.Height * MmToPx;

        // Draw selection border
        var border = new Rectangle
        {
            Width = w + 4,
            Height = h + 4,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(border, x - 2);
        Canvas.SetTop(border, y - 2);
        Panel.SetZIndex(border, int.MaxValue);
        DesignCanvas.Children.Add(border);
        _selectionHandles.Add(border);

        // Draw 4 corner handles with resize cursors
        double handleSize = DesignConstants.HandleSize;
        var corners = new[]
        {
            new Point(x - handleSize / 2, y - handleSize / 2),           // 0: Top-Left
            new Point(x + w - handleSize / 2, y - handleSize / 2),       // 1: Top-Right
            new Point(x - handleSize / 2, y + h - handleSize / 2),       // 2: Bottom-Left
            new Point(x + w - handleSize / 2, y + h - handleSize / 2),   // 3: Bottom-Right
        };

        var cursors = new[] { Cursors.SizeNWSE, Cursors.SizeNESW, Cursors.SizeNESW, Cursors.SizeNWSE };

        for (int i = 0; i < corners.Length; i++)
        {
            var handleIndex = i;
            var handle = new Rectangle
            {
                Width = handleSize,
                Height = handleSize,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
                StrokeThickness = 1.5,
                IsHitTestVisible = !element.IsLocked,
                Cursor = cursors[i],
                Tag = handleIndex
            };

            handle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;

            Canvas.SetLeft(handle, corners[i].X);
            Canvas.SetTop(handle, corners[i].Y);
            Panel.SetZIndex(handle, int.MaxValue);
            DesignCanvas.Children.Add(handle);
            _selectionHandles.Add(handle);
        }
    }

    private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Rectangle handle || ViewModel?.SelectedElement == null) return;
        if (handle.Tag is not int handleIndex) return;

        var element = ViewModel.SelectedElement;
        if (element.IsLocked) return;

        _isResizing = true;
        _resizeHandleIndex = handleIndex;
        _resizeStart = e.GetPosition(DesignCanvas);
        _resizeStartX = element.X;
        _resizeStartY = element.Y;
        _resizeStartWidth = element.Width;
        _resizeStartHeight = element.Height;

        DesignCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void ClearSelectionHandles()
    {
        foreach (var handle in _selectionHandles)
        {
            if (handle.Tag is int)
                handle.MouseLeftButtonDown -= ResizeHandle_MouseLeftButtonDown;
            DesignCanvas.Children.Remove(handle);
        }
        _selectionHandles.Clear();
    }
}
