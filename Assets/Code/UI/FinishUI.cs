using System;

using UnityEngine;
using UnityEngine.UIElements;

public class FinishUI : MonoBehaviour, IScheduled {
    private float _tweenSpeed = 0.5f;
    private UIDocument _document;
    private float _targetBottom;
    private Button _selected = null;

    private VisualElement _scroller;
    private EventCallback<GeometryChangedEvent> _scrollBarOneOff;

    private Action _onClick;

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;
        
        _scroller = root.Q<VisualElement>("FinishBuildRoot");
        _scrollBarOneOff = evt => { _targetBottom = -_scroller.resolvedStyle.height; _scroller.style.bottom = _targetBottom; _scroller.UnregisterCallback(_scrollBarOneOff); };
        _scroller.RegisterCallback(_scrollBarOneOff);

        _selected = _scroller.Q<Button>();
        _selected.RegisterCallback<ClickEvent>(evt => _onClick?.Invoke());
    }

    public void InitStep() => LeanTween.value(_targetBottom, 0, _tweenSpeed).setOnUpdate((val) => _scroller.style.bottom = val);
    
    public void RunStep() { }
    
    public void CleanupStep() => LeanTween.value(0, _targetBottom, _tweenSpeed).setOnUpdate((val) => _scroller.style.bottom = val);

    public void AssignAction(Action a) => _onClick += a;
}