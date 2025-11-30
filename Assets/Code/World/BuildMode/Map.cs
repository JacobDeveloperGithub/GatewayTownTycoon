using System.Collections.Generic;
using UnityEngine;

public class Map {
    private Dictionary<Coordinate, MapObject> _objects;
    private GridService _grid;
    private Bounds _worldBounds;

    public Map(GridService grid, Bounds bounds) {
        _objects = new();
        _grid = grid;
        _worldBounds = bounds;
    }

    public void PutAt(MapObject obj, Coordinate coordinate, bool replace = false) {
        _objects.TryAdd(coordinate, obj);
        obj.SetDependencies(_grid);
        obj.SetCoordinate(coordinate);
        if (!replace && obj is IPlaceable place) place.OnPlace();
    }
    
    public void EraseAt(Coordinate coordinate, bool replace = false) {
        if(!_objects.TryGetValue(coordinate, out MapObject obj)) return;
        if (!replace && obj is IErase erase) erase.OnErase();   
        _objects.Remove(coordinate);
        UnityEngine.GameObject.Destroy(obj.gameObject);
    }
    
    public MapObject[] AllObjects() {
        MapObject[] values = new MapObject[_objects.Count];
        _objects.Values.CopyTo(values, 0);
        return values;
    }

    public void Shuffle() {
        MapObject[] objects = AllObjects();
        _objects.Clear();
        foreach (MapObject mo in objects) {
            if (mo.Locked) {
                PutAt(mo, mo.GetCoordinate(), true);
                continue;
            }
            Vector2 pos;
            Coordinate at;
            do {
                pos = new(Random.Range(_worldBounds.min.x, _worldBounds.max.x), Random.Range(_worldBounds.min.y, _worldBounds.max.y));
                at = _grid.GetCoordinate(pos);
            } while (HasAt(at));
            mo.transform.position = _grid.PositionFromCoordinate(at);
            PutAt(mo, at, true);
        } 
    } 

    public bool HasAt(Coordinate coordinate) => _objects.ContainsKey(coordinate);
    public MapObject GetAt(Coordinate coordinate) {
        if (!_objects.TryGetValue(coordinate, out MapObject obj)) return null;
        return obj;
    }
}