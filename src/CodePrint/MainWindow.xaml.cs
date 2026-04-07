using System.Windows;
using System.Windows.Input;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;
using CodePrint.ViewModels;
using CodePrint.Views.Dialogs;
using Microsoft.Win32;

namespace CodePrint;

public partial class MainWindow : Window
{
    private readonly MainViewModel _designerViewModel = new();
    private HomeViewModel? _homeViewModel;
    private PdfCropViewModel? _pdfCropViewModel;
    private PhotoPrintViewModel? _photoPrintViewModel;

    public MainWindow()
    {
        InitializeComponent();
        SetupHomePage();
    }

    private MainViewModel ViewModel => _designerViewModel;

    // ── Navigation ──

    private void SetupHomePage()
    {
        _homeViewModel = (HomeViewModel)HomePageView.DataContext;
        _homeViewModel.NavigateToDesigner += NavigateToDesigner;
        _homeViewModel.NavigateToPdfCrop += NavigateToPdfCrop;
        _homeViewModel.NavigateToPhotoPrint += NavigateToPhotoPrint;
    }

    private void NavigateToDesigner(LabelDocument document)
    {
        _designerViewModel.CurrentDocument = document;
        _designerViewModel.RefreshDocumentProperties();
        DesignerView.DataContext = _designerViewModel;
        ShowView("Designer");
    }

    private void NavigateToPdfCrop()
    {
        _pdfCropViewModel = (PdfCropViewModel)PdfCropViewPanel.DataContext;
        _pdfCropViewModel.NavigateBack += () => ShowView("Home");
        ShowView("PdfCrop");
    }

    private void NavigateToPhotoPrint()
    {
        _photoPrintViewModel = (PhotoPrintViewModel)PhotoPrintViewPanel.DataContext;
        _photoPrintViewModel.NavigateBack += () => ShowView("Home");
        ShowView("PhotoPrint");
    }

    private void ShowView(string view)
    {
        HomePageView.Visibility = view == "Home" ? Visibility.Visible : Visibility.Collapsed;
        DesignerView.Visibility = view == "Designer" ? Visibility.Visible : Visibility.Collapsed;
        PdfCropViewPanel.Visibility = view == "PdfCrop" ? Visibility.Visible : Visibility.Collapsed;
        PhotoPrintViewPanel.Visibility = view == "PhotoPrint" ? Visibility.Visible : Visibility.Collapsed;

        // Only apply designer keyboard shortcuts when the designer is visible
        if (view == "Designer")
        {
            SetupDesignerInputBindings();
        }
        else
        {
            InputBindings.Clear();
        }
    }

    private void SetupDesignerInputBindings()
    {
        InputBindings.Clear();
        InputBindings.Add(new KeyBinding(ViewModel.SaveCommand, ShortcutKeys.Save));
        InputBindings.Add(new KeyBinding(ViewModel.UndoCommand, ShortcutKeys.Undo));
        InputBindings.Add(new KeyBinding(ViewModel.RedoCommand, ShortcutKeys.Redo));
        InputBindings.Add(new KeyBinding(ViewModel.SelectAllCommand, ShortcutKeys.SelectAll));
        InputBindings.Add(new KeyBinding(ViewModel.DeleteSelectedCommand, new KeyGesture(Key.Delete)));
        InputBindings.Add(new KeyBinding(ViewModel.CopySelectedCommand, ShortcutKeys.Copy));
        InputBindings.Add(new KeyBinding(ViewModel.PasteCommand, ShortcutKeys.Paste));
        InputBindings.Add(new KeyBinding(ViewModel.CutSelectedCommand, ShortcutKeys.Cut));
        InputBindings.Add(new KeyBinding(ViewModel.DuplicateCommand, ShortcutKeys.Duplicate));
        InputBindings.Add(new KeyBinding(ViewModel.ZoomInCommand, ShortcutKeys.ZoomIn));
        InputBindings.Add(new KeyBinding(ViewModel.ZoomOutCommand, ShortcutKeys.ZoomOut));
        InputBindings.Add(new KeyBinding(ViewModel.FitToWindowCommand, ShortcutKeys.FitToWindow));
        InputBindings.Add(new KeyBinding(ViewModel.PrintCommand, ShortcutKeys.Print));
        InputBindings.Add(new KeyBinding(ViewModel.MoveLayerUpCommand, ShortcutKeys.LayerUp));
        InputBindings.Add(new KeyBinding(ViewModel.MoveLayerDownCommand, ShortcutKeys.LayerDown));
        InputBindings.Add(new KeyBinding(ViewModel.BringToFrontCommand, ShortcutKeys.BringToFront));
        InputBindings.Add(new KeyBinding(ViewModel.SendToBackCommand, ShortcutKeys.SendToBack));
        InputBindings.Add(new KeyBinding(ViewModel.NudgeLeftCommand, new KeyGesture(Key.Left)));
        InputBindings.Add(new KeyBinding(ViewModel.NudgeRightCommand, new KeyGesture(Key.Right)));
        InputBindings.Add(new KeyBinding(ViewModel.NudgeUpCommand, new KeyGesture(Key.Up)));
        InputBindings.Add(new KeyBinding(ViewModel.NudgeDownCommand, new KeyGesture(Key.Down)));
        InputBindings.Add(new KeyBinding(ViewModel.LargeNudgeLeftCommand, ShortcutKeys.NudgeLeft));
        InputBindings.Add(new KeyBinding(ViewModel.LargeNudgeRightCommand, ShortcutKeys.NudgeRight));
        InputBindings.Add(new KeyBinding(ViewModel.LargeNudgeUpCommand, ShortcutKeys.NudgeUp));
        InputBindings.Add(new KeyBinding(ViewModel.LargeNudgeDownCommand, ShortcutKeys.NudgeDown));
    }

    private void BackToHome_Click(object sender, RoutedEventArgs e)
    {
        ShowView("Home");
    }

    // ── Designer Dialog Handlers ──

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LabelSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LabelSettingsDialog { Owner = this };
        dialog.LoadFromMainViewModel(ViewModel);
        if (dialog.ShowDialog() == true)
        {
            dialog.SettingsViewModel.ApplyToDocument(ViewModel.CurrentDocument);
            ViewModel.RefreshDocumentProperties();
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintSettingsDialog { Owner = this };
        dialog.ShowDialog();
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileService.FileFilter,
            DefaultExt = FileService.FileExtension,
            FileName = ViewModel.CurrentDocument.Name
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.SaveToFile(dialog.FileName);
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PreviewDialog(ViewModel.CurrentDocument) { Owner = this };
        dialog.ShowDialog();
    }
}