using UnityEngine;
using UnityEngine.InputSystem;

public class WheelController : KnobController {
    protected override void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Knob");

        minRot = -50f;
        maxRot = 50f;

        _mouse = Mouse.current;
        _keyboard = Keyboard.current;

        currentAngle = startAngle;
        targetAngle = startAngle;

        initialRotation = transform.rotation;
        SetWheelRotation(startAngle);

        _effectiveSensitivity = sensitivity * (DEFAULT_HEIGHT / Screen.height);
    }

    protected override void Update() {
        HandleInput();
        HandleRotation();
    }

    protected override void HandleInput() {
        var leftButton = _mouse.leftButton;

        if (leftButton.wasReleasedThisFrame && isActive) {
            isActive = false;
            OnKnobReleased();
            _mouse.WarpCursorPosition(_clickPos);
            CursorManager.cursorManager.UnlockPositionNextFrame();
        }

        if (isActive && !_justActivated) {
            float deltaY = _mouse.delta.ReadValue().y;
            float currentSensitivity = GetEffectiveSensitivity();

            deltaY *= currentSensitivity;
            float newTargetAngle = Mathf.Clamp(targetAngle - deltaY, minRot, maxRot);

            if (newTargetAngle != targetAngle) {
                SetTargetAngle(newTargetAngle);
            }
        }

        _justActivated = false;
    }

    public override void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            _clickPos = _mouse.position.ReadValue();
            isActive = true;
            hasTargetAngle = false;
            _justActivated = true;
            CursorManager.cursorManager.lockPosition = true;
        }
    }

    protected void SetWheelRotation(float angle) {
        Quaternion targetRotation = initialRotation * Quaternion.AngleAxis(angle, Vector3.right);
        transform.rotation = targetRotation;
    }

    protected override void SetKnobRotation(float angle) {
        SetWheelRotation(angle);
    }

    public override bool IsRotationValid() {
        float expectedX = (initialRotation * Quaternion.AngleAxis(currentAngle, Vector3.right)).eulerAngles.x;
        float actualX = transform.rotation.eulerAngles.x;
        float difference = Mathf.DeltaAngle(expectedX, actualX);
        return Mathf.Abs(difference) < 1f;
    }

    public virtual float GetNormalizedValue() {
        return (currentAngle - minRot) / (maxRot - minRot);
    }
}