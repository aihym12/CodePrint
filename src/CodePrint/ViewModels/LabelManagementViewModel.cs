using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class LabelManagementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LabelFolder> _folders = new()
    {
        new LabelFolder { Name = "全部" },
        new LabelFolder { Name = "默认" }
    };

    [ObservableProperty]
    private LabelFolder? _selectedFolder;

    [ObservableProperty]
    private ObservableCollection<LabelDocument> _labels = new();

    [ObservableProperty]
    private ObservableCollection<LabelDocument> _filteredLabels = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isMultiSelectMode;

    [ObservableProperty]
    private ObservableCollection<LabelDocument> _selectedLabels = new();

    [ObservableProperty]
    private int _templateCount;

    [ObservableProperty]
    private int _templateLimit = 50;

    public string CapacityText => $"模板容量 {TemplateCount}/{TemplateLimit}";

    partial void OnTemplateCountChanged(int value) => OnPropertyChanged(nameof(CapacityText));

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredLabels.Clear();
        var source = SelectedFolder?.Name == "全部" || SelectedFolder == null
            ? Labels
            : new ObservableCollection<LabelDocument>(Labels.Where(l => l.FolderId == SelectedFolder.Id));

        foreach (var label in source.Where(l =>
            string.IsNullOrEmpty(SearchText) ||
            l.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
        {
            FilteredLabels.Add(label);
        }
    }

    [RelayCommand]
    private void CreateFolder()
    {
        Folders.Add(new LabelFolder { Name = $"新建文件夹{Folders.Count}" });
    }

    [RelayCommand]
    private void DeleteFolder(LabelFolder folder)
    {
        if (folder.Name is "全部" or "默认") return;
        Folders.Remove(folder);
    }

    [RelayCommand]
    private void CreateLabel()
    {
        var label = new LabelDocument
        {
            Name = $"新建标签_{Labels.Count + 1}",
            FolderId = SelectedFolder?.Id
        };
        Labels.Add(label);
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void DeleteLabel(LabelDocument label)
    {
        Labels.Remove(label);
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void DuplicateLabel(LabelDocument label)
    {
        var copy = new LabelDocument
        {
            Name = label.Name + "_副本",
            WidthMm = label.WidthMm,
            HeightMm = label.HeightMm,
            FolderId = label.FolderId
        };
        Labels.Add(copy);
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        IsMultiSelectMode = !IsMultiSelectMode;
        if (!IsMultiSelectMode) SelectedLabels.Clear();
    }

    [RelayCommand]
    private void BatchDelete()
    {
        foreach (var label in SelectedLabels.ToList())
            Labels.Remove(label);
        SelectedLabels.Clear();
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void SelectFolder(LabelFolder folder)
    {
        SelectedFolder = folder;
        ApplyFilter();
    }
}
