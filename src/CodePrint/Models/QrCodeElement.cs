namespace CodePrint.Models;

public enum QrContentType { Text, Url, Phone, Email, WiFi, VCard }
public enum QrErrorCorrectionLevel { L, M, Q, H }

public class QrCodeElement : LabelElement
{
    public QrCodeElement() { Type = ElementType.QrCode; Name = "二维码"; Width = 20; Height = 20; }
    public string Content { get; set; } = "https://example.com";
    public QrContentType ContentType { get; set; } = QrContentType.Text;
    public QrErrorCorrectionLevel ErrorCorrection { get; set; } = QrErrorCorrectionLevel.M;
    public int QuietZone { get; set; } = 2;
    public string ForegroundColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string? LogoPath { get; set; }
}
