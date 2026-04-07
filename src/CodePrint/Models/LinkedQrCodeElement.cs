namespace CodePrint.Models;

public class LinkedQrCodeElement : QrCodeElement
{
    public LinkedQrCodeElement() { Type = ElementType.LinkedQrCode; Name = "关联二维码"; }
    public string LinkedUrl { get; set; } = string.Empty;
    public bool IsDynamic { get; set; }
}
