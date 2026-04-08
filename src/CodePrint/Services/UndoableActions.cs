using System.Collections.ObjectModel;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// Undoable action for adding an element to the document.
/// </summary>
public class AddElementAction : IUndoableAction
{
    private readonly ObservableCollection<LabelElement> _elements;
    private readonly LabelElement _element;

    public string Description => $"添加 {_element.Name}";

    public AddElementAction(ObservableCollection<LabelElement> elements, LabelElement element)
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
    private readonly ObservableCollection<LabelElement> _elements;
    private readonly LabelElement _element;
    private int _index;

    public string Description => $"删除 {_element.Name}";

    public RemoveElementAction(ObservableCollection<LabelElement> elements, LabelElement element)
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

    public void Execute()
    {
        _element.X = _newX;
        _element.Y = _newY;
    }

    public void Undo()
    {
        _element.X = _oldX;
        _element.Y = _oldY;
    }
}

/// <summary>
/// Undoable action for resizing an element (including any position change caused by the resize).
/// </summary>
public class ResizeElementAction : IUndoableAction
{
    private readonly LabelElement _element;
    private readonly double _oldX, _oldY, _oldW, _oldH;
    private readonly double _newX, _newY, _newW, _newH;

    public string Description => $"调整 {_element.Name} 大小";

    public ResizeElementAction(LabelElement element,
        double oldX, double oldY, double oldW, double oldH,
        double newX, double newY, double newW, double newH)
    {
        _element = element;
        _oldX = oldX;
        _oldY = oldY;
        _oldW = oldW;
        _oldH = oldH;
        _newX = newX;
        _newY = newY;
        _newW = newW;
        _newH = newH;
    }

    public void Execute()
    {
        _element.X = _newX;
        _element.Y = _newY;
        _element.Width = _newW;
        _element.Height = _newH;
    }

    public void Undo()
    {
        _element.X = _oldX;
        _element.Y = _oldY;
        _element.Width = _oldW;
        _element.Height = _oldH;
    }
}

/// <summary>
/// Undoable action that groups multiple actions as a single undo/redo unit.
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> _actions;

    public string Description { get; }

    public CompositeAction(string description, IEnumerable<IUndoableAction> actions)
    {
        Description = description;
        _actions = actions.ToList();
    }

    public void Execute()
    {
        foreach (var action in _actions)
            action.Execute();
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _actions.Count - 1; i >= 0; i--)
            _actions[i].Undo();
    }
}

/// <summary>
/// Undoable action for changing a single property on an element.
/// </summary>
public class PropertyChangeAction<T> : IUndoableAction
{
    private readonly LabelElement _element;
    private readonly Action<LabelElement, T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;

    public string Description { get; }

    public PropertyChangeAction(LabelElement element, string description,
        Action<LabelElement, T> setter, T oldValue, T newValue)
    {
        _element = element;
        Description = description;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Execute() => _setter(_element, _newValue);
    public void Undo() => _setter(_element, _oldValue);
}
