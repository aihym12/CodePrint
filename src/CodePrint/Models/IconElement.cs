namespace CodePrint.Models;

public class IconElement : LabelElement
{
    public IconElement() { Type = ElementType.Icon; Name = "图标"; Width = 10; Height = 10; }
    public string IconKey { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
}
