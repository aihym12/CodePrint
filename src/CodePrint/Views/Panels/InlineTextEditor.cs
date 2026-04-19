using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;
using Block = System.Windows.Documents.Block;

namespace CodePrint.Views.Panels;

/// <summary>
/// Encapsulates the inline WYSIWYG text-editing experience for a <see cref="TextElement"/>
/// on the canvas.
///
/// The editor runs an explicit state machine:
///   <c>Idle → Editing → (Committing | Cancelling) → Idle</c>
/// All external entry points (<see cref="Begin"/>, <see cref="Commit"/>, <see cref="Cancel"/>)
/// are re-entrancy safe; the state machine guarantees nested / concurrent
/// invocations (e.g. LostFocus firing while an outer Commit is already running)
/// cannot double-apply changes.
/// </summary>
internal sealed class InlineTextEditor
{
    private enum EditorState { Idle, Editing, Finalizing }

    private static readonly double MmToPx = DesignConstants.MmToPixel;

    private EditorState _state = EditorState.Idle;

    private TextBox? _editor;
    private Border? _chrome;            // visual-only hint border; never captures hit tests
    private TextElement? _element;
    private FrameworkElement? _hiddenVisual;
    private Canvas? _host;
    private UndoRedoService? _undoRedo;

    private string _initialContent = string.Empty;
    private double _initialHeightMm;

    /// <summary>True while an editor is attached and awaiting user input.</summary>
    public bool IsEditing => _state == EditorState.Editing;

    /// <summary>The element currently being edited, or null.</summary>
    public TextElement? EditingElement => _element;

    /// <summary>Raised after a successful commit (content may or may not have changed).</summary>
    public event EventHandler? Committed;

    /// <summary>Raised after a cancellation (changes discarded).</summary>
    public event EventHandler? Cancelled;

    /// <summary>
    /// Enters inline edit mode for <paramref name="element"/>.
    /// If another edit is already active, it is committed first.
    /// </summary>
    /// <param name="element">The text element to edit.</param>
    /// <param name="host">The canvas on which the element is rendered.</param>
    /// <param name="elementVisual">
    /// The existing rendered visual for the element, which will be temporarily hidden.
    /// May be null if the visual cannot be located (editor still functions).
    /// </param>
    /// <param name="undoRedo">Undo/redo service; if supplied, commits are recorded.</param>
    /// <param name="caretIndex">
    /// Optional caret position within the text. If null or out-of-range, the entire
    /// content is selected (standard "replace-all" behaviour).
    /// </param>
    public void Begin(TextElement element,
                      Canvas host,
                      FrameworkElement? elementVisual,
                      UndoRedoService? undoRedo,
                      int? caretIndex)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (host == null) throw new ArgumentNullException(nameof(host));

        // Flush any prior session before re-using fields.
        if (_state != EditorState.Idle) Commit();

        _element = element;
        _host = host;
        _hiddenVisual = elementVisual;
        _undoRedo = undoRedo;
        _initialContent = element.Content ?? string.Empty;
        _initialHeightMm = element.Height;

        // Hide the rendered visual while editing so it doesn't double-draw under the TextBox.
        if (_hiddenVisual != null)
            _hiddenVisual.Visibility = Visibility.Collapsed;

        BuildEditor(element);

        // Listen for property changes so style edits made elsewhere (toolbar / properties
        // panel) are reflected live inside the editor without rebuilding it.
        element.PropertyChanged += OnElementPropertyChanged;

        _state = EditorState.Editing;

        // Focus and caret. Must happen after Canvas.Children.Add so the control is in the tree.
        _editor!.Focus();
        Keyboard.Focus(_editor);

