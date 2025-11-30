using System.Collections.Generic;
using UnityEngine;

public sealed class RoadGraph {
    private Graph<RoadGameGraphNode> _graph;
    private GridService _grid;
    private Map _map;

    private Dictionary<Vector2, RoadGameGraphNode> _nodes;
    private List<RoadGameGraphNode> _entrances;
    private List<RoadGameGraphNode> _mapExits;
    private List<RoadGameGraphNode> _destinations;


    public RoadGraph(GridService grid, Map map) {
        _graph = new(10);
        _nodes = new();
        _grid = grid;
        _map = map;
    }

    public Graph<RoadGameGraphNode> GetGraph() => _graph;
    public RoadGameGraphNode GetRandomEntrance() => _entrances[Random.Range(0, _entrances.Count)];
    public RoadGameGraphNode GetRandomExit() => _mapExits[Random.Range(0, _mapExits.Count)];
    public RoadGameGraphNode GetRandomDestination() => _destinations[Random.Range(0, _destinations.Count)];
    public List<RoadGameGraphNode> GetEntrances() => _entrances;
    public List<RoadGameGraphNode> GetExits() => _mapExits;
    public List<RoadGameGraphNode> GetDestinations() => _destinations;

    public void RedrawGraph() {
        _graph = new(10);
        _nodes = new();

        _entrances = new();
        _mapExits = new();
        _destinations = new();
        foreach (MapObject obj in _map.AllObjects()) {
            if (!obj.TryGetComponent(out IConnectedToRoadGraph iConnect)) continue;
            iConnect.ConnectToRoadGraph(this);
        }
        
        foreach (RoadGameGraphNode node in GetGraph().GetNodes()) {
            if (node.StartingNode) _entrances.Add(node);
            if (node.ExitNode) _mapExits.Add(node);
            if (node.Destination) _destinations.Add(node);
        }
        _graph.ResizeSolver();
        RoadGameGraphNode[] cpy = new RoadGameGraphNode[_destinations.Count];
        _destinations.CopyTo(cpy);
        foreach (RoadGameGraphNode destination in cpy) {
            if (_graph.GetPath(_entrances[0], destination).Length == 0) _destinations.Remove(destination);
        }
    }

    public bool IsMapValid() {
        if (_entrances == null || _mapExits == null) return false;
        foreach (RoadGameGraphNode entrance in _entrances) {
            foreach (RoadGameGraphNode exit in _mapExits) {
                if (_grid.GetCoordinate(entrance.Position).Equals(_grid.GetCoordinate(exit.Position))) continue;
                if (_graph.GetPath(entrance, exit).Length == 0) return false;
            }
        }
        return true;
    }

    public RoadGameGraphNode GetOrCreate(Vector3 pos) {
        if (_nodes.TryGetValue(pos, out var n)) return n;
        n = new RoadGameGraphNode(pos);
        _nodes.Add(pos, n);
        _graph.RegisterNode(n);
        return n;
    }
}