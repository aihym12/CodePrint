namespace CodePrint.Models;

public enum BarcodeFormat
{
    CODE128, CODE39, EAN13, EAN8, UPCA, ITF14, Codabar
}

public class BarcodeElement : LabelElement
{
    public BarcodeElement() { Type = ElementType.Barcode; Name = "条码"; Width = 40; Height = 15; }
    public string Content { get; set; } = "1234567890";
    public BarcodeFormat Format { get; set; } = BarcodeFormat.CODE128;
    public bool ShowText { get; set; } = true;
    public bool TextOnBottom { get; set; } = true;
    public string ForegroundColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public double BarHeight { get; set; } = 10;
    public double NarrowWidthRatio { get; set; } = 1.0;
}
