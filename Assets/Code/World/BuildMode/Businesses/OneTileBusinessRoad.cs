using UnityEngine;

public class OneTileBusinessRoad : MonoBehaviour {
    [SerializeField] private Lane Entry;
    [SerializeField] private Lane Exit;
    private RoadGameGraphNode _entrance;
    private RoadGameGraphNode _exitSpawn;

    public void ConnectToRoadGraph(Vector2 center, float gridSize, RoadGraph graph) {
        AddLaneToGraph(center, gridSize, graph, Entry, true);
        AddLaneToGraph(center, gridSize, graph, Exit, false);
    }

    public RoadGameGraphNode GetExit() => _exitSpawn;
    public RoadGameGraphNode GetEntrance() => _entrance;
    
    private void AddLaneToGraph(Vector2 center, float gridSize, RoadGraph graph, Lane lane, bool isEntrance) {
        Vector2 p0 = GridService.Quantize(center + gridSize * (Vector2)lane.Start.position);
        Vector2 p1 = GridService.Quantize(center + gridSize * (Vector2)lane.End.position);

        RoadGameGraphNode start = graph.GetOrCreate(p0);
        RoadGameGraphNode end = graph.GetOrCreate(p1);
        if (isEntrance) {
            end.Destination = true;
            _entrance = end;
        } else {
            _exitSpawn = end;
        }
        start.AddNeighbor(end);
    }
}
