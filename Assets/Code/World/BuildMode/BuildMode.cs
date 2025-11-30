using FSA;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RoadRuleTiles))]
public class BuildMode : MonoBehaviour, IScheduled {
    private enum Modes {None, DrawRoads, PlaceBusiness, Clean, Erase, RotationMode}
    private StateMachine _machine;

    [SerializeField] private RoadNode _placeholder;

    [SerializeField] private BuildMenuUI _ui;

    [SerializeField] private Sprite _roadMenuSprite;
    [SerializeField] private Sprite _eraseMenuSprite;
    [SerializeField] private Sprite _cleanDecorSprite;
    [SerializeField] private Sprite _businessSprite;
    [SerializeField] private Sprite _rotateSprite;
    [SerializeField] private Sprite _gridSprite;

    [SerializeField] private Material _gridMaterial;
    [SerializeField] private GameObject _tilePreview;

    [SerializeField] private BusinessMenuUI _businessUI;
    [SerializeField] private Business[] _buildableBusinesses;

    private Map _map;
    private GridService _grid;
    private RoadRuleTiles _roadRules;
    private TownStatisticsService _townSvc;
    private Camera _camera;
    private Bounds _levelEditorBounds;
    private UndoSystem _undo;

    private GameObject _previewObject;
    private Business _selectedBusiness;
    private bool _renderGrid = false;
    private bool _evaluatedSince = false;

    public void GetDependencies(GridService grid, Map map, TownStatisticsService stats, Bounds worldBounds) {
        _map = map;
        _grid = grid;
        _townSvc = stats;
        _camera = Camera.main;
        _levelEditorBounds = worldBounds;
        _undo = new(grid, map, stats);
    }

    private void Awake() {
        _machine = new StateMachineBuilder()
            .WithState(Modes.None)

            .WithState(Modes.DrawRoads)
            .WithOnRun(DrawRoads)

            .WithState(Modes.PlaceBusiness)
            .WithOnRun(DrawBusinesses)
            .WithOnExit(() => {if (_previewObject) Destroy(_previewObject);})

            .WithState(Modes.Clean)
            .WithOnRun(CleanDecor)
            
            .WithState(Modes.Erase)
            .WithOnRun(EraseObjects)
            
            .WithState(Modes.RotationMode)
            .WithOnRun(RotateBusinesses)
            
            .Build();
        
        _roadRules = GetComponent<RoadRuleTiles>();
        _ui.AddButtonToScrollViewMenu(_roadMenuSprite, () => {
            if (_machine.IsInState(Modes.DrawRoads)) _machine.SetState(Modes.None, true);
            else _machine.SetState(Modes.DrawRoads, true);
            if (_businessUI.IsEnabled()) _businessUI.Disable();
        });
        
        _ui.AddButtonToScrollViewMenu(_eraseMenuSprite, () => {
            if (_machine.IsInState(Modes.Erase)) _machine.SetState(Modes.None, true);
            else _machine.SetState(Modes.Erase, true);
            if (_businessUI.IsEnabled()) _businessUI.Disable();
        });
        
        _ui.AddButtonToScrollViewMenu(_cleanDecorSprite, () => {
            if (_machine.IsInState(Modes.Clean)) _machine.SetState(Modes.None, true);
            else _machine.SetState(Modes.Clean, true);
            if (_businessUI.IsEnabled()) _businessUI.Disable();
        });

        _ui.AddButtonToScrollViewMenu(_businessSprite, () => {
            if (_businessUI.IsEnabled()) _businessUI.Disable();
            else EnableBusinessMenu();
            _machine.SetState(Modes.None, true);
        });
        
        _ui.AddButtonToScrollViewMenu(_rotateSprite, () => {
            if (_machine.IsInState(Modes.RotationMode)) _machine.SetState(Modes.None, true);
            else _machine.SetState(Modes.RotationMode, true);
            if (_businessUI.IsEnabled()) _businessUI.Disable();
        });
        
        _ui.AddButtonToScrollViewMenu(_gridSprite, () => {
            _renderGrid = !_renderGrid;
            _machine.SetState(Modes.None, true);
        });
        
        foreach (Business b in _buildableBusinesses) {
            _businessUI.AddButtonToScrollViewMenu(b.ShopContext, () => {
                _selectedBusiness = b;
                _previewObject = Instantiate(_tilePreview);
                _previewObject.transform.localScale = _grid.GetSize() * Vector3.one;
                _machine.SetState(Modes.PlaceBusiness);
                _businessUI.Disable();
            });
        }
    }

