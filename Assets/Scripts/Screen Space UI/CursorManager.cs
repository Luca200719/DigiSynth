using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour {
    public Sprite defaultCursorSprite;
    public Sprite pressedCursorSprite;
    public CanvasGroup inputBlocker;

    Mouse _mouse;
    RectTransform _cursorRect;
    Image _cursorImage;

    public static CursorManager cursorManager { get; private set; }

    public bool lockPosition {
        get => _lockPosition;
        set {
            _lockPosition = value;
            inputBlocker.blocksRaycasts = value;
        }
    }
    bool _lockPosition;

    void Awake() {
        if (cursorManager == null) {
            cursorManager = this;
        }
        else {
            Destroy(gameObject);
            return;
        }
    }

    void Start() {
        Cursor.visible = false;

        _mouse = Mouse.current;

        _cursorRect = transform.parent.GetComponent<RectTransform>();
        _cursorImage = GetComponent<Image>();

        _cursorImage.sprite = defaultCursorSprite;
    }

    void Update() {
        if (!lockPosition) {
            Vector2 mousePos = _mouse.position.ReadValue();
            _cursorRect.position = mousePos;
        }

        if (_mouse.leftButton.wasPressedThisFrame) {
            _cursorImage.sprite = pressedCursorSprite;
        }

        if (_mouse.leftButton.wasReleasedThisFrame) {
            _cursorImage.sprite = defaultCursorSprite;
        }
    }

    public void UnlockPositionNextFrame() {
        StartCoroutine(UnlockPositionCoroutine());
    }

    private System.Collections.IEnumerator UnlockPositionCoroutine() {
        yield return null;
        lockPosition = false;
    }
}