using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TownUI : MonoBehaviour, IScheduled {
    [SerializeField] private Sprite _mayor;
    [SerializeField] private Sprite _money;
    [SerializeField] private Sprite _people;

    [SerializeField] private float _tweenSpeed;
    [SerializeField] private float _timeUntilHide;

    [SerializeField] private Sprite[] _gradeSprites; // S,A,B,C,D,F

    private UIDocument _document;
    private float _timer = 0;
    private float _currentTarget;

    private Label _moneyLabel;
    private Label _townLabel;
    private Label _dateLabel;
    private VisualElement _mayorSprite;
    private VisualElement _infoBar;
    private VisualElement _gradeSprite;
    private VisualElement _root;

    private TownStatisticsService _townSvc;

    public void GetDependencies(TownStatisticsService townSvc) {
        _townSvc = townSvc;
    }

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        _root = _document.rootVisualElement;
        
        _infoBar = _root.Q<VisualElement>("InfoBar");
        _mayorSprite = _root.Q<VisualElement>("MayorSprite");
        _gradeSprite = _root.Q<VisualElement>("RatingSprite");
        var moneyIcon = _root.Q<VisualElement>("MoneyIcon");
        
        _mayorSprite.style.backgroundImage = GameManager.Instance ? new StyleBackground(GameManager.Instance.MayorIcon) : new StyleBackground(_mayor);
        _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[5]);
        moneyIcon.style.backgroundImage = new StyleBackground(_money);
        
        _moneyLabel = _root.Q<Label>("MoneyLabel");
        _townLabel = _root.Q<Label>("TownLabel");
        _townLabel.text = GameManager.Instance ? GameManager.Instance.TownName : "Townsville";
        _dateLabel = _root.Q<Label>("DateLabel");
        
        _infoBar.style.top = 0f;

        _currentTarget = 0;
    }

    public void Show() {
        _currentTarget = 0;
        _infoBar.style.top = 0;
    }

    public void InitStep() {
        _townSvc.OnMoneyChangedTo += (x) => SetMoneyCount(x);
        _townSvc.OnTownRatingChangedTo += (x) => SetRatingIcon(x);
        SetMoneyCount(_townSvc.GetMoney());
    }
    
    public void RunStep() {
        Vector2 mousePosition = ControlsService.Instance.MousePosition();
        float screenHeight = Screen.height;
        float topAreaThreshold = screenHeight * 0.05f;
        if (mousePosition.y > (screenHeight - topAreaThreshold)) {
            _currentTarget = 0;
            _timer = 0;
        } else {
            _timer += Time.deltaTime;
        }
        if (_timer >= _timeUntilHide) {
            _currentTarget = -_infoBar.resolvedStyle.height;
        }

        if (_infoBar.style.top != _currentTarget) {
            _infoBar.style.top = Mathf.MoveTowards(_infoBar.style.top.value.value, _currentTarget, _tweenSpeed * Time.deltaTime);
        }
    }
    
    public void CleanupStep() { }

    public void SetDate(DateTime date) {
        string suffix = date.Day switch {
            1 or 21 or 31 => "st",
            2 or 22 => "nd",
            3 or 23 => "rd",
            _ => "th"
        };
        _dateLabel.text = $"{date:dddd}, {date:MMMM} {date.Day}{suffix}";
    }

    public void SetTownName(string name) => _townLabel.text = name;

    public void SetRatingIcon(int val) {
        if (val >= 95) {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[0]);
        } else if (val >= 90) {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[1]);
        } else if (val >= 80) {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[2]);
        } else if (val >= 70) {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[3]);
        } else if (val >= 60) {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[4]);
        } else {
            _gradeSprite.style.backgroundImage = new StyleBackground(_gradeSprites[5]);
        }
    }

    private void SetMoneyCount(int amount) => _moneyLabel.text = amount.ToString();
}
