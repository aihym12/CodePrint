namespace CodePrint.Models;

public enum QrContentType { Text, Url, Phone, Email, WiFi, VCard }
public enum QrErrorCorrectionLevel { L, M, Q, H }

public class QrCodeElement : LabelElement
{
    private string _content = "https://example.com";
    private QrContentType _contentType = QrContentType.Text;
    private QrErrorCorrectionLevel _errorCorrection = QrErrorCorrectionLevel.M;
    private int _quietZone = 2;
    private string _foregroundColor = "#000000";
    private string _backgroundColor = "#FFFFFF";
    private string? _logoPath;

    public QrCodeElement() { Type = ElementType.QrCode; Name = "二维码"; Width = 20; Height = 20; }

    public string Content { get => _content; set => SetField(ref _content, value); }
    public QrContentType ContentType { get => _contentType; set => SetField(ref _contentType, value); }
    public QrErrorCorrectionLevel ErrorCorrection { get => _errorCorrection; set => SetField(ref _errorCorrection, value); }
    public int QuietZone { get => _quietZone; set => SetField(ref _quietZone, value); }
    public string ForegroundColor { get => _foregroundColor; set => SetField(ref _foregroundColor, value); }
    public string BackgroundColor { get => _backgroundColor; set => SetField(ref _backgroundColor, value); }
    public string? LogoPath { get => _logoPath; set => SetField(ref _logoPath, value); }
}
