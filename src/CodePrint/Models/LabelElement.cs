namespace CodePrint.Models;

public enum ElementType
{
    Text, Barcode, QrCode, LinkedQrCode, Image, Icon,
    Line, Rectangle, Date, Table, Pdf, Warning, Watermark
}

public class LabelElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ElementType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public double Opacity { get; set; } = 1.0;
    public int ZIndex { get; set; }
    public bool IsLocked { get; set; }
    public bool IsVisible { get; set; } = true;
    public string Name { get; set; } = string.Empty;
}
