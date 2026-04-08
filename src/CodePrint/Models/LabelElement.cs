using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CodePrint.Models;

public enum ElementType
{
    Text, Barcode, QrCode, LinkedQrCode, Image, Icon,
    Line, Rectangle, Date, Table, Pdf, Warning, Watermark
}

public class LabelElement : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private ElementType _type;
    private double _x;
    private double _y;
    private double _width;
    private double _height;
    private double _rotation;
    private double _opacity = 1.0;
    private int _zIndex;
    private bool _isLocked;
    private bool _isVisible = true;
    private string _name = string.Empty;

    public string Id { get => _id; set => SetField(ref _id, value); }
    public ElementType Type { get => _type; set => SetField(ref _type, value); }
    public double X { get => _x; set => SetField(ref _x, value); }
    public double Y { get => _y; set => SetField(ref _y, value); }
    public double Width { get => _width; set => SetField(ref _width, value); }
    public double Height { get => _height; set => SetField(ref _height, value); }
    public double Rotation { get => _rotation; set => SetField(ref _rotation, value); }
    public double Opacity { get => _opacity; set => SetField(ref _opacity, value); }
    public int ZIndex { get => _zIndex; set => SetField(ref _zIndex, value); }
    public bool IsLocked { get => _isLocked; set => SetField(ref _isLocked, value); }
    public bool IsVisible { get => _isVisible; set => SetField(ref _isVisible, value); }
    public string Name { get => _name; set => SetField(ref _name, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Notify that a derived-class property changed (forces visual refresh).</summary>
    public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
}
