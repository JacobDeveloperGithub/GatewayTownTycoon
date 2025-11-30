using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class EscMenuUI : MonoBehaviour {
    private string _mainMenu = "Title";
    private VisualElement _root;

    [SerializeField] private SettingsUI _settings;

    private void OnEnable() {
        var document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;
        _root = root.Q<VisualElement>("ESC_Root");
        var btn = root.Q<Button>("Esc_btn");
        var btn2 = root.Q<Button>("Settings_btn");
        var btn3 = root.Q<Button>("Exit_btn");
        btn.RegisterCallback<ClickEvent>(_ => SceneManager.LoadScene(_mainMenu));
        btn2.RegisterCallback<ClickEvent>(_ => _settings.Show());
        btn3.RegisterCallback<ClickEvent>(_ => Hide());
    }

    public void Show() {
        _root.style.display = DisplayStyle.Flex;
        _root.pickingMode = PickingMode.Position;
    }
    public void Hide() {
        print("hide");
        if (_settings.IsEnabled()) _settings.Hide();
        else {
            _root.style.display = DisplayStyle.None;
            _root.pickingMode = PickingMode.Ignore;
        }
    }
    public bool IsEnabled() => _root.style.display == DisplayStyle.Flex;
}
