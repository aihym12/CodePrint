using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
    private LabelManagementViewModel? _labelManagementViewModel;

    /// <summary>Suppresses toolbar → model updates while syncing toolbar from model.</summary>
    private bool _isSyncingToolbar;

    /// <summary>Maps internal font family names to display names for the toolbar ComboBox.</summary>
    private static readonly Dictionary<string, string> FontFamilyToDisplay = new()
    {
        { "Microsoft YaHei", "微软雅黑" },
        { "SimSun", "宋体" },
        { "SimHei", "黑体" },
        { "KaiTi", "楷体" },
        { "FangSong", "仿宋" },
    };

    /// <summary>Maps display font names to internal font family names.</summary>
    private static readonly Dictionary<string, string> DisplayToFontFamily = FontFamilyToDisplay
        .ToDictionary(kv => kv.Value, kv => kv.Key);

    public MainWindow()
    {
        InitializeComponent();
        SetupHomePage();
        _designerViewModel.RequestShowPrintDialog += () => Print_Click(this, new RoutedEventArgs());
        _designerViewModel.PropertyChanged += DesignerViewModel_PropertyChanged;
    }

    private MainViewModel ViewModel => _designerViewModel;

    // ── Navigation ──

    private void SetupHomePage()
    {
        _homeViewModel = (HomeViewModel)HomePageView.DataContext;
        _homeViewModel.NavigateToDesigner += NavigateToDesigner;
        _homeViewModel.NavigateToPdfCrop += NavigateToPdfCrop;
        _homeViewModel.NavigateToPhotoPrint += NavigateToPhotoPrint;
        _homeViewModel.NavigateToTemplateLibrary += NavigateToLabelManagement;
    }

    private void NavigateToDesigner(LabelDocument document)
    {
        _designerViewModel.CurrentDocument = document;
        _designerViewModel.RefreshDocumentProperties();

        // 如果文档已存储，设置文件路径以便保存时覆盖
        if (LabelStorageService.Exists(document.Id))
            _designerViewModel.CurrentFilePath = "__storage__:" + document.Id;
        else
            _designerViewModel.CurrentFilePath = null;

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

    private void NavigateToLabelManagement()
    {
        _labelManagementViewModel = (LabelManagementViewModel)LabelManagementViewPanel.DataContext;
        _labelManagementViewModel.NavigateBack -= OnLabelManagementBack;
        _labelManagementViewModel.NavigateBack += OnLabelManagementBack;
        _labelManagementViewModel.RequestEditLabel -= OnEditLabel;
        _labelManagementViewModel.RequestEditLabel += OnEditLabel;
        _labelManagementViewModel.RequestPrintLabel -= OnPrintLabel;
        _labelManagementViewModel.RequestPrintLabel += OnPrintLabel;
        _labelManagementViewModel.LoadLabels();
        ShowView("LabelManagement");
    }

    private void OnLabelManagementBack()
    {
        ShowView("Home");
    }

    private void OnEditLabel(LabelDocument document)
    {
        NavigateToDesigner(document);
    }

    private void OnPrintLabel(LabelDocument document)
    {
        var dialog = new PrintSettingsDialog { Owner = this };
        dialog.SetDocument(document);
        dialog.ShowDialog();
    }

    private void ShowView(string view)
    {
        HomePageView.Visibility = view == "Home" ? Visibility.Visible : Visibility.Collapsed;
        DesignerView.Visibility = view == "Designer" ? Visibility.Visible : Visibility.Collapsed;
        PdfCropViewPanel.Visibility = view == "PdfCrop" ? Visibility.Visible : Visibility.Collapsed;
        PhotoPrintViewPanel.Visibility = view == "PhotoPrint" ? Visibility.Visible : Visibility.Collapsed;
        LabelManagementViewPanel.Visibility = view == "LabelManagement" ? Visibility.Visible : Visibility.Collapsed;

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
        // 返回主页前自动保存当前文档到本地存储
        SaveCurrentDocumentToStorage();
        ShowView("Home");
    }

    /// <summary>将当前设计器文档保存到本地标签存储。</summary>
    private void SaveCurrentDocumentToStorage()
    {
        if (_designerViewModel.CurrentDocument != null)
        {
            LabelStorageService.Save(_designerViewModel.CurrentDocument);
        }
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
        dialog.SetDocument(ViewModel.CurrentDocument);
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

    // ── Font Formatting Toolbar Sync ──

    private LabelElement? _subscribedElement;

    private void DesignerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedElement))
        {
            // Unsubscribe from previously selected element
            if (_subscribedElement != null)
            {
                _subscribedElement.PropertyChanged -= SelectedElement_PropertyChanged;
                _subscribedElement = null;
            }

            // Subscribe to newly selected element
            if (ViewModel.SelectedElement != null)
            {
                _subscribedElement = ViewModel.SelectedElement;
                _subscribedElement.PropertyChanged += SelectedElement_PropertyChanged;
            }

            SyncToolbarFromElement();
        }
    }

    private void SelectedElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When model changes (e.g. from PropertyPanel), keep toolbar in sync
        if (e.PropertyName is nameof(TextElement.FontFamily) or nameof(TextElement.FontSize)
            or nameof(TextElement.LetterSpacing) or nameof(TextElement.LineSpacing)
            or nameof(TextElement.IsBold) or nameof(TextElement.IsItalic)
            or nameof(TextElement.IsUnderline) or nameof(TextElement.IsStrikethrough))
        {
            SyncToolbarFromElement();
        }
    }

    /// <summary>Updates the font formatting toolbar controls to reflect the selected element.</summary>
    private void SyncToolbarFromElement()
    {
        _isSyncingToolbar = true;
        try
        {
            var element = ViewModel.SelectedElement;

            string? fontFamily = null;
            double? fontSize = null;
            double letterSpacing = 0;
            double lineSpacing = 1.2;
            bool isBold = false;
            bool isItalic = false;
            bool isUnderline = false;
            bool isStrikethrough = false;

            if (element is TextElement text)
            {
                fontFamily = text.FontFamily;
                fontSize = text.FontSize;
                letterSpacing = text.LetterSpacing;
                lineSpacing = text.LineSpacing;
                isBold = text.IsBold;
                isItalic = text.IsItalic;
                isUnderline = text.IsUnderline;
                isStrikethrough = text.IsStrikethrough;
            }
            else if (element is DateElement date)
            {
                fontFamily = date.FontFamily;
                fontSize = date.FontSize;
            }
            else if (element is WatermarkElement watermark)
            {
                fontFamily = watermark.FontFamily;
                fontSize = watermark.FontSize;
            }

            // Sync font family ComboBox
            if (fontFamily != null)
            {
                SelectComboItemByContent(FontFamilyCombo, fontFamily);
            }

            // Sync font size ComboBox
            if (fontSize.HasValue)
            {
                FontSizeCombo.Text = fontSize.Value.ToString("G");
            }

            // Sync letter spacing and line spacing
            LetterSpacingBox.Text = letterSpacing.ToString("G");
            LineSpacingBox.Text = lineSpacing.ToString("G");

            // Sync bold/italic/underline/strikethrough button appearance
            UpdateToggleButtonAppearance(BoldButton, isBold);
            UpdateToggleButtonAppearance(ItalicButton, isItalic);
            UpdateToggleButtonAppearance(UnderlineButton, isUnderline);
            UpdateToggleButtonAppearance(StrikethroughButton, isStrikethrough);
        }
        finally
        {
            _isSyncingToolbar = false;
        }
    }

    private static void SelectComboItemByContent(ComboBox combo, string content)
    {
        var displayName = FontFamilyToDisplay.TryGetValue(content, out var mapped) ? mapped : content;

        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Content is string itemText &&
                (string.Equals(itemText, displayName, System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(itemText, content, System.StringComparison.OrdinalIgnoreCase)))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        // If not found in the list, keep current selection
    }

    private static void UpdateToggleButtonAppearance(Button button, bool isActive)
    {
        button.Background = isActive
            ? new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D0D0D0"))
            : System.Windows.Media.Brushes.Transparent;
    }

    // ── Font Formatting Toolbar Event Handlers ──

    /// <summary>Maps display font name to internal font family name.</summary>
    private static string FontDisplayToFamily(string displayName)
    {
        return DisplayToFontFamily.TryGetValue(displayName, out var family) ? family : displayName;
    }

    private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingToolbar) return;

        if (FontFamilyCombo.SelectedItem is ComboBoxItem item && item.Content is string displayName)
        {
            var familyName = FontDisplayToFamily(displayName);
            var element = ViewModel.SelectedElement;

            if (element is TextElement text) text.FontFamily = familyName;
            else if (element is DateElement date) date.FontFamily = familyName;
            else if (element is WatermarkElement watermark) watermark.FontFamily = familyName;
        }
    }

    private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingToolbar) return;
        ApplyFontSizeFromCombo();
    }

    private void FontSizeCombo_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isSyncingToolbar) return;
        ApplyFontSizeFromCombo();
    }

    private void ApplyFontSizeFromCombo()
    {
        string? text = null;
        if (FontSizeCombo.SelectedItem is ComboBoxItem item && item.Content is string content)
            text = content;
        else if (!string.IsNullOrWhiteSpace(FontSizeCombo.Text))
            text = FontSizeCombo.Text;

        if (text != null && double.TryParse(text, out var size) && size >= 1 && size <= 200)
        {
            var element = ViewModel.SelectedElement;
            if (element is TextElement textEl) textEl.FontSize = size;
            else if (element is DateElement dateEl) dateEl.FontSize = size;
            else if (element is WatermarkElement watermarkEl) watermarkEl.FontSize = size;
        }
    }

    private void LetterSpacingBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isSyncingToolbar) return;
        if (ViewModel.SelectedElement is TextElement text && double.TryParse(LetterSpacingBox.Text, out var spacing)
            && spacing >= -10 && spacing <= 100)
            text.LetterSpacing = spacing;
    }

    private void LineSpacingBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isSyncingToolbar) return;
        if (ViewModel.SelectedElement is TextElement text && double.TryParse(LineSpacingBox.Text, out var spacing)
            && spacing >= 0.5 && spacing <= 10)
            text.LineSpacing = spacing;
    }

    private void BoldButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedElement is TextElement text)
        {
            text.IsBold = !text.IsBold;
            UpdateToggleButtonAppearance(BoldButton, text.IsBold);
        }
    }

    private void ItalicButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedElement is TextElement text)
        {
            text.IsItalic = !text.IsItalic;
            UpdateToggleButtonAppearance(ItalicButton, text.IsItalic);
        }
    }

    private void UnderlineButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedElement is TextElement text)
        {
            text.IsUnderline = !text.IsUnderline;
            UpdateToggleButtonAppearance(UnderlineButton, text.IsUnderline);
        }
    }

    private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedElement is TextElement text)
        {
            text.IsStrikethrough = !text.IsStrikethrough;
            UpdateToggleButtonAppearance(StrikethroughButton, text.IsStrikethrough);
        }
    }
}