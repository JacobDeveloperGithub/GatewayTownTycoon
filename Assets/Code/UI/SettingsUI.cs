using UnityEngine;
using UnityEngine.UIElements;

public class SettingsUI : MonoBehaviour {
    private VisualElement _root;

    private Slider _sfxSlider;
    private Slider _musicSlider;
    private Button _closeButton;
    
    private void OnEnable() {
        var document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;
        _root = root.Q<VisualElement>("SettingsRoot");

        _sfxSlider = root.Q<Slider>("sfxVolumeSlider");
        _musicSlider = root.Q<Slider>("musicVolumeSlider");
        _closeButton = root.Q<Button>("SettingsCloseButton");

        if (_closeButton != null) _closeButton.RegisterCallback<ClickEvent>(_ => Hide());

        if (_root != null) _root.style.display = DisplayStyle.None;
    }

    public bool IsEnabled() => _root.style.display == DisplayStyle.Flex;

    public void Show() {
        if (_root == null)
            return;

        if (GameManager.Instance != null) {
            if (_musicSlider != null) _musicSlider.value = GameManager.Instance.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = GameManager.Instance.SFXVolume;
        }

        _root.style.display = DisplayStyle.Flex;
        _root.pickingMode = PickingMode.Position;
    }

    public void Hide() {
        if (_root == null)
            return;

        if (GameManager.Instance != null) {
            if (_musicSlider != null)
                GameManager.Instance.MusicVolume = _musicSlider.value;

            if (_sfxSlider != null)
                GameManager.Instance.SFXVolume = _sfxSlider.value;
        }

        _root.style.display = DisplayStyle.None;
        _root.pickingMode = PickingMode.Ignore;
    }
}
