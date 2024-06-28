using System;
using System.Collections.Generic;

public class HistoryManager
{
    private Stack<HashSet<int>> undoStack = new Stack<HashSet<int>>();
    private Stack<HashSet<int>> redoStack = new Stack<HashSet<int>>();
    private HashSet<int> currentSet;
    private int maxSize;

    public HistoryManager(int maxSize)
    {
        this.maxSize = maxSize;
        currentSet = new HashSet<int>();
    }

    public void Add(HashSet<int> item)
    {
        if (undoStack.Count >= maxSize)
        {
            undoStack.Pop();
        }
        undoStack.Push(new HashSet<int>(currentSet));
        currentSet = new HashSet<int>(item);
        redoStack.Clear();
    }

    public HashSet<int> Undo()
    {
        if (undoStack.Count > 0)
        {
            redoStack.Push(new HashSet<int>(currentSet));
            currentSet = undoStack.Pop();
        }
        return new HashSet<int>(currentSet);
    }

    public HashSet<int> Redo()
    {
        if (redoStack.Count > 0)
        {
            undoStack.Push(new HashSet<int>(currentSet));
            currentSet = redoStack.Pop();
        }
        return new HashSet<int>(currentSet);
    }

    public HashSet<int> GetCurrentState()
    {
        return new HashSet<int>(currentSet);
    }
}