using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

/// <summary>ViewModel for the home/landing page with three main entry points.</summary>
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private UserProfile _currentUser = new() { Nickname = "用户", Phone = "13400007122" };

    [ObservableProperty]
    private string _activeNavItem = "新建标签";

    /// <summary>Raised when the user has configured a new blank label and wants to enter the designer.</summary>
    public event Action<LabelDocument>? NavigateToDesigner;

    /// <summary>Raised when the user imports a photo and wants to enter the designer with OCR processing.</summary>
    public event Action<LabelDocument, string>? NavigateToDesignerWithOcr;

    /// <summary>Raised when the user selects PDF Crop &amp; Print.</summary>
    public event Action? NavigateToPdfCrop;

    /// <summary>Raised when the user selects Photo Print.</summary>
    public event Action? NavigateToPhotoPrint;

    /// <summary>Raised when the user selects Template Library.</summary>
    public event Action? NavigateToTemplateLibrary;

    public string TemplateCapacityText => $"模板容量 {CurrentUser.TemplateCount}/{CurrentUser.TemplateLimit}";

    [RelayCommand]
    private void CreateBlankLabel()
    {
        // This will be handled by the view to open the NewLabelDialog,
        // then call ConfirmNewLabel with the resulting document.
    }

    /// <summary>Called by the view after the new label dialog is confirmed.</summary>
    public void ConfirmNewLabel(LabelDocument document)
    {
        NavigateToDesigner?.Invoke(document);
    }

    /// <summary>Called by the view after importing a photo for OCR-based template creation.</summary>
    public void ConfirmNewLabelWithOcr(LabelDocument document, string imagePath)
    {
        NavigateToDesignerWithOcr?.Invoke(document, imagePath);
    }

    [RelayCommand]
    private void OpenPdfCrop()
    {
        NavigateToPdfCrop?.Invoke();
    }

    [RelayCommand]
    private void OpenPhotoPrint()
    {
        NavigateToPhotoPrint?.Invoke();
    }

    [RelayCommand]
    private void OpenTemplateLibrary()
    {
        ActiveNavItem = "模板选择";
        NavigateToTemplateLibrary?.Invoke();
    }

    [RelayCommand]
    private void SelectNavItem(string? item)
    {
        if (item != null)
            ActiveNavItem = item;
    }
}
