using System.Windows;
using CodePrint.ViewModels;

namespace CodePrint.Views.Dialogs;

public partial class LabelSettingsDialog : Window
{
    public LabelSettingsDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads settings from the specified document into the dialog.
    /// </summary>
    public void LoadFromMainViewModel(MainViewModel mainVm)
    {
        if (DataContext is LabelSettingsViewModel vm)
        {
            vm.LoadFromDocument(mainVm.CurrentDocument);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Gets the ViewModel for applying settings back to the document.
    /// </summary>
    public LabelSettingsViewModel SettingsViewModel => (LabelSettingsViewModel)DataContext;
}
