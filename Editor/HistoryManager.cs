using System.Collections.Generic;
using System.Linq;

public class HistoryManager
{
    private const int MaxStackSize = 20;
    private Stack<HashSet<int>> undoStack = new Stack<HashSet<int>>();
    private Stack<HashSet<int>> redoStack = new Stack<HashSet<int>>();

    public void AddState(HashSet<int> state)
    {
        undoStack.Push(new HashSet<int>(state));
        if (undoStack.Count > MaxStackSize)
        {
            undoStack = new Stack<HashSet<int>>(undoStack.Take(MaxStackSize));
        }
        redoStack.Clear();
    }

    public HashSet<int> Undo(HashSet<int> currentState)
    {
        if (undoStack.Count > 0)
        {
            redoStack.Push(new HashSet<int>(currentState));
            if (redoStack.Count > MaxStackSize)
            {
                redoStack = new Stack<HashSet<int>>(redoStack.Take(MaxStackSize));
            }
            currentState = undoStack.Pop();
        }
        return new HashSet<int>(currentState);
    }

    public HashSet<int> Redo(HashSet<int> currentState)
    {
        if (redoStack.Count > 0)
        {
            undoStack.Push(new HashSet<int>(currentState));
            if (undoStack.Count > MaxStackSize)
            {
                undoStack = new Stack<HashSet<int>>(undoStack.Take(MaxStackSize));
            }
            currentState = redoStack.Pop();
        }
        return new HashSet<int>(currentState);
    }
}

public class TriangleSelectionManager
{
    private HistoryManager _history;
    private HashSet<int> _allTriangleIndices;
    private HashSet<int> _selectedTriangleIndices;

    public TriangleSelectionManager(HashSet<int> allTriangleIndices)
    {
        _history = new HistoryManager();
        _allTriangleIndices = new HashSet<int>(allTriangleIndices);
        _selectedTriangleIndices = new HashSet<int>();
    }

    public void SelectAllTriangles()
    {
        _history.AddState(_selectedTriangleIndices);
        _selectedTriangleIndices = new HashSet<int>(_allTriangleIndices);
    }

    public void UnselectAllTriangles()
    {
        _history.AddState(_selectedTriangleIndices);
        _selectedTriangleIndices = new HashSet<int>();
    }

    public void ReverseAllTriangles()
    {
        _history.AddState(_selectedTriangleIndices);
        var reversedSelection = new HashSet<int>(_allTriangleIndices);
        reversedSelection.ExceptWith(_selectedTriangleIndices);
        _selectedTriangleIndices = reversedSelection;
    }

    public void UpdateSelection(HashSet<int> indices, bool isPreviewSelected)
    {
        _history.AddState(_selectedTriangleIndices);
        if (isPreviewSelected)
        {
            _selectedTriangleIndices.ExceptWith(indices);
        }
        else
        {
            _selectedTriangleIndices.UnionWith(indices);
        }
    }

    public void Undo()
    {
        _selectedTriangleIndices = _history.Undo(_selectedTriangleIndices);
    }

    public void Redo()
    {
        _selectedTriangleIndices = _history.Redo(_selectedTriangleIndices);
    }

    public HashSet<int> GetAllTriangles()
    {
        return new HashSet<int>(_allTriangleIndices);
    }

    public HashSet<int> GetSelectedTriangles()
    {
        return new HashSet<int>(_selectedTriangleIndices);
    }

    public HashSet<int> GetUnselectedTriangles()
    {
        var unselectedTriangles = new HashSet<int>(_allTriangleIndices);
        unselectedTriangles.ExceptWith(_selectedTriangleIndices);
        return new HashSet<int>(unselectedTriangles);
    }

    public HashSet<int> GetUniqueTriangles(HashSet<int> triangleIndices, bool isPreviewSelected)
    {
        if (isPreviewSelected)
        {
            return triangleIndices.Intersect(_selectedTriangleIndices).ToHashSet();
        }
        else
        {
            return triangleIndices.Intersect(GetUnselectedTriangles()).ToHashSet();
        }
    }
}
