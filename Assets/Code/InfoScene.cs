using UnityEngine;
using UnityEngine.UIElements;

public class InfoScene : MonoBehaviour {
    private UIDocument _document;

    [SerializeField] private ScenarioSelect _select;
    [SerializeField] private Sprite[] _inOrder;

    private VisualElement _root;

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        
        var myrs = _root.Q<VisualElement>("Mayors");

        for (int i = 0; i < _inOrder.Length; i++) {
            int val = i;
            myrs.Q<Button>($"Mayor{i}").RegisterCallback<ClickEvent>(ctx => {
                GameManager.Instance.MayorIcon = _inOrder[val];
            }); 
        }
        
        _root.Q<Button>("Done").RegisterCallback<ClickEvent>(ctx => {
            _select.gameObject.SetActive(true);
            _root.style.display = DisplayStyle.None;
            GameManager.Instance.TownName = _root.Q<TextField>("TextField").text == "Town Name Here" ? "Townsville" :  _root.Q<TextField>("TextField").text;
        });
    }
}