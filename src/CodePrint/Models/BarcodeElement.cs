namespace CodePrint.Models;

public enum BarcodeFormat
{
    CODE128, CODE39, EAN13, EAN8, UPCA, ITF14, Codabar
}

public class BarcodeElement : LabelElement
{
    private string _content = "1234567890";
    private BarcodeFormat _format = BarcodeFormat.CODE128;
    private bool _showText = true;
    private bool _textOnBottom = true;
    private string _foregroundColor = "#000000";
    private string _backgroundColor = "#FFFFFF";
    private double _barHeight = 10;
    private double _narrowWidthRatio = 1.0;

    public BarcodeElement() { Type = ElementType.Barcode; Name = "条码"; Width = 40; Height = 15; }

    public string Content { get => _content; set => SetField(ref _content, value); }
    public BarcodeFormat Format { get => _format; set => SetField(ref _format, value); }
    public bool ShowText { get => _showText; set => SetField(ref _showText, value); }
    public bool TextOnBottom { get => _textOnBottom; set => SetField(ref _textOnBottom, value); }
    public string ForegroundColor { get => _foregroundColor; set => SetField(ref _foregroundColor, value); }
    public string BackgroundColor { get => _backgroundColor; set => SetField(ref _backgroundColor, value); }
    public double BarHeight { get => _barHeight; set => SetField(ref _barHeight, value); }
    public double NarrowWidthRatio { get => _narrowWidthRatio; set => SetField(ref _narrowWidthRatio, value); }
}
