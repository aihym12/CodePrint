namespace CodePrint.Models;

public class LinkedQrCodeElement : QrCodeElement
{
    private string _linkedUrl = string.Empty;
    private bool _isDynamic;

    public LinkedQrCodeElement() { Type = ElementType.LinkedQrCode; Name = "关联二维码"; }

    public string LinkedUrl { get => _linkedUrl; set => SetField(ref _linkedUrl, value); }
    public bool IsDynamic { get => _isDynamic; set => SetField(ref _isDynamic, value); }
}
