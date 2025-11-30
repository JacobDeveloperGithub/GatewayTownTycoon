public class Decor : MapObject, IPopularityEffect, IErase {
    public int GetPopularityChange() => 2;

    public void OnErase() {
        SFXManager.Instance.PlayErase();
    }
}