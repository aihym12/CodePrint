using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class TemplateLibraryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LabelTemplate> _templates = new();

    [ObservableProperty]
    private ObservableCollection<LabelTemplate> _filteredTemplates = new();

    [ObservableProperty]
    private TemplateCategory? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<TemplateCategoryItem> Categories { get; } = new()
    {
        new() { Category = null, DisplayName = "全部" },
        new() { Category = TemplateCategory.ECommerce, DisplayName = "电商物流" },
        new() { Category = TemplateCategory.ProductLabel, DisplayName = "产品标签" },
        new() { Category = TemplateCategory.Food, DisplayName = "食品" },
        new() { Category = TemplateCategory.Retail, DisplayName = "零售" },
        new() { Category = TemplateCategory.Industrial, DisplayName = "工业" },
        new() { Category = TemplateCategory.Office, DisplayName = "办公" },
    };

    partial void OnSelectedCategoryChanged(TemplateCategory? value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredTemplates.Clear();
        foreach (var t in Templates.Where(t =>
            (SelectedCategory == null || t.Category == SelectedCategory) &&
            (string.IsNullOrEmpty(SearchText) || t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))))
        {
            FilteredTemplates.Add(t);
        }
    }

    /// <summary>Raised when a template is selected for use, carrying the template's document.</summary>
    public event Action<LabelDocument>? TemplateSelected;

    [RelayCommand]
    private void UseTemplate(LabelTemplate template)
    {
        if (template?.Document != null)
        {
            TemplateSelected?.Invoke(template.Document);
        }
    }

    [RelayCommand]
    private void ToggleFavorite(LabelTemplate template)
    {
        template.IsFavorite = !template.IsFavorite;
    }

    [RelayCommand]
    private void FilterByCategory(TemplateCategoryItem item)
    {
        SelectedCategory = item.Category;
    }
}

public class TemplateCategoryItem
{
    public TemplateCategory? Category { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
