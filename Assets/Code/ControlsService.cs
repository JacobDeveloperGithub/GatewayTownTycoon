using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ControlsService : MonoBehaviour {
    public static ControlsService Instance {get; private set;}

    private PlayerInput _playerInput;
    private InputActionMap _map;
    private InputAction _mousePosition;
    private InputAction _leftMouse;
    private InputAction _rightMouse;
    private InputAction _space;
    private InputAction _undo;
    private InputAction _esc;

    public bool IsTouchScheme { get; private set; }
    public bool IsKeyboardScheme { get; private set; }

    private void Awake() {
        if (Instance && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _playerInput = GetComponent<PlayerInput>();
        CacheActions(_playerInput.currentActionMap);

        _playerInput.onControlsChanged += OnControlsChanged;

        UpdateControlScheme(_playerInput.currentControlScheme);
    }

    private void CacheActions(InputActionMap map) {
        _mousePosition = map.FindAction("MousePosition", true);
        _leftMouse = map.FindAction("LeftMousePressed", true);
        _rightMouse = map.FindAction("RightMousePressed", true);
        _space = map.FindAction("Space", true);
        _undo = map.FindAction("Undo", true);
        _esc = map.FindAction("Esc", true);
    }

    private void OnControlsChanged(PlayerInput obj) {
        UpdateControlScheme(obj.currentControlScheme);
    }

    private void UpdateControlScheme(string scheme) {
        IsKeyboardScheme = scheme == "Keyboard&Mouse" || scheme == "Keyboard";
        IsTouchScheme = scheme == "Touch";
    }

    public Vector2 MousePosition() => _mousePosition.ReadValue<Vector2>();
    public bool LeftMouseClicked() => _leftMouse.WasPressedThisFrame();
    public bool RightMouseClicked() => _rightMouse.WasPressedThisFrame();
    public bool IsLeftMouseClicked() => _leftMouse.IsPressed();
    public bool IsRightMouseClicked() => _rightMouse.IsPressed();
    public bool IsSpacePressed() => _space.IsPressed();
    public bool UndoPressed() => _undo.WasPressedThisFrame();
    public bool EscPressed() => _esc.WasPressedThisFrame();
}