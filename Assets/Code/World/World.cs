using UnityEngine;
using UnityEngine.InputSystem;

using FSA;
using System.Linq;

public class World : MonoBehaviour {
    private enum States {Idle, BuildMode, PlayMode, TransitionToBuild, EventChance}
    private StateMachine _machine;
    
    [Header("World Starting Values")]
    [SerializeField] private Bounds _worldBounds;
    [SerializeField] private float _gridSize;
    [SerializeField] private int _startingCash;
    [SerializeField] private int _dailyVisitors;
    [SerializeField] private int _defaultRating = 10;

    [Header("BuildMode Steps")]
    [SerializeField] private BuildMode _buildMode;
    
    [Header("PlayMode Steps")]
    [SerializeField] private PlayMode _playMode;

    [Header("UI")]
    [SerializeField] private BusinessMenuUI _businessMenu;
    [SerializeField] private TownUI _townUI;
    [SerializeField] private BuildMenuUI _buildMenuUI;
    [SerializeField] private FinishUI _finishUI;
    [SerializeField] private WorldUI _worldUI;
    [SerializeField] private EscMenuUI _escUI;

    [Header("Services")]
    [SerializeField] private Map _map;

    [Header("Misc")]
    [SerializeField] private Car _carPrefab;
    [SerializeField] private Decor _meteorDecor;

    private CarFactory _carFactory;
    private RoadGraph _graph;
    private TownStatisticsService _townSvc;
    private GridService _grid;

    private IScheduled[] _buildModeSteps;
    private IScheduled[] _playModeSteps;

    private bool _buildFinished = false;

    private int _weeklyMessageCount = 0;
    private string[] _messages = new string[5]{
        "Your first week is done, congratulations!\n\nUse this money to build more businesses and roads to support those businesses!",
        "Your second week is done!\n\nYou're really getting the hang of this!\n\nNote: You can hold space to speed up the weeks between build days.",
        "Another week gone by, another paycheck.\n\nMore expensive businesses yield more profit. Don't be afraid to sell old\nbusinesses to make room for new ones.\n\nBeware though, anything sold will not refund its entire cost to you!",
        "You're well on your way to wealth!\n\nSome businesses increase your popularity with travelers more than others.\nSome even hurt your popularity!\n\nI hear people love to gamble, and hate to be berated by advertisements.",
        "Your town is (probably) more popular than ever!\n\nKeep it up, and thanks for playing."
    };

    private bool _won = false;
    private bool _negative = false;

    private void Awake() {
        Application.targetFrameRate = -1;
        _grid = new(_gridSize);
        _map = new(_grid, _worldBounds);
        _graph = new(_grid, _map);
        _townSvc = new(_startingCash, _dailyVisitors, _defaultRating);
        _carFactory = new(_grid, _graph, _townSvc, _carPrefab);

        _buildModeSteps = new IScheduled[] {_buildMode, _buildMenuUI, _finishUI};
        _playModeSteps = new IScheduled[]  {_playMode, _townUI};

        StoreExistingOnLoad();

        _machine = new StateMachineBuilder()
            .WithState(States.Idle)
            .WithTransition(States.BuildMode, () => !MessageManager.Instance.IsEnabled())

            .WithState(States.BuildMode)
            .WithOnEnter(() => _townUI.Show())
            .WithOnEnter(() => InitializeSchedule(_buildModeSteps))
            .WithOnRun(() => RunSchedule(_buildModeSteps))
            .WithOnExit(() => CleanupSchedule(_buildModeSteps))
            .WithOnExit(() => SoundTrackManager.Instance.StartPlayMode())
            .WithTransition(States.PlayMode, () => _buildFinished && MapValid())

            .WithState(States.PlayMode)
            .WithOnEnter(() => _buildFinished = false)
            .WithOnEnter(() => InitializeSchedule(_playModeSteps))
            .WithOnRun(() => RunSchedule(_playModeSteps))
            .WithOnExit(() => CleanupSchedule(_playModeSteps))
            .WithOnExit(() => SoundTrackManager.Instance.StartBuildMode())
            .WithTransition(States.TransitionToBuild, () => _playMode.PlayModeCompleted())

            .WithState(States.TransitionToBuild)
            .WithOnEnter(PlayWeeklyMessage)
            .WithTransition(States.EventChance, () => !MessageManager.Instance.IsEnabled())
            
            .WithState(States.EventChance)
            .WithOnEnter(RollRandomEvent)
            .WithTransition(States.BuildMode, () => !MessageManager.Instance.IsEnabled())
            
            .Build();
    }

