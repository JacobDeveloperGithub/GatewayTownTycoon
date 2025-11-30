using System;

public class TownStatisticsService {
    private int _money;
    private int _townRating; // 0-100

    public TownStatisticsService(int money, int visitors, int rating) {
        _money = money;
        _townRating = rating;
    }

    public void SpendMoney(int amount) {
        _money -= amount;
        OnMoneyChangedTo?.Invoke(_money);
        OnMoneySpent?.Invoke(amount);
    }
    
    public void AddMoney(int amount) {
        _money += amount;
        OnMoneyChangedTo?.Invoke(_money);
        OnMoneyMade?.Invoke(amount);
    }

    public bool HasEnoughMoney(int cost) => _money >= cost;
    public int GetMoney() => _money;

    public void SetTownRating(int amount) {
        _townRating = amount;
        OnTownRatingChangedTo?.Invoke(_townRating);
    }

    public int GetTownRating() => _townRating;

    public Action<int> OnMoneyChangedTo;
    public Action<int> OnVisitorsChangedTo;
    public Action<int> OnTownRatingChangedTo;
    
    public Action<int> OnMoneyMade;
    public Action<int> OnMoneySpent;
}
