using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public string TownName;
    public Sprite MayorIcon;

    public float MusicVolume = 0.5f; //0-1
    public float SFXVolume = 0.5f; //0-1

    private void Awake() {
        if (Instance && Instance != this) {
            Destroy(this);
        } else {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }
}