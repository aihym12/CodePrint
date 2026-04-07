namespace CodePrint.Models;

public class WarningElement : LabelElement
{
    public WarningElement() { Type = ElementType.Warning; Name = "警示语"; Width = 15; Height = 15; }
    public string WarningText { get; set; } = "易碎品";
    public string IconKey { get; set; } = "fragile";
    public bool ShowIcon { get; set; } = true;
    public bool ShowText { get; set; } = true;
}
