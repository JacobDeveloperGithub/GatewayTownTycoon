using UnityEngine;

public class CarFactory {
    private Car _prefab;
    private TownStatisticsService _townSvc;
    private RoadGraph _graphService;
    private GridService _grid;

    private int _factoryProducedCount = 0;
    private float _speed = -1;

    public CarFactory(GridService grid, RoadGraph graph, TownStatisticsService town, Car prefab, float speed = -1) {
        _graphService = graph;
        _townSvc = town;
        _grid = grid;
        _prefab = prefab;
        _speed = speed;
    }

    public Car CreateCar(RoadGameGraphNode start) {
        RoadGameGraphNode e = null;
        do {
            e = _graphService.GetRandomExit();
        } while (_grid.GetCoordinate(e.Position).Equals(_grid.GetCoordinate(start.Position)));
        float minChance = -1;
        if (_townSvc != null) minChance = Mathf.Min(8, Mathf.Max(2, (float)_townSvc.GetTownRating() / 10));
        if (UnityEngine.Random.Range(1,10) <= minChance && _graphService.GetDestinations().Count != 0) {
            e = _graphService.GetRandomDestination();    
        }
        Car car = GameObject.Instantiate(_prefab);
        Vector3 startPos = start.Position;
        Vector3 moveDir = -start.DirToNext;
        int numHit = 0;//Physics2D.RaycastAll(startPos, moveDir).Length + 1;
        startPos += car.transform.localScale.y * 1.1f * numHit * moveDir;
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        car.transform.SetPositionAndRotation(startPos, Quaternion.Euler(0, 0, angle));
        car.transform.localScale *= _grid.GetSize();
        car.name = $"Car {_factoryProducedCount++}";
        car.Initialize(_graphService);
        car.DriveTo(e);
        if (_speed != -1) car.SetSpeed(_speed);
        return car;
    }

    public void ResetCar(Car c, RoadGameGraphNode start) {
        RoadGameGraphNode e = null;
        do {
            e = _graphService.GetRandomExit();
        } while (_grid.GetCoordinate(e.Position).Equals(_grid.GetCoordinate(start.Position)));
        float minChance = -1;
        if (_townSvc != null) minChance = Mathf.Min(8, Mathf.Max(2, (float)_townSvc.GetTownRating() / 10));
        if (UnityEngine.Random.Range(1,10) <= minChance && _graphService.GetDestinations().Count != 0) {
            e = _graphService.GetRandomDestination();    
        }
        Vector3 startPos = start.Position;
        Vector3 moveDir = -start.DirToNext;
        int numHit = 0;//Physics2D.RaycastAll(startPos, moveDir).Length + 1;
        startPos += c.transform.localScale.y * 1.1f * numHit * moveDir;
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        c.transform.SetPositionAndRotation(startPos, Quaternion.Euler(0, 0, angle));
        c.name = $"Car {_factoryProducedCount++}";
        c.Initialize(_graphService);
        c.DriveTo(e);
        if (_speed != -1) c.SetSpeed(_speed);
    }
}