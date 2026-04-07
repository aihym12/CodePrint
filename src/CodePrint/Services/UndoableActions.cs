using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// Undoable action for adding an element to the document.
/// </summary>
public class AddElementAction : IUndoableAction
{
    private readonly List<LabelElement> _elements;
    private readonly LabelElement _element;

    public string Description => $"添加 {_element.Name}";

    public AddElementAction(List<LabelElement> elements, LabelElement element)
    {
        _elements = elements;
        _element = element;
    }

    public void Execute() => _elements.Add(_element);
    public void Undo() => _elements.Remove(_element);
}

/// <summary>
/// Undoable action for removing an element from the document.
/// </summary>
public class RemoveElementAction : IUndoableAction
{
    private readonly List<LabelElement> _elements;
    private readonly LabelElement _element;
    private int _index;

    public string Description => $"删除 {_element.Name}";

    public RemoveElementAction(List<LabelElement> elements, LabelElement element)
    {
        _elements = elements;
        _element = element;
    }

    public void Execute()
    {
        _index = _elements.IndexOf(_element);
        _elements.Remove(_element);
    }

    public void Undo()
    {
        if (_index >= 0 && _index <= _elements.Count)
            _elements.Insert(_index, _element);
        else
            _elements.Add(_element);
    }
}

/// <summary>
/// Undoable action for moving an element.
/// </summary>
public class MoveElementAction : IUndoableAction
{
    private readonly LabelElement _element;
    private readonly double _oldX, _oldY;
    private readonly double _newX, _newY;

    public string Description => $"移动 {_element.Name}";

    public MoveElementAction(LabelElement element, double oldX, double oldY, double newX, double newY)
    {
        _element = element;
        _oldX = oldX;
        _oldY = oldY;
        _newX = newX;
        _newY = newY;
    }

    public void Execute() { _element.X = _newX; _element.Y = _newY; }
    public void Undo() { _element.X = _oldX; _element.Y = _oldY; }
}

/// <summary>
/// Undoable action for resizing an element.
/// </summary>
public class ResizeElementAction : IUndoableAction
{
    private readonly LabelElement _element;
    private readonly double _oldW, _oldH;
    private readonly double _newW, _newH;

    public string Description => $"调整 {_element.Name} 大小";

    public ResizeElementAction(LabelElement element, double oldW, double oldH, double newW, double newH)
    {
        _element = element;
        _oldW = oldW;
        _oldH = oldH;
        _newW = newW;
        _newH = newH;
    }

    public void Execute() { _element.Width = _newW; _element.Height = _newH; }
    public void Undo() { _element.Width = _oldW; _element.Height = _oldH; }
}
