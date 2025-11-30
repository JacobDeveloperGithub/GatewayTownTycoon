using UnityEngine;

public class CasinoBusiness : OneTileBusiness {
    public override int GetRevenueAmount() {
        return Random.Range(0, ShopContext.RevenuePerCustomer);
    }
}
