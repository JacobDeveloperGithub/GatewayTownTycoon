using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using FSA;
using UnityEngine.InputSystem;

public class PlayMode : MonoBehaviour, IScheduled {
    private enum States {Idle, SpawningWeek, WeekSummary, Finished}
    private StateMachine _machine;

    [SerializeField] private SpriteRenderer _lightingObject;
    private DateTime _date;
    
    private TownStatisticsService _townSvc;
    private RoadGraph _roadGraph;
    private CarFactory _carSvc;
    private TownUI _townUI;
    private Map _map;

    [SerializeField] private float _targetWeekDurationSeconds = 30f;

    private float _spawnIntervalSeconds = 0.2f;
    private readonly int _simulatedDaysInWeek = 6;

    private List<Car> _pool;

    private float _spawnTimerSeconds;
    private int _carsSpawnedThisWeek;
    private int _weeklyVisitorTarget;

    private float _estimatedWeekDurationSeconds;
    private float _singleDayVisualDurationSeconds;

    private int _totalRevenueThisWeek;
    private int _totalVisitorsThisWeek;
    private int _totalUpkeepThisWeek;

    private void Awake() {
        _machine = new StateMachineBuilder()
            .WithState(States.Idle)
            .WithTransition(States.SpawningWeek, () => true)

            .WithState(States.SpawningWeek)
            .WithOnEnter(BeginWeek)
            .WithOnRun(SpawnCarsOverWeek)
            .WithTransition(States.WeekSummary, WeekSimulationComplete)

            .WithState(States.WeekSummary)
            .WithOnEnter(EndWeekAndChargeUpkeep)
            .WithOnEnter(() => MessageManager.Instance.ShowMessage("Weekly Profit", SummaryText()))
            .WithTransition(States.Finished, () => !MessageManager.Instance.IsEnabled())

            .WithState(States.Finished)

            .Build();
    }

    public void GetDependencies(CarFactory carFactory, RoadGraph graph, TownStatisticsService stats, Map map, TownUI townUI) {
        _carSvc = carFactory;
        _roadGraph = graph;
        _townSvc = stats;
        _townUI = townUI;
        _map = map;
        _pool = new();
        _date = new DateTime(2025, 1, 1);
        if (townUI)
            _townUI.SetDate(_date);
    }

    public void InitStep() {
        _machine.SetState(States.Idle);

        ComputeTownRating();
        PlanWeeklyVisitorsAndSpawns();


        _spawnTimerSeconds = 0f;
        _carsSpawnedThisWeek = 0;

        _totalRevenueThisWeek = 0;
        _totalVisitorsThisWeek = _weeklyVisitorTarget;
        _totalUpkeepThisWeek = 0;

        if (_townSvc != null) _townSvc.OnMoneyMade += AddMoney;
    }

    public void RunStep() {
        _machine.RunStateMachine(Time.deltaTime);
        if (ControlsService.Instance.IsSpacePressed()) Time.timeScale = 5;
        else Time.timeScale = 1;
    }

    public void CleanupStep() {
        Time.timeScale = 1;
        LeanTween.cancel(gameObject);
        LeanTween.cancel(_lightingObject.gameObject);
        SetLightingAlpha(0);
    }

    public bool PlayModeCompleted() => _machine.IsInState(States.Finished);
    public DateTime GetDate() => _date;

    private void ComputeTownRating() {
        int popularity = 0;
        
        foreach (MapObject m in _map.AllObjects()) {
            if (m is IPopularityEffect effect) {
                popularity += effect.GetPopularityChange();        
            }
        }

        if (_townSvc != null) _townSvc.SetTownRating(popularity);
    }

    private void PlanWeeklyVisitorsAndSpawns() {
        _weeklyVisitorTarget = 0;


        if (_townSvc == null) {
            _weeklyVisitorTarget = 9999;
            _spawnIntervalSeconds = UnityEngine.Random.Range(0.25f, 1.0f);
            _estimatedWeekDurationSeconds = 9999f;
        } else {
            for (int day = 0; day < _simulatedDaysInWeek; day++) {
                int visitorsForDay = Mathf.Max(10, 200 * _townSvc.GetTownRating() / 100);
                visitorsForDay += UnityEngine.Random.Range(-visitorsForDay / 2, visitorsForDay / 2);
                _weeklyVisitorTarget += visitorsForDay;
            }
            _spawnIntervalSeconds = Mathf.Max(_targetWeekDurationSeconds / _weeklyVisitorTarget, 0.005f);
            _estimatedWeekDurationSeconds = _targetWeekDurationSeconds;
        }
    }

