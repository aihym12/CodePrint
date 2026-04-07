using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Models;
using CodePrint.Services;

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

    [ObservableProperty]
    private int _sortMode; // 0=按修改时间, 1=按名称, 2=按创建时间

    partial void OnSortModeChanged(int value) => ApplyFilter();

    public string CapacityText => $"模板容量 {TemplateCount}/{TemplateLimit}";

    /// <summary>用户想编辑某个标签时触发。</summary>
    public event Action<LabelDocument>? RequestEditLabel;

    /// <summary>用户想打印某个标签时触发。</summary>
    public event Action<LabelDocument>? RequestPrintLabel;

    /// <summary>用户想返回主页时触发。</summary>
    public event Action? NavigateBack;

    partial void OnTemplateCountChanged(int value) => OnPropertyChanged(nameof(CapacityText));

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>从本地存储加载所有标签。</summary>
    public void LoadLabels()
    {
        Labels.Clear();
        foreach (var doc in LabelStorageService.LoadAll())
            Labels.Add(doc);
        TemplateCount = Labels.Count;
        SelectedFolder = Folders.FirstOrDefault();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredLabels.Clear();
        var source = SelectedFolder?.Name == "全部" || SelectedFolder == null
            ? Labels
            : new ObservableCollection<LabelDocument>(Labels.Where(l => l.FolderId == SelectedFolder.Id));

        var filtered = source.Where(l =>
            string.IsNullOrEmpty(SearchText) ||
            l.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // 排序
        var sorted = SortMode switch
        {
            1 => filtered.OrderBy(l => l.Name),
            2 => filtered.OrderBy(l => l.CreatedAt),
            _ => filtered.OrderByDescending(l => l.ModifiedAt)
        };

        foreach (var label in sorted)
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
            FolderId = SelectedFolder?.Name == "全部" ? null : SelectedFolder?.Id
        };
        LabelStorageService.Save(label);
        Labels.Insert(0, label);
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void EditLabel(LabelDocument? label)
    {
        if (label != null)
            RequestEditLabel?.Invoke(label);
    }

    [RelayCommand]
    private void PrintLabel(LabelDocument? label)
    {
        if (label != null)
            RequestPrintLabel?.Invoke(label);
    }

    [RelayCommand]
    private void DeleteLabel(LabelDocument? label)
    {
        if (label == null) return;
        LabelStorageService.Delete(label.Id);
        Labels.Remove(label);
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void DuplicateLabel(LabelDocument? label)
    {
        if (label == null) return;
        var copy = new LabelDocument
        {
            Name = label.Name + "_副本",
            WidthMm = label.WidthMm,
            HeightMm = label.HeightMm,
            Orientation = label.Orientation,
            FolderId = label.FolderId,
            BackgroundColor = label.BackgroundColor
        };
        // 复制元素
        foreach (var el in label.Elements)
        {
            var clone = ElementCloneService.Clone(el);
            copy.Elements.Add(clone);
        }
        LabelStorageService.Save(copy);
        Labels.Insert(0, copy);
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
        {
            LabelStorageService.Delete(label.Id);
            Labels.Remove(label);
        }
        SelectedLabels.Clear();
        TemplateCount = Labels.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void BatchPrint()
    {
        // 批量打印：依次打印选中的标签
        foreach (var label in SelectedLabels.ToList())
        {
            RequestPrintLabel?.Invoke(label);
        }
    }

    [RelayCommand]
    private void SelectFolder(LabelFolder folder)
    {
        SelectedFolder = folder;
        ApplyFilter();
    }

    [RelayCommand]
    private void SetSortMode(string? mode)
    {
        SortMode = int.TryParse(mode, out var m) ? m : 0;
        ApplyFilter();
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBack?.Invoke();
    }
}