    public void InitStep() => _machine.SetState(Modes.None);
    public void ClearMode() => _machine.SetState(Modes.None, true); 
    public void RunStep() {
        _machine.RunStateMachine(Time.deltaTime);
        if (ControlsService.Instance.UndoPressed()) {
            _undo.UndoOne();
            _evaluatedSince = false;
        }
        if (ShouldEvalutate()) EvaluateAll();
        DrawGrid();
    }
    
    public void CleanupStep() {
        _renderGrid = false;
        _undo.Clear();
    }


    private void EnableBusinessMenu() => _businessUI.Enable();
    private void EvaluateAll() => EvaluateAllRoads();
    private void OnRenderObject() {
        if (_renderGrid) DrawGrid();
    }

    private bool ValidPosition(Vector2 pos) => _levelEditorBounds.Contains(pos);
    private bool IsMousePressed() => ControlsService.Instance.IsLeftMouseClicked();
    private bool WasMouseClicked() => ControlsService.Instance.LeftMouseClicked();
    private bool RightMouseClicked() => ControlsService.Instance.RightMouseClicked();
    private bool ShouldEvalutate() => IsMousePressed() && !_evaluatedSince;
    
    private Vector2 MouseWorldPosition() {
        Vector2 pos = ControlsService.Instance.MousePosition();
        return _camera.ScreenToWorldPoint(pos);
    }

    private void DrawRoads() {
        if (RightMouseClicked()) {
            _machine.SetState(Modes.None, true);
            return;
        }
        if (!IsMousePressed()) return;
        Vector2 worldPos = MouseWorldPosition();
        if (!ValidPosition(worldPos)) return;
        Coordinate c = _grid.GetCoordinate(worldPos);
        if (_map.HasAt(c)) return;
        if (_placeholder is IBuildCost cost) {
            if (!_townSvc.HasEnoughMoney(cost.GetBuildCost())) {
                NotificationManager.Instance.EnqueueNotification("Can't afford road.");
                return;
            }
            _townSvc.SpendMoney(cost.GetBuildCost());
            _undo.EnqueueAction(new() {
                ActionPosition = c,
                ActionValue = cost.GetBuildCost(),
                Type = ActionType.Place
            });
        }
        SpawnGenericAtPosition(c);
        _evaluatedSince = false;
    }

    private void EraseObjects() {
        if (RightMouseClicked()) {
            _machine.SetState(Modes.None, true);
            return;
        }
        if (!IsMousePressed()) return;
        Vector2 worldPos = MouseWorldPosition();
        if (!ValidPosition(worldPos)) return;
        Coordinate c = _grid.GetCoordinate(worldPos);
        MapObject obj = _map.GetAt(c);
        if (obj == null || obj is not IErase e || obj is Decor) return;
        if (obj is RoadNode node &&  node.Locked) return;
        if (obj is IRefundCost refund) {
            _townSvc.AddMoney(refund.GetRefundAmount());
            _undo.EnqueueAction(new() {
                ActionPosition = c,
                ActionValue = refund.GetRefundAmount(),
                ObjectCopy = obj.PrefabSelf,
                Type = ActionType.Delete
            });
        }
        _map.EraseAt(_grid.GetCoordinate(worldPos));
        _evaluatedSince = false;
    }
    
    private void CleanDecor() {
        if (RightMouseClicked()) {
            _machine.SetState(Modes.None, true);
            return;
        }
        if (!IsMousePressed()) return;
        Vector2 worldPos = MouseWorldPosition();
        if (!ValidPosition(worldPos)) return;
        Coordinate c = _grid.GetCoordinate(worldPos);
        MapObject obj = _map.GetAt(c);
        if (obj == null || obj is not Decor d) return;
        if (!_townSvc.HasEnoughMoney(500)) {
            NotificationManager.Instance.EnqueueNotification("Can't afford to remove.");
            _machine.SetState(Modes.None, true);
            return;
        }
        _townSvc.SpendMoney(500);
        _undo.EnqueueAction(new() {
            ActionPosition = c,
            ActionValue = -500,
            ObjectCopy = obj.PrefabSelf,
            Type = ActionType.Delete
        });
        _map.EraseAt(_grid.GetCoordinate(worldPos));
        _evaluatedSince = false;
    }
    