    private Car GetFromPool() => _pool.Find(c => !c.ActiveInSimulation());

    private void BeginWeek() {
        _spawnTimerSeconds = 0f;
        _carsSpawnedThisWeek = 0;

        StartWeekDayNightCycles();
    }

    private void StartWeekDayNightCycles() {
        if (_simulatedDaysInWeek <= 0 || _estimatedWeekDurationSeconds <= 0f)
            return;

        _singleDayVisualDurationSeconds = _estimatedWeekDurationSeconds / _simulatedDaysInWeek;
        float halfDayTweenDuration = _singleDayVisualDurationSeconds / 2f;

        Color current = _lightingObject.color;
        _lightingObject.color = new Color(current.r, current.g, current.b, 0f);

        for (int dayIndex = 0; dayIndex < _simulatedDaysInWeek; dayIndex++) {
            float delay = _singleDayVisualDurationSeconds * dayIndex;
            LeanTween.delayedCall(gameObject, delay, () => PlaySingleDayNightCycle(halfDayTweenDuration));
        }
    }

    private void PlaySingleDayNightCycle(float halfDuration) {
        LeanTween.value(gameObject, 0f, 0.98f, halfDuration)
            .setEaseInCubic()
            .setOnUpdate(alpha => SetLightingAlpha(alpha))
            .setOnComplete(() => {
                AdvanceDateOneDay();
                LeanTween.value(gameObject, 0.98f, 0f, halfDuration)
                    .setEaseInCubic()
                    .setOnUpdate(alpha => SetLightingAlpha(alpha));
            });
    }

    private void SetLightingAlpha(float alpha) {
        Color c = _lightingObject.color;
        _lightingObject.color = new Color(c.r, c.g, c.b, alpha);
    }

    private void AdvanceDateOneDay() {
        _date = _date.AddDays(1);
        _townUI.SetDate(_date);
    }

    private void SpawnCarsOverWeek() {
        if (_carsSpawnedThisWeek >= _weeklyVisitorTarget) return;
        _spawnTimerSeconds += Time.deltaTime;
        if (_spawnTimerSeconds < _spawnIntervalSeconds) return;
        do {
            SpawnCarForWeek();
            _spawnTimerSeconds -= _spawnIntervalSeconds;
        } while (_spawnTimerSeconds >= _spawnIntervalSeconds);
    }

    private void SpawnCarForWeek() {
        int nodeCount = _roadGraph.GetGraph().NodeCount();
        if (nodeCount < 2) return;

        RoadGameGraphNode start = _roadGraph.GetRandomEntrance();
        Car car;
        car = GetFromPool();
        if (car == null) {
            car = _carSvc.CreateCar(start);
            _pool.Add(car);
        } else {
            _carSvc.ResetCar(car, start);
        }
        _carsSpawnedThisWeek++;
    }

    private bool WeekSimulationComplete() {
        if (_carsSpawnedThisWeek < _weeklyVisitorTarget) return false;
        return _pool.All(car => !car.ActiveInSimulation());
    }

    private void EndWeekAndChargeUpkeep() {
        foreach (MapObject obj in _map.AllObjects()) {
            if (obj is IUpkeepCost upkeep) {
                int cost = upkeep.GetUpkeepCost();
                if (_townSvc != null) _townSvc.SpendMoney(cost);
                _totalUpkeepThisWeek += cost;
            }
        }

        _townSvc.OnMoneyMade -= AddMoney;
    }

    private void AddMoney(int amount) {
        _totalRevenueThisWeek += amount;
    }

    private string SummaryText() =>
        $"Total Visitors this week: {_totalVisitorsThisWeek}\n" +
        $"Total Revenue made this week: {_totalRevenueThisWeek}\n" +
        $"Total Upkeep cost this week: {_totalUpkeepThisWeek}\n" +
        $"Profit: {_totalRevenueThisWeek - _totalUpkeepThisWeek}";
}
         
