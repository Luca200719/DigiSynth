using UnityEngine;

public class ClickWheelController : WheelController {
    public float[] clickValues;
    public float clickThreshold = 0.05f;

    protected bool shouldSnapToClickValue;
    protected float targetClickAngle = 0f;

    protected override void Awake() {
        base.Awake();
        ObjectRegistry.registry.Register(gameObject, "Click Wheel");
    }

    protected override void Update() {
        base.Update();
        HandleClickValues();
    }

    protected override void HandleInput() {
        var leftButton = _mouse.leftButton;

        if (leftButton.wasReleasedThisFrame && isActive) {
            isActive = false;
            OnWheelReleased();
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
                shouldSnapToClickValue = false;
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

            shouldSnapToClickValue = false;
        }
    }

    protected virtual void OnWheelReleased() {
        if (clickValues != null && clickValues.Length > 0 && clickThreshold > 0) {
            float normalizedValue = (currentAngle - minRot) / (maxRot - minRot);
            float closestValue = clickValues[0];
            float closestDistance = Mathf.Abs(normalizedValue - closestValue);

            for (int i = 1; i < clickValues.Length; i++) {
                float distance = Mathf.Abs(normalizedValue - clickValues[i]);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestValue = clickValues[i];
                }
            }

            if (closestDistance <= clickThreshold) {
                targetClickAngle = minRot + (closestValue * (maxRot - minRot));
                shouldSnapToClickValue = true;
            }
        }
    }

    protected virtual void HandleClickValues() {
        if (shouldSnapToClickValue && !isActive) {
            SetTargetAngle(targetClickAngle);
            shouldSnapToClickValue = false;
            OnSnapComplete();
        }
    }

    protected virtual void OnSnapComplete() {
        CheckDirty(targetClickAngle);
    }
}