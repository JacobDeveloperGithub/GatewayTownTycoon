using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NotificationManager : MonoBehaviour {
    public static NotificationManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset notificationTemplate;

    [SerializeField] private int maxVisible = 3;
    [SerializeField] private float showDuration = 5f;
    [SerializeField] private float tweenDuration = 0.4f;
    [SerializeField] private float tweenOffsetY = 80f;

    private VisualElement root;
    private VisualElement notificationsContainer;

    private readonly Queue<string> pendingMessages = new();
    private readonly HashSet<string> liveMessages = new();
    private readonly List<VisualElement> activeNotifications = new();

    private void Awake() {
        if (Instance && Instance != this) Destroy(this);
        else Instance = this;

        root = uiDocument.rootVisualElement;
        notificationsContainer = root.Q<VisualElement>("NotificationsContainer");
        if (notificationsContainer == null) {
            Debug.LogError("NotificationManager: 'NotificationsContainer' not found in UXML.");
        }
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    public void EnqueueNotification(string message) {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (liveMessages.Contains(message)) return;
        pendingMessages.Enqueue(message);
        liveMessages.Add(message);
        TryShowNext();
    }

    private void TryShowNext() {
        if (pendingMessages.Count == 0) return;
        if (activeNotifications.Count >= maxVisible) return;
        var message = pendingMessages.Dequeue();
        CreateAndPlayNotification(message);
    }

    private void CreateAndPlayNotification(string message) {
        if (notificationsContainer == null || notificationTemplate == null) return;
        var instance = notificationTemplate.CloneTree();
        instance.name = "NotificationInstance";
        var bar = instance.Q<VisualElement>("NotificationBar");
        var label = instance.Q<Label>("NotificationLabel");
        if (label != null) {
            label.text = message;
        }
        instance.userData = message;
        notificationsContainer.Add(instance);
        activeNotifications.Add(instance);
        float startY = tweenOffsetY;
        float endY = 0f;
        instance.style.translate = new Translate(0, startY, 0);
        LeanTween.value(gameObject, startY, endY, tweenDuration)
            .setEaseOutCubic()
            .setOnUpdate(v =>
            {
                instance.style.translate = new Translate(0, v, 0);
            });
        StartCoroutine(HideAfterDelay(instance, showDuration));
    }

    private IEnumerator HideAfterDelay(VisualElement instance, float delay) {
        yield return new WaitForSeconds(delay);
        if (instance == null) yield break;
        float startY = 0f;
        float endY = tweenOffsetY;
        bool finished = false;
        LeanTween.value(gameObject, startY, endY, tweenDuration)
            .setEaseInCubic()
            .setOnUpdate(v =>
            {
                instance.style.translate = new Translate(0, v, 0);
            })
            .setOnComplete(() =>
            {
                finished = true;
            });
        while (!finished) yield return null;
        if (instance.parent != null) {
            instance.parent.Remove(instance);
        }
        activeNotifications.Remove(instance);
        if (instance.userData is string msg) {
            liveMessages.Remove(msg);
        }
        TryShowNext();
    }
}