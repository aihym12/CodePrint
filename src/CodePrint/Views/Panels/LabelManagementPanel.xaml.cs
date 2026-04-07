using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodePrint.Models;
using CodePrint.ViewModels;

namespace CodePrint.Views.Panels;

public partial class LabelManagementPanel : UserControl
{
    public LabelManagementPanel()
    {
        InitializeComponent();
    }

    public LabelManagementViewModel ViewModel => (LabelManagementViewModel)DataContext;

    /// <summary>双击标签卡片缩略图区域，打开编辑。</summary>
    private void LabelCard_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement fe)
        {
            if (fe.DataContext is LabelDocument doc)
            {
                ViewModel.EditLabelCommand.Execute(doc);
            }
            e.Handled = true;
        }
    }

    /// <summary>"···"更多按钮点击，弹出上下文菜单。</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not LabelDocument doc) return;

        var menu = new ContextMenu();

        var duplicateItem = new MenuItem { Header = "复制" };
        duplicateItem.Click += (_, _) => ViewModel.DuplicateLabelCommand.Execute(doc);
        menu.Items.Add(duplicateItem);

        var editItem = new MenuItem { Header = "编辑" };
        editItem.Click += (_, _) => ViewModel.EditLabelCommand.Execute(doc);
        menu.Items.Add(editItem);

        var printItem = new MenuItem { Header = "打印" };
        printItem.Click += (_, _) => ViewModel.PrintLabelCommand.Execute(doc);
        menu.Items.Add(printItem);

        menu.Items.Add(new Separator());

        var deleteItem = new MenuItem { Header = "删除", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35)) };
        deleteItem.Click += (_, _) => ViewModel.DeleteLabelCommand.Execute(doc);
        menu.Items.Add(deleteItem);

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
    }
}
