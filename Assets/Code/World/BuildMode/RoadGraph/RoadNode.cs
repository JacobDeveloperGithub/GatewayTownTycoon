using UnityEngine;

[System.Serializable]
public class Lane {
    public Transform Start;
    public Transform End;
    public int Degree;
    public Direction RecievingDirection;
    public Direction ConnectionDirection;
}

public sealed class RoadNode : MapObject, IConnectedToRoadGraph, IBuildCost, IRefundCost, IUpkeepCost, IPlaceable, IErase {
    public Lane[] Lanes;
    public bool Intersection;
    public bool StarterRoad;
    public Direction EntranceFacing;
    
    [SerializeField] private int _buildCost = 100;
    [SerializeField] private int _upkeepCost = 2;
    [SerializeField] private int _refundCost = 50;

    public void ConnectToRoadGraph(RoadGraph graph) {
        foreach (Lane l in Lanes) {
            AddLaneToGraph(graph, l);
        }
    }

    public int GetBuildCost() => _buildCost;
    public int GetUpkeepCost() => _upkeepCost;
    public int GetRefundAmount() =>_refundCost;

    public int SetUpkeepCost(int cost) => _upkeepCost = cost;
    
    public void OnPlace() {
        SFXManager.Instance?.PlayBuild();
    }

    public void OnErase() {
        SFXManager.Instance?.PlayErase();
    }

    public bool IsValidConnectionSide(Direction side) => true;

    private void AddLaneToGraph(RoadGraph graph,  Lane lane) {
        if (lane.Start == null || lane.End == null) return;

        Vector2 d0 = DirVec(lane.RecievingDirection);
        Vector2 p0 = lane.Start.position;
        Vector2 p4 = lane.End.position;

        Vector2 p2 = Mathf.Abs(d0.x) > 0.5f ? new(p4.x, p0.y) : new(p0.x, p4.y);
        Vector2 p1 = p0 + (p2 - p0) * 0.3f;
        Vector2 p3 = p4 + (p2 - p4) * 0.3f;

        int n = lane.Degree;
        if (lane.Degree <= 2) {
            RoadGameGraphNode start = graph.GetOrCreate(GridService.Quantize(p0));
            RoadGameGraphNode end = graph.GetOrCreate(GridService.Quantize(p4));
            Vector2 dirNext = (end.Position - start.Position).normalized;
            Vector2 movedTowards = start.Position + dirNext;
            if (StarterRoad && start.Position.magnitude > movedTowards.magnitude) {
                start.StartingNode = true;
                start.DirToNext = (end.Position - start.Position).normalized;
            } else if (StarterRoad && start.Position.magnitude < movedTowards.magnitude){
                end.ExitNode = true;
                end.OnNodeEntry += c=> c.Exit();
            }
            start.AddNeighbor(end);
        } else {
            RoadGameGraphNode prev = graph.GetOrCreate(GridService.Quantize(p0));
            for (int i = 1; i < n; i++) {
                float t = (float)i / n;
                var pi = QuadraticBezier(p1, p2, p3, t);
                var nxt = graph.GetOrCreate(GridService.Quantize(pi));
                prev.AddNeighbor(nxt);
                prev = nxt;
            }
            prev.AddNeighbor(graph.GetOrCreate(GridService.Quantize(p4)));
        }
    }
    
    private Vector2 DirVec(Direction d) => d switch {
        Direction.North => new(0, 1),
        Direction.East  => new(1, 0),
        Direction.South => new(0, -1),
        Direction.West  => new(-1, 0),
        _ => Vector2.zero
    };

    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }
}

