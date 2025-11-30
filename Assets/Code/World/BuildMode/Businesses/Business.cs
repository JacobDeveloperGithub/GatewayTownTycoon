using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BusinessShopContext {
    public Sprite UISprite;
    public int Cost;
    public int RevenuePerCustomer;
    public int WeeklyUpkeepCost;
    public int RatingRequirement;
    public string Title;
    public string Description;
}

[Serializable]
public struct BusinessWorldContext {
    public float TimeInBusiness;
    public int ReputationImpact;
}

[RequireComponent(typeof(ParticleSystem))]
public abstract class Business : MapObject, IPlaceable, IErase, IRefundCost, IBuildCost, IUpkeepCost, IGenerateRevenue  {
    public BusinessShopContext ShopContext;
    public BusinessWorldContext WorldContext;
    

    protected TownStatisticsService _townSvc;

    private ParticleSystem _particles;
    
    public void GetBusinessDependencies(TownStatisticsService townService) {
        _townSvc = townService;
        _particles = GetComponent<ParticleSystem>();
    }

    public virtual void OnPlace() {
        SFXManager.Instance?.PlayBuild();
    }

    public virtual void OnErase() {
        SFXManager.Instance?.PlayErase();
    }

    public int GetRefundAmount() => ShopContext.Cost / 2;
    public int GetBuildCost() => ShopContext.Cost;
    public int GetUpkeepCost() => ShopContext.WeeklyUpkeepCost;
    public virtual int GetRevenueAmount() => ShopContext.RevenuePerCustomer;

    protected void MakeMoney() {
        int profit = GetRevenueAmount();
        if (profit > 0) {
            _townSvc.AddMoney(profit);
            SFXManager.Instance?.PlayCash();
            _particles.Emit(GetRevenueAmount());
        } else {
            _townSvc.SpendMoney(profit);
        }
    }
}

public abstract class CarBusiness : Business, IConnectedToRoadGraph, IRotate, IPopularityEffect {
    public Direction EntranceFacing;

    private List<Car> _carsInBusiness;
    private float _timer;

    protected virtual void Start() {
        _timer = WorldContext.TimeInBusiness;
        _carsInBusiness = new();
    }

    private void Update() {
        if (_carsInBusiness.Count > 0) {
            _timer -= Time.deltaTime;
            if (_timer <= 0) {
                Car[] cars = new Car[_carsInBusiness.Count];
                _carsInBusiness.CopyTo(cars);
                foreach (Car c in cars)
                    OnBusinessExit(c);
                _timer = WorldContext.TimeInBusiness;
            }
        }
    }

    
    public void OnBusinessEnter(Car c) {
        _carsInBusiness.Add(c);
    } 
    
    public virtual void OnBusinessExit(Car c) {
        MakeMoney();
        _carsInBusiness.Remove(c);
    }

    public abstract void ConnectToRoadGraph(RoadGraph graph);

    public virtual void Rotate() {
        EntranceFacing = EntranceFacing switch {
            Direction.North => Direction.East,
            Direction.East => Direction.South,
            Direction.South => Direction.West,
            Direction.West => Direction.North,
            _ => Direction.North
        };
    }

    public bool IsValidConnectionSide(Direction side) => side == EntranceFacing;

    public int GetPopularityChange() => WorldContext.ReputationImpact;
}