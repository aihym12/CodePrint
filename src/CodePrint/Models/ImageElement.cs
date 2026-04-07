namespace CodePrint.Models;

public class ImageElement : LabelElement
{
    public ImageElement() { Type = ElementType.Image; Name = "图片"; Width = 20; Height = 20; }
    public string ImagePath { get; set; } = string.Empty;
    public bool MaintainAspectRatio { get; set; } = true;
    public double CornerRadius { get; set; }
    public double Brightness { get; set; } = 1.0;
    public double Contrast { get; set; } = 1.0;
}
