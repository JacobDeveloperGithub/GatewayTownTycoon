using UnityEngine;
using UnityEngine.UIElements;

public class Title : MonoBehaviour {
    private UIDocument _document;

    [SerializeField] private SettingsUI _settings;
    [SerializeField] private InfoScene _info;

    private VisualElement _root;

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        
        var btn = _root.Q<Button>("StartButton");
        var settings = _root.Q<Button>("SettingsButton");
        btn.RegisterCallback<ClickEvent>(ctx => {
            _info.gameObject.SetActive(true);
            if (_settings.IsEnabled()) _settings.Hide();
            _root.style.display = DisplayStyle.None;
        });
        settings.RegisterCallback<ClickEvent>(_ => _settings.Show());
    }
}