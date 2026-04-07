using System.Windows.Input;

namespace CodePrint.Helpers;

/// <summary>
/// Keyboard shortcut definitions for the application.
/// </summary>
public static class ShortcutKeys
{
    public static readonly KeyGesture Save = new(Key.S, ModifierKeys.Control);
    public static readonly KeyGesture Undo = new(Key.Z, ModifierKeys.Control);
    public static readonly KeyGesture Redo = new(Key.Y, ModifierKeys.Control);
    public static readonly KeyGesture SelectAll = new(Key.A, ModifierKeys.Control);
    public static readonly KeyGesture Copy = new(Key.C, ModifierKeys.Control);
    public static readonly KeyGesture Paste = new(Key.V, ModifierKeys.Control);
    public static readonly KeyGesture Cut = new(Key.X, ModifierKeys.Control);
    public static readonly KeyGesture Delete = new(Key.Delete);
    public static readonly KeyGesture Duplicate = new(Key.D, ModifierKeys.Control);
    public static readonly KeyGesture ZoomIn = new(Key.OemPlus, ModifierKeys.Control);
    public static readonly KeyGesture ZoomOut = new(Key.OemMinus, ModifierKeys.Control);
    public static readonly KeyGesture FitToWindow = new(Key.D0, ModifierKeys.Control);
    public static readonly KeyGesture Print = new(Key.P, ModifierKeys.Control);
    public static readonly KeyGesture BringToFront = new(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift);
    public static readonly KeyGesture SendToBack = new(Key.OemOpenBrackets, ModifierKeys.Control | ModifierKeys.Shift);
    public static readonly KeyGesture MoveLayerUp = new(Key.OemCloseBrackets, ModifierKeys.Control);
    public static readonly KeyGesture MoveLayerDown = new(Key.OemOpenBrackets, ModifierKeys.Control);
    public static readonly KeyGesture Group = new(Key.G, ModifierKeys.Control);
    public static readonly KeyGesture Ungroup = new(Key.G, ModifierKeys.Control | ModifierKeys.Shift);

    // Shift+Arrow for large nudge
    public static readonly KeyGesture NudgeLeft = new(Key.Left, ModifierKeys.Shift);
    public static readonly KeyGesture NudgeRight = new(Key.Right, ModifierKeys.Shift);
    public static readonly KeyGesture NudgeUp = new(Key.Up, ModifierKeys.Shift);
    public static readonly KeyGesture NudgeDown = new(Key.Down, ModifierKeys.Shift);

    // LayerUp/Down aliases for MoveLayerUp/Down
    public static readonly KeyGesture LayerUp = MoveLayerUp;
    public static readonly KeyGesture LayerDown = MoveLayerDown;
}
