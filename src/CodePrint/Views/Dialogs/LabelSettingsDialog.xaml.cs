using System.Windows;

namespace CodePrint.Views.Dialogs;

public partial class LabelSettingsDialog : Window
{
    public LabelSettingsDialog()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
