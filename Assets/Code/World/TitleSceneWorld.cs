using UnityEngine;

public class TitleSceneWorld : MonoBehaviour {
    
    [Header("World Starting Values")]
    [SerializeField] private float _gridSize;
    
    [Header("PlayMode Steps")]
    [SerializeField] private PlayMode _playMode;

    [Header("Services")]
    [SerializeField] private Map _map;

    [Header("Misc")]
    [SerializeField] private Car _carPrefab;

    private CarFactory _carFactory;
    private RoadGraph _graph;
    private TownStatisticsService _townSvc;
    private GridService _grid;

    private void Awake() {
        Application.targetFrameRate = -1;
        _grid = new(_gridSize);
        _map = new(_grid, new());
        _graph = new(_grid, _map);
        _carFactory = new(_grid, _graph, _townSvc, _carPrefab, 5);

        StoreExistingOnLoad();
    }

    private void Start() {
        _playMode.GetDependencies(_carFactory, _graph, _townSvc, _map, null);

        _playMode.CleanupStep();
        _graph.RedrawGraph();
        _playMode.InitStep();
    }

    private void Update() {
        _playMode.RunStep();
    }

    private void StoreExistingOnLoad() {
        foreach (MapObject obj in FindObjectsByType<MapObject>(FindObjectsSortMode.None)) {
            Coordinate c = _grid.GetCoordinate(obj.transform.position);
            if (_map.HasAt(c) && !obj.Locked) {
                Destroy(obj.gameObject);
            } else {
                if (_map.HasAt(c)) _map.EraseAt(c, true);
                obj.transform.position = _grid.PositionFromCoordinate(c);
                obj.transform.localScale = _grid.GetSize() * Vector3.one;
                if (obj is Business b) {
                    b.GetBusinessDependencies(_townSvc);
                }
                _map.PutAt(obj, c, true);
            }
        }
    }

    private void OnDrawGizmos() {
       if (_graph == null || _graph.GetGraph().NodeCount() == 0) return; 
       Gizmos.color = Color.white; 
       foreach (var node in _graph.GetGraph().GetNodes()) { 
            Gizmos.DrawSphere(node.Position, 0.01f); 
            foreach (RoadGameGraphNode n in node.Neighbors) { // draw main line 
                Gizmos.DrawLine(node.Position, n.Position); 
                // simple arrowhead 
                Vector2 dir = (n.Position - node.Position).normalized; 
                Vector3 back = n.Position - dir * 0.05f; // pull back a bit from end 
                Vector3 left = Quaternion.Euler(0, 0, 45) * -dir * 0.03f; 
                Vector3 right = Quaternion.Euler(0, 0, -45) * -dir * 0.03f; 
                Gizmos.DrawLine(n.Position, back + left); 
                Gizmos.DrawLine(n.Position, back + right); 
            }
       }
    } 
}
