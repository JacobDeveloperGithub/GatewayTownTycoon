using System;
using UnityEngine;

[Serializable]
public struct LimitedSFX {
    public AudioClip Clip;
    public int Limit;
}

public class SFXManager : MonoBehaviour {
    public static SFXManager Instance { get; private set; }

    [SerializeField] private LimitedSFX _buildSfx;
    [SerializeField] private LimitedSFX _eraseSfx;
    [SerializeField] private LimitedSFX _cashSfx;
    [SerializeField] private AudioClip _click;
    [SerializeField] private AudioClip _hover;

    private AudioSource[] _builds;
    private AudioSource[] _erases;
    private AudioSource[] _cashes;

    private float _volume = 0.5f;
    private float _globalVolume = 0.5f;

    private void Awake() {
        if (Instance && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start() {
        _builds = new AudioSource[_buildSfx.Limit];
        _erases = new AudioSource[_eraseSfx.Limit];
        _cashes = new AudioSource[_cashSfx.Limit];

        if (GameManager.Instance) _globalVolume = GameManager.Instance.SFXVolume;

        for (int i = 0; i < _buildSfx.Limit; i++) {
            _builds[i] = gameObject.AddComponent<AudioSource>();
            _builds[i].clip = _buildSfx.Clip;
            _builds[i].volume = _volume * _globalVolume;
        }
        
        for (int i = 0; i < _eraseSfx.Limit; i++) {
            _erases[i] = gameObject.AddComponent<AudioSource>();
            _erases[i].clip = _eraseSfx.Clip;
            _erases[i].volume = _volume * _globalVolume;

        }
        
        for (int i = 0; i < _cashSfx.Limit; i++) {
            _cashes[i] = gameObject.AddComponent<AudioSource>();
            _cashes[i].clip = _cashSfx.Clip;
            _cashes[i].volume = _volume * _globalVolume;
        }
    }

    private void Update() {
        if (!GameManager.Instance || _globalVolume == GameManager.Instance.SFXVolume) return;
        _globalVolume = GameManager.Instance.SFXVolume;
        foreach (AudioSource src in _builds) {
            src.volume = _volume * _globalVolume;
        }
        foreach (AudioSource src in _erases) {
            src.volume = _volume * _globalVolume;
        }
        foreach (AudioSource src in _cashes) {
            src.volume = _volume * _globalVolume;
        }
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    public void PlayBuild() {
        foreach (AudioSource src in _builds) {
            if (src.isPlaying) continue;
            src.Play();
            break;
        }
    }
    
    public void PlayErase() {
        foreach (AudioSource src in _erases) {
            if (src.isPlaying) continue;
            src.Play();
            break;
        }
    }
    
    public void PlayCash() {
        foreach (AudioSource src in _cashes) {
            if (src.isPlaying) continue;
            src.Play();
            break;
        }
    }

    public void PlayHover() {
        AudioSource.PlayClipAtPoint(_hover, Vector3.zero);        
    }

    public void PlayClick() {
        AudioSource.PlayClipAtPoint(_click, Vector3.zero);        
    }
}
