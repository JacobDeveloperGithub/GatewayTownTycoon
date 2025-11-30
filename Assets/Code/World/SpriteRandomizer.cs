using UnityEngine;

public class SpriteRandomizer : MonoBehaviour {
    [SerializeField] private Sprite[] _randomSpriteSelection;

    private void Start() {
        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = _randomSpriteSelection[Random.Range(0,_randomSpriteSelection.Length)];
    }
}