    private void DrawBusinesses() {
        if (RightMouseClicked()) {
            Destroy(_previewObject);
            _machine.SetState(Modes.None, true);
            _selectedBusiness = null;
            return;
        }
        Vector2 worldPos = MouseWorldPosition();
        if (ValidPosition(worldPos)) _previewObject.transform.position = _grid.PositionFromCoordinate(_grid.GetCoordinate(MouseWorldPosition()));
        else _previewObject.transform.position = Vector3.one * 999;
        if (!WasMouseClicked()) return;
        if (!ValidPosition(worldPos)) {
            NotificationManager.Instance.EnqueueNotification("Attempted to place business outside of playable area. Toggle gridmode for assistance.");
            Destroy(_previewObject);
            _machine.SetState(Modes.None, true);
            _selectedBusiness = null;
            return;
        }
        Coordinate c = _grid.GetCoordinate(worldPos);
        if(_map.HasAt(c)) return;
        foreach (MapObject mo in _map.AllObjects()) {
            if (mo is not Business) continue;
            Coordinate diff = mo.GetCoordinate() - c;
            if (Mathf.Abs(diff.y) + Mathf.Abs(diff.x) <= 2) {
                if (Mathf.Abs(diff.y) == 2 || Mathf.Abs(diff.x) == 2) continue;
                NotificationManager.Instance.EnqueueNotification("Business too close to another. Must be atleast 1 tile away in all directions");
                Destroy(_previewObject);
                _machine.SetState(Modes.None, true);
                _selectedBusiness = null;
                return;
            }
        }
        if (_selectedBusiness is IBuildCost cost) {
            if (!_townSvc.HasEnoughMoney(cost.GetBuildCost())) {
                NotificationManager.Instance.EnqueueNotification("Can't afford business.");
                Destroy(_previewObject);
                _machine.SetState(Modes.None, true);
                _selectedBusiness = null;
                return;
            }
            _townSvc.SpendMoney(cost.GetBuildCost());
            _undo.EnqueueAction(new() {
                ActionPosition = c,
                ActionValue = cost.GetBuildCost(),
                Type = ActionType.Place
            });
        }
        Business b = Instantiate(_selectedBusiness);
        b.PrefabSelf = _selectedBusiness.PrefabSelf;
        b.GetBusinessDependencies(_townSvc);
        b.transform.localScale = _grid.GetSize() * Vector3.one;
        b.transform.position = _grid.PositionFromCoordinate(c);
        _map.PutAt(b, c);
        _evaluatedSince = false;
    }
    
    private void RotateBusinesses() {
        if (RightMouseClicked()) {
            _machine.SetState(Modes.None, true);
            return;
        }
        if (!WasMouseClicked()) return;
        Vector2 worldPos = MouseWorldPosition();
        if (!ValidPosition(worldPos)) return;
        Coordinate c = _grid.GetCoordinate(worldPos);
        if (!_map.HasAt(c)) return;
        if (_map.GetAt(c) is not IRotate rotate) return;
        rotate.Rotate();
        _undo.EnqueueAction(new() {
            ActionPosition = c,
            Type = ActionType.Rotate
        });
        _evaluatedSince = false;
    }

    private void SpawnGenericAtPosition(Coordinate coord) {
        RoadNode rn = Instantiate(_placeholder, _grid.PositionFromCoordinate(coord), Quaternion.identity);
        rn.transform.localScale = _grid.GetSize() * Vector3.one;
        _map.PutAt( rn, coord);
    }

    private void EvaluateAllRoads() {
        foreach (MapObject obj in _map.AllObjects()) {
            if (obj == null) continue;
            if (!obj.TryGetComponent(out RoadNode node)) continue;
            bool wasLocked = node.Locked;
            bool isStarter = node.StarterRoad;
            if (isStarter) continue;
            Coordinate coord = node.GetCoordinate();
            RoadNode prefab = _roadRules.GetRoadRuleTilePrefab(coord, _map);
            if (_map.HasAt(coord)) {
                _map.EraseAt(coord, true);
            }
            RoadNode rn = Instantiate(prefab, _grid.PositionFromCoordinate(coord), Quaternion.identity);
            rn.transform.localScale = _grid.GetSize() * Vector3.one;
            rn.Locked = wasLocked;
            rn.StarterRoad = isStarter;
            _map.PutAt( rn, coord, true);
        }
        _evaluatedSince = true;
    }

    private void DrawGrid() {
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        
        _gridMaterial.SetPass(0);
        
        GL.Begin(GL.LINES);
        float size = _grid.GetSize();
        float halfSize = size / 2f;
        
        float startX = Mathf.Floor(_levelEditorBounds.min.x / size) * size + halfSize;
        float startY = Mathf.Floor(_levelEditorBounds.min.y / size) * size + halfSize;
        
        for (float x = startX; x <= _levelEditorBounds.max.x; x += size) {
            GL.Vertex(new(x, _levelEditorBounds.min.y));
            GL.Vertex(new(x, _levelEditorBounds.max.y));
        }
        
        for (float y = startY; y <= _levelEditorBounds.max.y; y += size) {
            GL.Vertex(new(_levelEditorBounds.min.x, y));
            GL.Vertex(new(_levelEditorBounds.max.x, y));
        }
        
        GL.End();
        GL.PopMatrix();
    }
}
