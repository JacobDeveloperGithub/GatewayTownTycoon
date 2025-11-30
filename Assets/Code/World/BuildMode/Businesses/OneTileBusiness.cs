using UnityEngine;

[System.Serializable] 
public struct OneTileBusinessOrientationContext {
    public OneTileBusinessRoad[] Orientations;
    public GameObject[] Visuals;

    public readonly OneTileBusinessRoad GetOrientation(Direction d) => d switch {
        Direction.North => Orientations[0],
        Direction.South => Orientations[1],
        Direction.East => Orientations[2],
        Direction.West => Orientations[3],
        _ => Orientations[0]
    };
    
    public readonly GameObject GetPrefab(Direction d) => d switch {
        Direction.North => Visuals[0],
        Direction.South => Visuals[1],
        Direction.East => Visuals[2],
        Direction.West => Visuals[3],
        _ => Visuals[0]
    };
}

public class OneTileBusiness : CarBusiness {
    public OneTileBusinessOrientationContext OrientationContext;
    private RoadGameGraphNode _exit;
    private RoadGameGraphNode _entrance;
    private OneTileBusinessRoad _activeOrientation;
    private GameObject _render;

    protected override void Start() {
        base.Start();
        _activeOrientation = OrientationContext.GetOrientation(EntranceFacing);
        if (_render) Destroy(_render);
        _render = Instantiate(OrientationContext.GetPrefab(EntranceFacing), transform.position, Quaternion.identity);
        _render.transform.parent = transform;
        _render.transform.localScale = new(1,1,1);
    }

    public override void ConnectToRoadGraph(RoadGraph graph) {
        _activeOrientation.ConnectToRoadGraph(transform.position, _grid.GetSize(), graph);
        _entrance = _activeOrientation.GetEntrance();
        _exit = _activeOrientation.GetExit();
        _entrance.OnNodeEntry += (c) => OnBusinessEnter(c);
    }

    public override void OnBusinessExit(Car c) {
        base.OnBusinessExit(c);
        c.ExitBusiness(_exit);
    }

    public override void OnErase() {
        base.OnErase();
        if (_render) Destroy(_render);
    }

    public override void Rotate() {
        base.Rotate();
        _activeOrientation = OrientationContext.GetOrientation(EntranceFacing);
        if (_render) Destroy(_render);
        _render = Instantiate(OrientationContext.GetPrefab(EntranceFacing), transform.position, Quaternion.identity);
        _render.transform.parent = transform;
        _render.transform.localScale = new(1,1,1);
    }
}