    private void Start() {
        _townUI.GetDependencies(_townSvc);
        _playMode.GetDependencies(_carFactory, _graph, _townSvc, _map, _townUI);
        _buildMode.GetDependencies(_grid, _map, _townSvc, _worldBounds);
        _machine.StartStateMachine();

        _townSvc.SpendMoney(0);
        _townSvc.SetTownRating(_defaultRating);
        
        _buildMode.CleanupStep();
        _playMode.CleanupStep();

        _worldUI.SetUISounds();
        _townUI.InitStep();

        _finishUI.AssignAction(() => _buildFinished = true);
        
        MessageManager.Instance.ShowMessage(
            "Welcome to Gateway Town Tycoon",
                 "It's build day! Every build day you are allowed to draw\n" 
            +    "and erase roads and buildings to make your\n"
            +    "small passerby town your own.\n\n"
                
            +    "Build days only come onces a week. Each day between\n"
            +    "build days WAVES of passerthrough fly through your town\n"
            +    "some just passing, some in need of food, snacks, and/or gas.\n\n"
                
            +    "Some useful tips to understand how to best manage this small town\n"
            +    "All tools available to you are on the right of the screen!\n"
            +    "Left click to do your current action. Right click to cancel!\n"
            +    "All roads must be connected before a day can start.\n"
            +    "If you make a mistake building, press [U] and you can undo it!\n"
            +    "When you're ready to end build day, click done on the bottom right!\n\n"
                
            +    "In this scenario:\n"
            +    "Try to have 1 million dollars at once before the end of June!\n"
            +    "Good luck :)\n"
            
        );
    }

    private void Update() {
        _machine.RunStateMachine(Time.deltaTime);

        if (ControlsService.Instance.EscPressed()) {
            if (_machine.IsInState(States.BuildMode)) _buildMode.ClearMode();
            if (_escUI.IsEnabled()) _escUI.Hide();
            else _escUI.Show();
        }
    }

    private void InitializeSchedule(IScheduled[] steps) {
        foreach (IScheduled step in steps) step.InitStep();
    }
    
    private void RunSchedule(IScheduled[] steps) {
        foreach (IScheduled step in steps) step.RunStep();
    }
    
    private void CleanupSchedule(IScheduled[] steps) {
        foreach (IScheduled step in steps) step.CleanupStep();
    }

    private bool MapValid() {
        _graph.RedrawGraph();
        if (_graph.IsMapValid()) {
            return true;
        } else {
            _buildFinished = false;
            NotificationManager.Instance.EnqueueNotification("Cannot proceed; Not all starting roads are connected to the graph.");
            return false;
        }
    }

    private void StoreExistingOnLoad() {
        foreach (MapObject obj in FindObjectsByType<MapObject>(FindObjectsSortMode.None)) {
            Coordinate c = _grid.GetCoordinate(obj.transform.position);
            if (_map.HasAt(c)) _map.EraseAt(c, true);
            obj.transform.position = _grid.PositionFromCoordinate(c);
            obj.transform.localScale = _grid.GetSize() * Vector3.one;
            if (obj is Business b) {
                b.GetBusinessDependencies(_townSvc);
            }
            _map.PutAt(obj, c, true);
        }
    }

    private void RollRandomEvent() {
        if (_playMode.GetDate() <= new System.DateTime(2025, 1, 20)) return;
        int roll = Random.Range(0,2);
        if (roll <= 0) {
            int evt = Random.Range(0,6);
            switch (evt) {
                case 0:
                    TaxBreak();
                    break;
                case 1:
                    MeteorEvent();
                    break;
                case 2:
                    TaxHike();
                    break;
                case 3:
                    MassiveCasinoWin();
                    break;
                case 4:
                    ReveredAdSpace();
                    break;
                case 5:
                    Tornado();
                    break;
                default:
                    break;
            }
        }
    }

    private void Tornado() {
        MessageManager.Instance.ShowMessage("Tornado Touchdown!", $"A tornado has struck your town!\n\nAll objects and roads have been re-arranged..! Sorry!");
        _map.Shuffle();
    }

    private void ReveredAdSpace() {
        MessageManager.Instance.ShowMessage("Advertiser Frenzy!", $"Your little town has become a hotspot.\nExisting billboard owners are willing to may 5 times as much\njust to retain the adspace they have.\n\nExisting billboards pay 5 times as much!");
        foreach (MapObject mo in _map.AllObjects()) {
            if (mo is BillboardBusiness b) {
                b.ShopContext.RevenuePerCustomer *= 5;
            }
        }
    }

