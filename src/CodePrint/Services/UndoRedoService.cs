namespace CodePrint.Services;

/// <summary>
/// Represents a reversible action for the undo/redo system.
/// </summary>
public interface IUndoableAction
{
    string Description { get; }
    void Execute();
    void Undo();
}

/// <summary>
/// Manages an undo/redo history stack.
/// </summary>
public class UndoRedoService
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private const int MaxHistorySize = 100;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    /// <summary>
    /// Executes an action and adds it to the undo history.
    /// </summary>
    public void Execute(IUndoableAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        _redoStack.Clear();

        // Trim history to prevent unbounded growth
        if (_undoStack.Count > MaxHistorySize)
        {
            var temp = new Stack<IUndoableAction>(_undoStack.Take(MaxHistorySize).Reverse());
            _undoStack.Clear();
            foreach (var item in temp) _undoStack.Push(item);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Undoes the most recent action.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Redoes the most recently undone action.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
