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

            if (sender is Button btn && btn.Tag is string elementType)
            {
                var data = new DataObject("ElementType", elementType);
                DragDrop.DoDragDrop(btn, data, DragDropEffects.Copy);
            }
        }
    }

    private void ElementButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string elementType)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.AddElementCommand.Execute(elementType);
            }
        }
    }

    private void ElementButton_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string elementType)
        {
            var menu = new ContextMenu();

            var addItem = new MenuItem { Header = "添加到画布" };
            addItem.Click += (_, _) =>
            {
                var window = Window.GetWindow(this);
                if (window?.DataContext is ViewModels.MainViewModel viewModel)
                {
                    viewModel.AddElementCommand.Execute(elementType);
                }
            };
            menu.Items.Add(addItem);

            menu.PlacementTarget = btn;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }

    private void ImportImageAsText_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window?.DataContext is ViewModels.MainViewModel viewModel)
        {
            viewModel.ImportImageAsTextCommand.Execute(null);
        }
    }
}
