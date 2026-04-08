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

    /// <summary>双击标签名称，进入内联编辑模式。</summary>
    private void LabelName_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is TextBlock textBlock)
        {
            // 找到同一 Grid 内的 TextBox
            if (textBlock.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is TextBox editBox)
                    {
                        editBox.Text = textBlock.Text;
                        textBlock.Visibility = Visibility.Collapsed;
                        editBox.Visibility = Visibility.Visible;
                        editBox.Focus();
                        editBox.SelectAll();
                        break;
                    }
                }
            }
            e.Handled = true;
        }
    }

    /// <summary>编辑框失焦时确认重命名。</summary>
    private void LabelNameEdit_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitLabelRename(sender as TextBox);
    }

    /// <summary>编辑框按键：Enter 确认，Escape 取消。</summary>
    private void LabelNameEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox editBox) return;

        if (e.Key == Key.Enter)
        {
            CommitLabelRename(editBox);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelLabelRename(editBox);
            e.Handled = true;
        }
    }

    private void CommitLabelRename(TextBox? editBox)
    {
        if (editBox == null || editBox.Visibility != Visibility.Visible) return;

        if (editBox.Parent is Grid grid && editBox.DataContext is LabelDocument doc)
        {
            var newName = editBox.Text?.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                ViewModel.RenameLabelCommand.Execute((doc, newName));
            }

            // 切换回显示模式
            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb && tb.FontWeight == FontWeights.SemiBold)
                {
                    tb.Visibility = Visibility.Visible;
                    break;
                }
            }
            editBox.Visibility = Visibility.Collapsed;
        }
    }

    private void CancelLabelRename(TextBox editBox)
    {
        if (editBox.Parent is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb && tb.FontWeight == FontWeights.SemiBold)
                {
                    tb.Visibility = Visibility.Visible;
                    break;
                }
            }
            editBox.Visibility = Visibility.Collapsed;
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
