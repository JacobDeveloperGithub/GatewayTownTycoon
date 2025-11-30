using System;
using System.Collections.Generic;
using UnityEngine;

public enum ActionType {Place, Delete, Rotate}

[Serializable]
public struct UndoableBuildAction {
    public Coordinate ActionPosition;
    public MapObject ObjectCopy;
    public int ActionValue;
    public ActionType Type;
}

public class UndoSystem {
    private Stack<UndoableBuildAction> _undoStack;
    private Map _map;
    private GridService _grid;
    private TownStatisticsService _svc;

    public UndoSystem(GridService grid, Map map, TownStatisticsService svc) {
        _undoStack = new();
        _map = map;
        _grid = grid;
        _svc = svc; 
    }

    public void Clear() => _undoStack.Clear();

    public void EnqueueAction(UndoableBuildAction action) => _undoStack.Push(action);

    public void UndoOne() {
        if (_undoStack.Count <= 0) return;
        UndoableBuildAction action = _undoStack.Pop();
        if (action.Type == ActionType.Place) {
            _map.EraseAt(action.ActionPosition);
            _svc.AddMoney(action.ActionValue);
        } else if (action.Type == ActionType.Delete) {
            _map.PutAt(GameObject.Instantiate(action.ObjectCopy), action.ActionPosition);
            MapObject mo = _map.GetAt(action.ActionPosition);
            mo.PrefabSelf = action.ObjectCopy.PrefabSelf;
            if (mo is Business b) {
                b.GetBusinessDependencies(_svc);
            }
            mo.transform.localScale = _grid.GetSize() * Vector3.one;
            mo.transform.position = _grid.PositionFromCoordinate(action.ActionPosition);
            _svc.SpendMoney(action.ActionValue);
        } else {
            MapObject mo = _map.GetAt(action.ActionPosition);
            if (mo is IRotate r) {
                r.Rotate();
                r.Rotate();
                r.Rotate();
            }
        }
    }
}
