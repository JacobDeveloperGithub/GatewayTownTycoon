using UnityEngine;

public abstract class MapObject : MonoBehaviour {
    public bool Locked = false;
    public MapObject PrefabSelf;

    protected GridService _grid;
    protected Coordinate _coordinate;

    public void SetDependencies(GridService grid) {
        _grid = grid;
    }

    public void SetCoordinate(Coordinate c) => _coordinate = c;
    public Coordinate GetCoordinate() => _coordinate;
}