        if (caretIndex is int idx && idx >= 0 && idx <= _editor.Text.Length)
        {
            _editor.CaretIndex = idx;
            _editor.SelectionLength = 0;
        }
        else
        {
            _editor.SelectAll();
        }
    }

    /// <summary>
    /// Commits the current edit: writes the new content back to the model, optionally
    /// extends the element height, and records a single (composite) undo entry.
    /// No-op if not currently editing.
    /// </summary>
    public void Commit()
    {
        if (_state != EditorState.Editing) return;

        // Snapshot everything locally so re-entrant events (e.g. LostFocus during teardown)
        // cannot double-process.
        _state = EditorState.Finalizing;

        var editor = _editor;
        var element = _element;
        var host = _host;
        var visual = _hiddenVisual;
        var undoRedo = _undoRedo;
        var oldContent = _initialContent;
        var oldHeight = _initialHeightMm;

        try
        {
            if (editor != null && element != null)
            {
                var newContent = editor.Text ?? string.Empty;

                // Auto-grow height if the editor's actual rendered height exceeds
                // the element's current height. Never shrink (so users who deliberately
                // sized the element down keep their sizing).
                double newHeight = oldHeight;
                try
                {
                    editor.UpdateLayout();
                    var neededMm = editor.ActualHeight / MmToPx;
                    if (neededMm > oldHeight + 0.01)
                    {
                        newHeight = Math.Ceiling(neededMm * 10.0) / 10.0; // round up to 0.1mm
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[InlineTextEditor] height measure failed: {ex.Message}");
                }

                var contentChanged = !string.Equals(oldContent, newContent, StringComparison.Ordinal);
                var heightChanged = Math.Abs(newHeight - oldHeight) > 0.0001;

                if (contentChanged) element.Content = newContent;
                if (heightChanged) element.Height = newHeight;

                RecordUndo(element, oldContent, newContent, oldHeight, newHeight, undoRedo);
            }
        }
        finally
        {
            TeardownEditor(host, element, visual, restoreVisualVisibility: true);
            _state = EditorState.Idle;
        }

        Committed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Cancels the current edit, discarding any changes made in the TextBox.
    /// No-op if not currently editing.
    /// </summary>
    public void Cancel()
    {
        if (_state != EditorState.Editing) return;

        _state = EditorState.Finalizing;

        var host = _host;
        var element = _element;
        var visual = _hiddenVisual;

        try
        {
            // Nothing to save — just clean up.
        }
        finally
        {
            TeardownEditor(host, element, visual, restoreVisualVisibility: true);
            _state = EditorState.Idle;
        }

        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Editor construction
    // ──────────────────────────────────────────────────────────────────────

    private void BuildEditor(TextElement element)
    {
        if (_host == null) return;

        var widthPx = element.Width * MmToPx;
        var heightPx = element.Height * MmToPx;

        // TextBox with zero padding & border → exact pixel alignment with the rendered
        // Border/TextBlock visual underneath.
        var tb = new TextBox
        {
            Text = element.Content ?? string.Empty,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Width = widthPx,
            MinHeight = heightPx,
            AcceptsReturn = element.IsMultiline,
            AcceptsTab = false,
            TextWrapping = element.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };

        ApplyStyle(tb, element);

        // Visual-only chrome — a dashed border sitting OUTSIDE the text box's box model
        // (2px larger per side, zero hit-testing). This gives a clear editing affordance
        // without introducing a 1–3px text jump relative to the rendered visual.
        var chrome = new Border
        {
            Width = widthPx + 4,
            Height = Math.Max(heightPx, tb.MinHeight) + 4,
            BorderThickness = new Thickness(1.5),
            BorderBrush = SafeBrush("#4A90D9", Brushes.DodgerBlue),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };

        Canvas.SetLeft(tb, element.X * MmToPx);
        Canvas.SetTop(tb, element.Y * MmToPx);
        Canvas.SetLeft(chrome, element.X * MmToPx - 2);
        Canvas.SetTop(chrome, element.Y * MmToPx - 2);

        // Editor above chrome; both above any element visual.
        Panel.SetZIndex(chrome, int.MaxValue - 2);
        Panel.SetZIndex(tb, int.MaxValue - 1);

        // Mirror the element's rotation so the editor rotates in sync with the rendered text.
        if (element.Rotation != 0)
        {
            tb.RenderTransformOrigin = new Point(0.5, 0.5);
            tb.RenderTransform = new RotateTransform(element.Rotation);
            chrome.RenderTransformOrigin = new Point(0.5, 0.5);
            chrome.RenderTransform = new RotateTransform(element.Rotation);
        }

        tb.KeyDown += Editor_KeyDown;
        tb.LostFocus += Editor_LostFocus;
        tb.SizeChanged += Editor_SizeChanged;

        _host.Children.Add(chrome);
        _host.Children.Add(tb);

        _editor = tb;
        _chrome = chrome;
    }

    /// <summary>
    /// Pushes all style-related properties from the model onto the TextBox.
    /// Called both on initial build and whenever the underlying element changes
    /// while editing is active.
    /// </summary>
    private static void ApplyStyle(TextBox tb, TextElement el)
    {
        try { tb.FontFamily = new FontFamily(el.FontFamily); }
        catch (Exception ex) { Debug.WriteLine($"[InlineTextEditor] invalid font '{el.FontFamily}': {ex.Message}"); }

        tb.FontSize = el.FontSize > 0 ? el.FontSize : 12;
        tb.FontWeight = el.IsBold ? FontWeights.Bold : FontWeights.Normal;
        tb.FontStyle = el.IsItalic ? FontStyles.Italic : FontStyles.Normal;
        tb.Foreground = SafeBrush(el.ForegroundColor, Brushes.Black);

        tb.Background = string.Equals(el.BackgroundColor, "Transparent", StringComparison.OrdinalIgnoreCase)
            ? Brushes.Transparent
            : SafeBrush(el.BackgroundColor, Brushes.Transparent);

        tb.TextAlignment = el.TextAlignment switch
        {
            Models.TextAlignment.Center => System.Windows.TextAlignment.Center,
            Models.TextAlignment.Right => System.Windows.TextAlignment.Right,
            Models.TextAlignment.Justify => System.Windows.TextAlignment.Justify,
            _ => System.Windows.TextAlignment.Left,
        };

        // LineHeight via block line stacking — matches CanvasRendererHelper.
        if (el.LineSpacing > 0)
        {
            tb.SetValue(Block.LineHeightProperty, el.FontSize * el.LineSpacing);
            tb.SetValue(Block.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight);
        }
        else
        {
            tb.ClearValue(Block.LineHeightProperty);
            tb.ClearValue(Block.LineStackingStrategyProperty);
        }

        // NOTE: letter-spacing and underline/strikethrough decorations cannot be applied
        // to a plain WPF TextBox the same way they're applied to the rendered TextBlock.
        // The chrome border signals "editing" visually; the rendered visual re-appears
        // on commit/cancel, so the user always sees the final styled result.
        // Sync the multiline wrapping flag so toggling it mid-edit takes effect.
        tb.AcceptsReturn = el.IsMultiline;
        tb.TextWrapping = el.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Event handlers
    // ──────────────────────────────────────────────────────────────────────

    private void Editor_KeyDown(object sender, KeyEventArgs e)
    {
        if (_state != EditorState.Editing || _element == null) return;

        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (_element.IsMultiline)
            {
                // Multiline: Ctrl+Enter commits, plain Enter inserts a newline.
                if (ctrl)
                {
                    Commit();
                    e.Handled = true;
                }
                // Else: let the TextBox insert a newline natively.
            }
            else
            {
                // Single-line: any Enter (without Shift) commits.
                if (!shift)
                {
                    Commit();
                    e.Handled = true;
                }
            }
        }
    }

    private void Editor_LostFocus(object sender, RoutedEventArgs e)
    {
        // Commit is a no-op when not in Editing state, so this is safe even during teardown.
        Commit();
    }

    private void Editor_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Keep the chrome border sized to the editor as the user types.
        if (_chrome != null && _editor != null)
        {
            _chrome.Height = Math.Max(_editor.ActualHeight, _editor.MinHeight) + 4;
        }
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_state != EditorState.Editing || _editor == null || _element == null) return;

        // Position / size changes → re-position the editor and chrome, don't rebuild.
        switch (e.PropertyName)
        {
            case nameof(LabelElement.X):
            case nameof(LabelElement.Y):
                Canvas.SetLeft(_editor, _element.X * MmToPx);
                Canvas.SetTop(_editor, _element.Y * MmToPx);
                if (_chrome != null)
                {
                    Canvas.SetLeft(_chrome, _element.X * MmToPx - 2);
                    Canvas.SetTop(_chrome, _element.Y * MmToPx - 2);
                }
                return;

            case nameof(LabelElement.Width):
                _editor.Width = _element.Width * MmToPx;
                if (_chrome != null) _chrome.Width = _element.Width * MmToPx + 4;
                return;

            case nameof(LabelElement.Height):
                _editor.MinHeight = _element.Height * MmToPx;
                return;

            case nameof(LabelElement.Rotation):
                if (_element.Rotation != 0)
                {
                    _editor.RenderTransformOrigin = new Point(0.5, 0.5);
                    _editor.RenderTransform = new RotateTransform(_element.Rotation);
                    if (_chrome != null)
                    {
                        _chrome.RenderTransformOrigin = new Point(0.5, 0.5);
                        _chrome.RenderTransform = new RotateTransform(_element.Rotation);
                    }
                }
                else
                {
                    _editor.RenderTransform = null;
                    if (_chrome != null) _chrome.RenderTransform = null;
                }
                return;

            case nameof(LabelElement.Opacity):
            case nameof(LabelElement.ZIndex):
            case nameof(LabelElement.IsVisible):
                return; // ignored while editing
        }

        // Style / text / font changes → re-apply all style props (cheap on a single TextBox).
        ApplyStyle(_editor, _element);

        // If the model's Content changed out from under us (e.g. via script), adopt it.
        if (e.PropertyName == nameof(TextElement.Content) &&
            !string.Equals(_editor.Text, _element.Content, StringComparison.Ordinal))
        {
            var caret = _editor.CaretIndex;
            _editor.Text = _element.Content ?? string.Empty;
            _editor.CaretIndex = Math.Min(caret, _editor.Text.Length);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Teardown / helpers
    // ──────────────────────────────────────────────────────────────────────

    private void TeardownEditor(Canvas? host,
                                TextElement? element,
                                FrameworkElement? visual,
                                bool restoreVisualVisibility)
    {
        // Unsubscribe first so nothing fires mid-teardown.
        if (_editor != null)
        {
            _editor.KeyDown -= Editor_KeyDown;
            _editor.LostFocus -= Editor_LostFocus;
            _editor.SizeChanged -= Editor_SizeChanged;
        }

        if (element != null)
            element.PropertyChanged -= OnElementPropertyChanged;

        if (host != null)
        {
            try
            {
                if (_editor != null && host.Children.Contains(_editor))
                    host.Children.Remove(_editor);
                if (_chrome != null && host.Children.Contains(_chrome))
                    host.Children.Remove(_chrome);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InlineTextEditor] teardown remove failed: {ex.Message}");
            }
        }

        if (restoreVisualVisibility && visual != null)
            visual.Visibility = Visibility.Visible;

        _editor = null;
        _chrome = null;
        _element = null;
        _host = null;
        _hiddenVisual = null;
        _undoRedo = null;
        _initialContent = string.Empty;
        _initialHeightMm = 0;
    }

    private static void RecordUndo(TextElement element,
                                   string oldContent, string newContent,
                                   double oldHeight, double newHeight,
                                   UndoRedoService? undoRedo)
    {
        if (undoRedo == null) return;

        var contentChanged = !string.Equals(oldContent, newContent, StringComparison.Ordinal);
        var heightChanged = Math.Abs(newHeight - oldHeight) > 0.0001;

        if (!contentChanged && !heightChanged) return;

        var contentAction = contentChanged
            ? new PropertyChangeAction<string>(element, $"编辑 {element.Name}",
                (el, v) => ((TextElement)el).Content = v, oldContent, newContent)
            : null;

        var heightAction = heightChanged
            ? new PropertyChangeAction<double>(element, $"调整 {element.Name} 高度",
                (el, v) => el.Height = v, oldHeight, newHeight)
            : null;

        if (contentAction != null && heightAction != null)
        {
            undoRedo.Record(new CompositeAction($"编辑 {element.Name}",
                new IUndoableAction[] { contentAction, heightAction }));
        }
        else if (contentAction != null)
        {
            undoRedo.Record(contentAction);
        }
        else if (heightAction != null)
        {
            undoRedo.Record(heightAction);
        }
    }

    private static SolidColorBrush SafeBrush(string? hex, SolidColorBrush fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[InlineTextEditor] invalid color '{hex}': {ex.Message}");
            return fallback;
        }
    }
}
