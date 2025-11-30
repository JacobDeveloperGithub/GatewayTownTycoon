public interface IUpkeepCost {
    public int GetUpkeepCost();
}

public interface IPopularityEffect {
    public int GetPopularityChange();
}

public interface IBuildCost {
    public int GetBuildCost();    
}

public interface IRefundCost {
    public int GetRefundAmount();    
}

public interface IErase {
    public void OnErase();
}

public interface IPlaceable {
    public void OnPlace();
}

public interface IRotate {
    public void Rotate();
}

public interface IGenerateRevenue {
    public int GetRevenueAmount();
}