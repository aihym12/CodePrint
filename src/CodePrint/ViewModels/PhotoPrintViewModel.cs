using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodePrint.ViewModels;

/// <summary>Scaling mode for photo printing.</summary>
public enum PhotoScaleMode
{
    /// <summary>Fill the label area (may crop edges).</summary>
    Fill,

    /// <summary>Fit within label maintaining aspect ratio (may have borders).</summary>
    Fit,

    /// <summary>Stretch to exactly fill the label (may distort).</summary>
    Stretch
}

/// <summary>Represents a single photo queued for printing.</summary>
public partial class PhotoItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private double _brightness = 1.0;

    [ObservableProperty]
    private double _contrast = 1.0;

    public string FileName => System.IO.Path.GetFileName(FilePath);
}

/// <summary>ViewModel for the Photo Print module (PRD Section 11).</summary>
public partial class PhotoPrintViewModel : ObservableObject
{
    /// <summary>Raised when user clicks Back to return to the home page.</summary>
    public event Action? NavigateBack;

    [ObservableProperty]
    private ObservableCollection<PhotoItem> _photos = new();

    [ObservableProperty]
    private PhotoItem? _selectedPhoto;

    [ObservableProperty]
    private PhotoScaleMode _scaleMode = PhotoScaleMode.Fit;

    [ObservableProperty]
    private string _statusText = "请选择或拖入图片文件";

    [RelayCommand]
    private void RemovePhoto()
    {
        if (SelectedPhoto != null)
        {
            Photos.Remove(SelectedPhoto);
            SelectedPhoto = Photos.Count > 0 ? Photos[0] : null;
        }
    }

    [RelayCommand]
    private void ClearPhotos()
    {
        Photos.Clear();
        SelectedPhoto = null;
        StatusText = "请选择或拖入图片文件";
    }

    [RelayCommand]
    private void RotateClockwise()
    {
        if (SelectedPhoto != null)
            SelectedPhoto.Rotation = (SelectedPhoto.Rotation + 90) % 360;
    }

    [RelayCommand]
    private void RotateCounterClockwise()
    {
        if (SelectedPhoto != null)
            SelectedPhoto.Rotation = (SelectedPhoto.Rotation + 270) % 360;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBack?.Invoke();
    }

    [RelayCommand]
    private void Print()
    {
        if (Photos.Count == 0)
        {
            StatusText = "请先添加图片";
            return;
        }
        StatusText = "正在发送到打印机…";
    }

    /// <summary>Adds images from file paths.</summary>
    public void AddFiles(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            Photos.Add(new PhotoItem { FilePath = path });
        }
        if (SelectedPhoto == null && Photos.Count > 0)
            SelectedPhoto = Photos[0];
        StatusText = $"已添加 {Photos.Count} 张图片";
    }
}
