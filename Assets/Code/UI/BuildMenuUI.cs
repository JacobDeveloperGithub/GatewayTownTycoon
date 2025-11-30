using UnityEngine;
using UnityEngine.UIElements;

public class BuildMenuUI : MonoBehaviour, IScheduled {
    [SerializeField] private float _tweenSpeed;

    [SerializeField] private VisualTreeAsset _scrollViewElementPrefab;

    private UIDocument _document;
    private float _targetRight;
    private float _currentTarget;
    private Button _selected = null;

    private ScrollView _scroller;
    private EventCallback<GeometryChangedEvent> _scrollBarOneOff;

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;
        
        _scroller = root.Q<ScrollView>("BuildMenu");
        _scrollBarOneOff = evt => { _targetRight = -_scroller.resolvedStyle.width; _scroller.style.right = _targetRight; _scroller.UnregisterCallback(_scrollBarOneOff); };
        _scroller.RegisterCallback(_scrollBarOneOff);
    }

    public void InitStep() => LeanTween.value(_targetRight, 0, _tweenSpeed).setOnUpdate((val) => _scroller.style.right = val);
    
    public void RunStep() {
        if (_scroller.style.right != _currentTarget) {
            _scroller.style.right = Mathf.MoveTowards(_scroller.style.right.value.value, _currentTarget, _tweenSpeed);
        }
    }
    
    public void CleanupStep() {
        LeanTween.value(0, _targetRight, _tweenSpeed).setOnUpdate((val) => _scroller.style.right = val);
        _selected?.RemoveFromClassList("checked");
    }

    public void AddButtonToScrollViewMenu(Sprite icon, System.Action OnButtonClicked) {
        var buttonInstance = _scrollViewElementPrefab.Instantiate();
        var button = buttonInstance.Q<Button>();
        button.style.backgroundImage = new StyleBackground(icon);
        button.RegisterCallback<ClickEvent>(evt => {
            OnButtonClicked?.Invoke();
            if (_selected == button) {
                button.RemoveFromClassList("checked");
            } else {
                _selected?.RemoveFromClassList("checked");
                button.AddToClassList("checked");
                _selected = button;
            }
        });
        _scroller.Add(buttonInstance);
    }
}