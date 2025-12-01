using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundTrackManager : MonoBehaviour {
    public static SoundTrackManager Instance { get; private set; }

    [Header("General")]
    private float _maxVolume = 0.5f;
    private float _globalVolume = 0.5f;
    private float _fadeDuration = 3; // leave at 10 for now

    [SerializeField] private AudioClip _buildModeIntro;
    [SerializeField] private AudioClip _buildModeLooping;

    [SerializeField] private AudioClip _playModeIntro;
    [SerializeField] private AudioClip _playModeLooping;

    private AudioSource _introSource;
    private AudioSource _loopSource;

    private enum MusicMode { None, Build, Play }

    private MusicMode _currentMode = MusicMode.None;
    private int _volumeTweenId = -1;

    private void Awake() {
        if (Instance && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        _introSource = GetComponent<AudioSource>();
        _introSource.playOnAwake = false;
        _introSource.loop = false;
        _introSource.volume = 0f;

        _loopSource = gameObject.AddComponent<AudioSource>();
        _loopSource.playOnAwake = false;
        _loopSource.loop = true;
        _loopSource.volume = 0f;
    }

    private void Start() {
        _currentMode = MusicMode.None;
        if (GameManager.Instance) _globalVolume = GameManager.Instance.MusicVolume;
        StartBuildMode();
    }

    private void Update() {
        if (GameManager.Instance && !Mathf.Approximately(_globalVolume, GameManager.Instance.MusicVolume)) {
            _globalVolume = GameManager.Instance.MusicVolume;
            var target = _maxVolume * _globalVolume;
            FadeTo(target, 0.25f); // quick adjust when settings change
        }
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }
    
    public void StartBuildMode() {
        if (_currentMode == MusicMode.Build) return;

        _currentMode = MusicMode.Build;
        FadeTo(0, _fadeDuration, () => ScheduleMusic(_buildModeIntro, _buildModeLooping));
    }

    public void StartPlayMode() {
        if (_currentMode == MusicMode.Play) return;

        _currentMode = MusicMode.Play;
        FadeTo(0, _fadeDuration, () => ScheduleMusic(_playModeIntro, _playModeLooping));
    }

    private void ScheduleMusic(AudioClip intro, AudioClip loop) {
        _introSource.Stop();
        _loopSource.Stop();

        _introSource.clip = intro;
        _loopSource.clip = loop;

        double dspStart = AudioSettings.dspTime + 0.05;          // slight safety offset
        double loopStart = dspStart + intro.length;

        _introSource.PlayScheduled(dspStart);
        _loopSource.PlayScheduled(loopStart);

        FadeTo(_maxVolume * _globalVolume, _fadeDuration);
    }

    private void FadeTo(float targetVolume, float duration, System.Action onComplete = null) {
        if (_volumeTweenId != -1) {
            LeanTween.cancel(_volumeTweenId);
            _volumeTweenId = -1;
        }

        float start = _introSource.volume;
        _volumeTweenId = LeanTween
            .value(gameObject, start, targetVolume, duration)
            .setOnUpdate(val => {
                _introSource.volume = val;
                _loopSource.volume = val;
            })
            .setEaseInOutSine()
            .setOnComplete(() => {
                _volumeTweenId = -1;
                onComplete?.Invoke();
            })
            .id;
    }
}
