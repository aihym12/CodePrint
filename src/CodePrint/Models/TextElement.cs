namespace CodePrint.Models;

public enum TextAlignment { Left, Center, Right, Justify }
public enum TextDirection { Horizontal, Vertical }

public class TextElement : LabelElement
{
    public TextElement() { Type = ElementType.Text; Name = "文本"; Width = 25; Height = 8; }
    public string Content { get; set; } = "文本";
    public string FontFamily { get; set; } = "Microsoft YaHei";
    public double FontSize { get; set; } = 12;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public bool IsStrikethrough { get; set; }
    public string ForegroundColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "Transparent";
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    public double LetterSpacing { get; set; }
    public double LineSpacing { get; set; } = 1.2;
    public TextDirection Direction { get; set; } = TextDirection.Horizontal;
    public bool IsMultiline { get; set; }
}