    private void MassiveCasinoWin() {
        bool hasCasino = false;
        foreach (MapObject mo in _map.AllObjects()) {
            if (mo is CasinoBusiness c) hasCasino = true;
        }
        if (hasCasino) {
            int has = _townSvc.GetMoney();
            if (has < 100_000) return;
            int cost = 100_000 * Mathf.FloorToInt(has/100_000);
            _townSvc.SpendMoney(cost);
            MessageManager.Instance.ShowMessage("Gamblers' Dream!", $"Huge win for random passerby at Casino in {GameManager.Instance.TownName}!\n\nRandom traveler won {cost} dollars! Local mayor mentally in shambles!");

        }
    }

    private void TaxHike() {
        MessageManager.Instance.ShowMessage("Grubby Government!", "Evil money grubbing governor says you're not paying your\nfair share.\n\nAll upkeep costs for businesses and roads are up 50%.");

        foreach (MapObject mo in _map.AllObjects()) {
            if (mo is Business b) b.ShopContext.WeeklyUpkeepCost = (int)(1.5f * b.ShopContext.WeeklyUpkeepCost);
            if (mo is RoadNode node) node.SetUpkeepCost((int)(node.GetUpkeepCost() * 1.5f));
        }
    }
    
    private void TaxBreak() {
        MessageManager.Instance.ShowMessage("Generous Government!", "The governor declares all your existing roads are paid off\n\nAll road upkeep costs are now 0 for existing roads.");

        foreach (MapObject mo in _map.AllObjects()) {
            if (mo is RoadNode node) node.SetUpkeepCost(0);
        }
    }

    private void MeteorEvent() {
        MessageManager.Instance.ShowMessage("Meteor Strike!", "Holy f****** s*** a meteor struck!\nGood thing you have natural disaster insurance. All destroyed buildings and roads were fully\nrefunded.");

        Vector2 pos = new(Random.Range(_worldBounds.min.x, _worldBounds.max.x), Random.Range(_worldBounds.min.y, _worldBounds.max.y));
        Coordinate at = _grid.GetCoordinate(pos);

        int size = 2;

        DestroyAtCoordinate(at);
        for (int i = 1; i <= size; i++) {
            Coordinate[] offsets;
            if (i == size) {
                offsets = new[] {
                    new Coordinate(i, 0),
                    new Coordinate(-i, 0),
                    new Coordinate(0, i),
                    new Coordinate(0, -i)
                };
            } else {
                offsets = new[] {
                    new Coordinate(i, 0),
                    new Coordinate(-i, 0),
                    new Coordinate(0, i),
                    new Coordinate(0, -i),
                    new Coordinate(i, i),
                    new Coordinate(-i, i),
                    new Coordinate(i, -i),
                    new Coordinate(-i, -i)
                };
            }

            foreach (Coordinate offset in offsets) {
                DestroyAtCoordinate(at + offset);
            }
        }
    }
    
    private bool ValidPosition(Vector2 pos) => _worldBounds.Contains(pos);

    private void DestroyAtCoordinate(Coordinate coordinate) {
        if (!ValidPosition(_grid.PositionFromCoordinate(coordinate))) return;
        if (_map.HasAt(coordinate)) {
            if (_map.GetAt(coordinate) is IRefundCost refund) {
                _townSvc.AddMoney(refund.GetRefundAmount() * 2);
            }
            _map.EraseAt(coordinate);
        }
        MapObject mo = Instantiate(_meteorDecor);
        mo.PrefabSelf = _meteorDecor;
        mo.transform.localScale = _grid.GetSize() * Vector3.one;
        mo.transform.position = _grid.PositionFromCoordinate(coordinate);
        _map.PutAt(mo, coordinate);
    }

    private void PlayWeeklyMessage() {
        if (!_negative && _townSvc.GetMoney() < 0) {
            MessageManager.Instance.ShowMessage("You (probably) lost!", "You have negative money!\n\nUpkeep costs are pretty scary. Try to avoid not having profitable\nbusinesses attached to your roads!\n\nRestarting may be your best option, or selling businesses and replacing them.\n\n(You can press esc to see the restart menu!)");
            _negative = true;
            return;
        }
        if (_weeklyMessageCount < _messages.Length) {
            MessageManager.Instance.ShowMessage("Build Day is Here!", _messages[_weeklyMessageCount++]);
            return;
        }
        if (_won) return;
        if (_townSvc.GetMoney() >= 1_000_000) {
            MessageManager.Instance.ShowMessage("You won!", "1 million dollars in a year! Shouldn't have been too bad!\n\nHope you had fun and built a cozy town");
            _won = true;
        } else if (_playMode.GetDate() >= new System.DateTime(2025,07,1)) {
            MessageManager.Instance.ShowMessage("You failed...", "1 million dollars is a lot.. I get it. You didn't make it in time!\n\nFeel free to continue building your town though! Hopefully it was fun regardless");
            _won = true;
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
