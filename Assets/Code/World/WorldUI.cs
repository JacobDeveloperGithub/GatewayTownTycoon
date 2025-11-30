using UnityEngine;
using UnityEngine.UIElements;

public class WorldUI : MonoBehaviour {
    private UIDocument _ui;
    private VisualElement _root;

    private void Awake() {
        _ui = GetComponent<UIDocument>();
        _root = _ui.rootVisualElement;
    }

    private void Start() {
        SetUISounds();
    }
    
    public void SetUISounds() {
        var buttons = _root.Query<Button>().ToList();
        foreach (var btn in buttons) {
            btn.UnregisterCallback<PointerEnterEvent>(OnHover);
            btn.UnregisterCallback<ClickEvent>(OnClick);

            btn.RegisterCallback<PointerEnterEvent>(OnHover);
            btn.RegisterCallback<ClickEvent>(OnClick);
        }
    }

    void OnHover(PointerEnterEvent evt) {
        if (SFXManager.Instance) SFXManager.Instance.PlayHover();
    }

    void OnClick(ClickEvent evt) {
        if (SFXManager.Instance) SFXManager.Instance.PlayClick();
    }
}