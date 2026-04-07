using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodePrint.Views.Panels;

public partial class ElementPanel : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragStarted;

    public ElementPanel()
    {
        InitializeComponent();
    }

    internal void ElementButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isDragStarted = false;
    }

    internal void ElementButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragStarted) return;

        var pos = e.GetPosition(null);
        var diff = pos - _dragStartPoint;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragStarted = true;

            if (sender is Button btn && btn.CommandParameter is string elementType)
            {
                var data = new DataObject("ElementType", elementType);
                DragDrop.DoDragDrop(btn, data, DragDropEffects.Copy);
            }
        }
    }
}
