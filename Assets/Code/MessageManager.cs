using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MessageManager : MonoBehaviour {
    public static MessageManager Instance { get; private set; }
    [SerializeField] private UIDocument uiDocument;

    private VisualElement _widgetRoot;
    private Label _title;
    private Label _body;
    private Button _exit;

    private void Awake() {
        if (Instance && Instance != this) Destroy(this);
        else Instance = this;

        var root = uiDocument.rootVisualElement;

        _widgetRoot = root.Q<VisualElement>("Message_Root");
        _title = root.Q<Label>("Message_Title");
        _body = root.Q<Label>("Message_Text");
        _exit = root.Q<Button>("Message_Close");

        _exit.RegisterCallback<ClickEvent>(ctx => Disable());
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    public void ShowMessage(string title, string body) {
        _widgetRoot.style.display = DisplayStyle.Flex;
        _title.text = title;
        _body.text = body;
        _widgetRoot.pickingMode = PickingMode.Position;
    }
    public void Disable() {
        _widgetRoot.style.display = DisplayStyle.None;
        _widgetRoot.pickingMode = PickingMode.Position;
    }
    public bool IsEnabled() => _widgetRoot.style.display == DisplayStyle.Flex;
}
