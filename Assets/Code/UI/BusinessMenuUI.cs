using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class BusinessMenuUI : MonoBehaviour {
    [SerializeField] private VisualTreeAsset _businessButton;

    private UIDocument _document;

    private VisualElement _root;
    private ScrollView _items;

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;
        _root = root.Q<VisualElement>("BusinessMenuRoot");
        _items = _root.Q<ScrollView>("Items");

        _root.RegisterCallback<ClickEvent>(_ => Disable());
        Disable();
    }

    public void Enable() {
        _root.style.display = DisplayStyle.Flex;
    }

    public void Disable() {
        _root.style.display = DisplayStyle.None;
    }

    public bool IsEnabled() => _root.style.display == DisplayStyle.Flex;

    public void AddButtonToScrollViewMenu(BusinessShopContext context, Action clickAction) {
        var buttonInstance = _businessButton.Instantiate();
        var button = buttonInstance.Q<Button>();
        var icon = buttonInstance.Q<VisualElement>("Icon");
        icon.style.backgroundImage = new StyleBackground(context.UISprite);
        var title = buttonInstance.Q<Label>("Name");
        var price = buttonInstance.Q<Label>("PurchaseCostValue");
        var upkeep = buttonInstance.Q<Label>("UpkeepCostValue");
        var revenue = buttonInstance.Q<Label>("RevenuePerCustomerValue");
        var desc = buttonInstance.Q<Label>("BusinessDescription");
        title.text = context.Title;
        price.text = context.Cost.ToString();
        upkeep.text = context.WeeklyUpkeepCost.ToString();
        revenue.text = context.RevenuePerCustomer.ToString();
        desc.text = context.Description;

        _items.Add(buttonInstance);

        button.RegisterCallback<ClickEvent>(evt => clickAction?.Invoke());
    }
}