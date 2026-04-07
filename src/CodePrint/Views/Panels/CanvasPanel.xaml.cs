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

    // Maps element Id to the rendered visual
    private readonly Dictionary<string, FrameworkElement> _elementVisuals = new();

    // Selection adorner rectangles
    private readonly List<Rectangle> _selectionHandles = new();

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
        // Remove old visuals
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
    }

    private void RemoveElementVisual(LabelElement element)
    {
        if (_elementVisuals.TryGetValue(element.Id, out var visual))
        {
            DesignCanvas.Children.Remove(visual);
            _elementVisuals.Remove(element.Id);
        }

        ClearSelectionHandles();
    }

    private void UpdatePlaceholderVisibility()
    {
        PlaceholderText.Visibility = _elementVisuals.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ── Selection ──

    private void ElementVisual_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || ViewModel == null) return;

        var elementId = fe.Tag as string;
        if (elementId == null) return;

        var element = ViewModel.CurrentDocument.Elements.FirstOrDefault(el => el.Id == elementId);
        if (element == null) return;

        ViewModel.SelectedElement = element;
        UpdateSelectionVisuals();

        if (element.IsLocked)
        {
            e.Handled = true;
            return;
        }

        // Start drag
        _isDragging = true;
        _dragStart = e.GetPosition(DesignCanvas);
        _elementStartX = element.X;
        _elementStartY = element.Y;
        fe.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Click on empty canvas area → deselect
        if (ViewModel != null)
        {
            ViewModel.SelectedElement = null;
            ViewModel.SelectedElements.Clear();
            ClearSelectionHandles();
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || ViewModel?.SelectedElement == null) return;

        var pos = e.GetPosition(DesignCanvas);
        var dx = (pos.X - _dragStart.X) / MmToPx;
        var dy = (pos.Y - _dragStart.Y) / MmToPx;

        ViewModel.SelectedElement.X = _elementStartX + dx;
        ViewModel.SelectedElement.Y = _elementStartY + dy;

        // Update visual position
        if (_elementVisuals.TryGetValue(ViewModel.SelectedElement.Id, out var visual))
        {
            Canvas.SetLeft(visual, ViewModel.SelectedElement.X * MmToPx);
            Canvas.SetTop(visual, ViewModel.SelectedElement.Y * MmToPx);
        }

        UpdateSelectionVisuals();
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && ViewModel?.SelectedElement != null)
        {
            // Find the element visual and release mouse
            if (_elementVisuals.TryGetValue(ViewModel.SelectedElement.Id, out var visual))
                visual.ReleaseMouseCapture();
        }

        _isDragging = false;
    }

    // ── Selection Handles ──

    private void UpdateSelectionVisuals()
    {
        ClearSelectionHandles();

        if (ViewModel?.SelectedElement == null) return;

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

        // Draw 4 corner handles
        double handleSize = DesignConstants.HandleSize;
        var corners = new[]
        {
            new Point(x - handleSize / 2, y - handleSize / 2),
            new Point(x + w - handleSize / 2, y - handleSize / 2),
            new Point(x - handleSize / 2, y + h - handleSize / 2),
            new Point(x + w - handleSize / 2, y + h - handleSize / 2),
        };

        foreach (var corner in corners)
        {
            var handle = new Rectangle
            {
                Width = handleSize,
                Height = handleSize,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(handle, corner.X);
            Canvas.SetTop(handle, corner.Y);
            Panel.SetZIndex(handle, int.MaxValue);
            DesignCanvas.Children.Add(handle);
            _selectionHandles.Add(handle);
        }
    }

    private void ClearSelectionHandles()
    {
        foreach (var handle in _selectionHandles)
            DesignCanvas.Children.Remove(handle);
        _selectionHandles.Clear();
    }
}
