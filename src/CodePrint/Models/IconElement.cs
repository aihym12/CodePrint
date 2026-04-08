namespace CodePrint.Models;

public class IconElement : LabelElement
{
    private string _iconKey = string.Empty;
    private string _color = "#000000";

    public IconElement() { Type = ElementType.Icon; Name = "图标"; Width = 10; Height = 10; }

    public string IconKey { get => _iconKey; set => SetField(ref _iconKey, value); }
    public string Color { get => _color; set => SetField(ref _color, value); }
}
