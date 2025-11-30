using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundTrackManager : MonoBehaviour {
    public static SoundTrackManager Instance { get; private set; }

    [Header("General")]
    private float _maxVolume = 0.5f;
    private float _globalVolume = 0.5f;
    [SerializeField] private float _fadeDuration = 10f; // leave at 10 for now

    [Header("Build Mode (seconds)")]
    [SerializeField] private float _buildModeFadeTo   = 10f; // intro start for build
    [SerializeField] private float _buildModeLoopFrom = 10f;
    [SerializeField] private float _buildModeLoopTo   = 10f;

    [Header("Play Mode (seconds)")]
    [SerializeField] private float _playModeFadeTo   = 10f; // intro start for play
    [SerializeField] private float _playModeLoopFrom = 10f;
    [SerializeField] private float _playModeLoopTo   = 10f;

    private enum MusicMode {
        None,
        Build,
        Play
    }

    private AudioSource _src;
    private MusicMode _currentMode = MusicMode.None;
    private float _currentLoopFrom;
    private float _currentLoopTo;
    private int _volumeTweenId = -1;

    private void Awake() {
        if (Instance && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _src = GetComponent<AudioSource>();
        _src.loop = false;
        _src.volume = 0f;
    }

    private void Start() {
        if (_src.clip == null)
            return;

        _currentMode = MusicMode.None;
        _src.Play();
        _currentLoopFrom = _buildModeLoopFrom;
        _currentLoopTo = _buildModeLoopTo;
        if (GameManager.Instance) _globalVolume = GameManager.Instance.MusicVolume;
        FadeTo(_maxVolume * _globalVolume, _fadeDuration);
    }

    private void Update() {
        if (GameManager.Instance && _globalVolume != GameManager.Instance.MusicVolume) {
            _globalVolume = GameManager.Instance.MusicVolume;
            if (_src.volume >= _maxVolume * _globalVolume) _src.volume = _maxVolume * _globalVolume;
            if (_src.volume <= _maxVolume * _globalVolume) _src.volume = _maxVolume * _globalVolume;
        }
        if (_src.clip == null || !_src.isPlaying) return;

        if (_volumeTweenId == -1 && _currentLoopTo > _currentLoopFrom && _src.time >= _currentLoopTo) {
            TransitionTo(_currentLoopFrom, _currentLoopFrom, _currentLoopTo);
        }
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    public void StartBuildMode() {
        if (_src.clip == null)
            return;

        if (_currentMode == MusicMode.Build)
            return;

        _currentMode = MusicMode.Build;

        TransitionTo(
            fadeTo:   _buildModeFadeTo,
            loopFrom: _buildModeLoopFrom,
            loopTo:   _buildModeLoopTo
        );
    }

    public void StartPlayMode() {
        if (_src.clip == null)
            return;

        if (_currentMode == MusicMode.Play)
            return;

        _currentMode = MusicMode.Play;

        TransitionTo(
            fadeTo:   _playModeFadeTo,
            loopFrom: _playModeLoopFrom,
            loopTo:   _playModeLoopTo
        );
    }

    private void TransitionTo(float fadeTo, float loopFrom, float loopTo) {
        FadeTo(0f, _fadeDuration, () => {
            SetLoopRegion(loopFrom, loopTo);

            _src.time = ClampTime(fadeTo);

            if (!_src.isPlaying)
                _src.Play();

            FadeTo(_maxVolume * _globalVolume, _fadeDuration);
        });
    }

    private void SetLoopRegion(float from, float to) {
        _currentLoopFrom = Mathf.Max(0f, from);
        _currentLoopTo   = Mathf.Max(_currentLoopFrom, to);
    }

    private float ClampTime(float t) {
        if (_src.clip == null)
            return 0f;

        return Mathf.Clamp(t, 0f, Mathf.Max(0f, _src.clip.length - 0.01f));
    }

    private void FadeTo(float targetVolume, float duration, System.Action onComplete = null) {
        if (_volumeTweenId != -1) {
            LeanTween.cancel(_volumeTweenId);
            _volumeTweenId = -1;
        }

        if (targetVolume > _src.volume) {
            _volumeTweenId = LeanTween
                .value(gameObject, _src.volume, targetVolume, duration)
                .setOnUpdate(val => _src.volume = val)
                .setEaseInSine()
                .setOnComplete(() => {
                    _volumeTweenId = -1;
                    onComplete?.Invoke();
                })
                .id;
        } else {
            _volumeTweenId = LeanTween
                .value(gameObject, _src.volume, targetVolume, duration)
                .setOnUpdate(val => _src.volume = val)
                .setEaseOutSine()
                .setOnComplete(() => {
                    _volumeTweenId = -1;
                    onComplete?.Invoke();
                })
                .id;
        }
    }

}
