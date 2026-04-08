namespace CodePrint.Models;

public class WarningElement : LabelElement
{
    private string _warningText = "易碎品";
    private string _iconKey = "fragile";
    private bool _showIcon = true;
    private bool _showText = true;

    public WarningElement() { Type = ElementType.Warning; Name = "警示语"; Width = 15; Height = 15; }

    public string WarningText { get => _warningText; set => SetField(ref _warningText, value); }
    public string IconKey { get => _iconKey; set => SetField(ref _iconKey, value); }
    public bool ShowIcon { get => _showIcon; set => SetField(ref _showIcon, value); }
    public bool ShowText { get => _showText; set => SetField(ref _showText, value); }
}
