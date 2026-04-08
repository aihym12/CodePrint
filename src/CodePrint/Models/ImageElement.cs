namespace CodePrint.Models;

public class ImageElement : LabelElement
{
    private string _imagePath = string.Empty;
    private bool _maintainAspectRatio = true;
    private double _cornerRadius;
    private double _brightness = 1.0;
    private double _contrast = 1.0;

    public ImageElement() { Type = ElementType.Image; Name = "图片"; Width = 20; Height = 20; }

    public string ImagePath { get => _imagePath; set => SetField(ref _imagePath, value); }
    public bool MaintainAspectRatio { get => _maintainAspectRatio; set => SetField(ref _maintainAspectRatio, value); }
    public double CornerRadius { get => _cornerRadius; set => SetField(ref _cornerRadius, value); }
    public double Brightness { get => _brightness; set => SetField(ref _brightness, value); }
    public double Contrast { get => _contrast; set => SetField(ref _contrast, value); }
}
