using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodePrint.ViewModels;
using CodePrint.Views.Dialogs;

namespace CodePrint.Views.Panels;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
    }

    private HomeViewModel ViewModel => (HomeViewModel)DataContext;

    private void NewBlankLabel_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new NewLabelDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ConfirmNewLabel(dialog.CreatedDocument);
        }
    }

    private void PdfCropPrint_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenPdfCropCommand.Execute(null);
    }

    private void PhotoPrint_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenPhotoPrintCommand.Execute(null);
    }
}
