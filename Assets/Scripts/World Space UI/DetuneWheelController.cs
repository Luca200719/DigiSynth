using UnityEngine;

public class DetuneWheelController : ClickWheelController {
    Renderer indicator;

    public Material unlit;
    public Material lit;

    protected bool isWithinZeroThreshold = false;

    protected override void Start() {
        base.Start();
        indicator = transform.parent.GetChild(2).GetComponent<Renderer>();
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
                UpdateIndicator();
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

    protected override void OnWheelReleased() {
        base.OnWheelReleased();
        if (!shouldSnapToClickValue) {
            if (DataManager.dataManager.data.isDirty) return;

            Preset currentPreset = DataManager.dataManager.presetData.presets[DataManager.dataManager.data.presetID - 1];
            float normalizedValue = (currentAngle - minRot) / (maxRot - minRot);
            float presetValue = currentPreset.valueCaps[(int)EffectsController.EffectParameter.Detune];
            if (!Mathf.Approximately(normalizedValue, presetValue))
                DataManager.dataManager.data.isDirty = true;
        }
    }

    protected override void OnSnapComplete() {
        Preset currentPreset = DataManager.dataManager.presetData.presets[DataManager.dataManager.data.presetID - 1];
        float normalizedValue = (targetClickAngle - minRot) / (maxRot - minRot);
        float presetValue = currentPreset.valueCaps[(int)EffectsController.EffectParameter.Detune];
        if (!Mathf.Approximately(normalizedValue, presetValue))
            DataManager.dataManager.data.isDirty = true;
    }

    protected void UpdateIndicator() {
        float distanceFromCenter = Mathf.Abs(currentAngle);
        float threshold = clickThreshold * (maxRot - minRot);
        bool shouldBeLit = distanceFromCenter <= threshold;

        if (shouldBeLit != isWithinZeroThreshold) {
            isWithinZeroThreshold = shouldBeLit;
            indicator.material = shouldBeLit ? lit : unlit;
        }
    }

    protected override void Update() {
        base.Update();
        if (!isActive && hasTargetAngle) {
            CheckIndicatorWhileMoving();
        }
    }

    protected void CheckIndicatorWhileMoving() {
        float threshold = clickThreshold * (maxRot - minRot);
        float distanceFromCenter = Mathf.Abs(currentAngle);
        bool shouldBeLit = distanceFromCenter <= threshold;

        if (shouldBeLit != isWithinZeroThreshold) {
            isWithinZeroThreshold = shouldBeLit;
            indicator.material = shouldBeLit ? lit : unlit;
        }
    }
}