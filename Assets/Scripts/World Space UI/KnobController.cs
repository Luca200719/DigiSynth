using UnityEngine;
using UnityEngine.InputSystem;

public class KnobController : MonoBehaviour {
    public float sensitivity = 1.5f;
    public float minRot = -125;
    public float maxRot = 125;
    public float startAngle;
    public float rotationSpeed = 2f;
    public float fineTuningMultiplier = 0.2f;

    public float maxDeltaTime = 0.05f;

    protected Mouse _mouse;
    protected Keyboard _keyboard;

    public bool isActive = false;
    protected bool _justActivated = false;
    protected Vector2 _clickPos;

    public float currentAngle;
    protected float targetAngle;
    protected bool hasTargetAngle = false;
    protected float _effectiveSensitivity;

    protected Quaternion initialRotation;

    protected const float DEFAULT_HEIGHT = 1080f;
    protected const float MIN_ANGLE = 0.5f;

    public bool displayParameter;
    protected DisplayController displayController;

    protected string knobName;

    protected float lastDisplayedValue = -1f;
    protected const float DISPLAY_UPDATE_THRESHOLD = 0.001f;

    protected virtual void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Knob");

        _mouse = Mouse.current;
        _keyboard = Keyboard.current;

        currentAngle = startAngle;
        targetAngle = startAngle;

        initialRotation = transform.rotation;
        SetKnobRotation(startAngle);

        _effectiveSensitivity = sensitivity * (DEFAULT_HEIGHT / Screen.height);
    }

    protected virtual void Start() {
        if (displayParameter) {
            displayController = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();
            knobName = transform.parent.name;

            if (EffectsController.knobEffects.ContainsKey(knobName)) {
                UpdateDisplayValue((currentAngle - minRot) / (maxRot - minRot), true);
            }
        }
    }

    protected virtual void Update() {
        HandleInput();
        HandleRotation();
    }

    protected virtual void HandleInput() {
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
            float newTargetAngle = Mathf.Clamp(targetAngle + deltaY, minRot, maxRot);

            if (newTargetAngle != targetAngle) {
                SetTargetAngle(newTargetAngle);

                if (displayParameter) {
                    float normalizedValue = (targetAngle - minRot) / (maxRot - minRot);
                    UpdateDisplayValue(normalizedValue, false);
                }
            }
        }

        _justActivated = false;
    }

    public virtual void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            _clickPos = _mouse.position.ReadValue();
            isActive = true;
            hasTargetAngle = false;
            _justActivated = true;
            CursorManager.cursorManager.lockPosition = true;
        }
    }

    protected virtual float GetEffectiveSensitivity() {
        bool isShiftHeld = _keyboard.leftShiftKey.isPressed || _keyboard.rightShiftKey.isPressed;
        float currentSensitivity = _effectiveSensitivity;

        if (isShiftHeld) {
            currentSensitivity *= fineTuningMultiplier;
        }

        return currentSensitivity;
    }

    protected virtual void HandleRotation() {
        if (hasTargetAngle) {
            float angleDifference = targetAngle - currentAngle;

            if (Mathf.Abs(angleDifference) < MIN_ANGLE) {
                currentAngle = targetAngle;
                SetKnobRotation(currentAngle);
                hasTargetAngle = false;

                OnRotationComplete();
            } else {
                float cappedDeltaTime = Mathf.Min(Time.deltaTime, maxDeltaTime);

                float normalizedDifference = Mathf.Abs(angleDifference) / (maxRot - minRot);
                float easingMultiplier = 1f + (normalizedDifference * 19f);

                float rotationAmount = angleDifference * rotationSpeed * easingMultiplier * cappedDeltaTime;

                if (Mathf.Abs(rotationAmount) > Mathf.Abs(angleDifference)) {
                    rotationAmount = angleDifference;
                }

                currentAngle += rotationAmount;

                currentAngle = Mathf.Clamp(currentAngle, minRot, maxRot);

                SetKnobRotation(currentAngle);

                if (Mathf.Abs(targetAngle - currentAngle) < MIN_ANGLE) {
                    currentAngle = targetAngle;
                    hasTargetAngle = false;

                    OnRotationComplete();
                }
            }
        }
    }

    protected virtual void SetKnobRotation(float angle) {
        Quaternion targetRotation = initialRotation * Quaternion.AngleAxis(angle, Vector3.up);
        transform.rotation = targetRotation;
    }

    protected virtual void SetTargetAngle(float angle) {
        targetAngle = Mathf.Clamp(angle, minRot, maxRot);
        hasTargetAngle = true;
    }

    public void LoadAngle(float angle) {
        SetTargetAngle(angle);
    }

    protected void CheckDirty(float angle) {
        if (DataManager.dataManager.data.isDirty) return;
        if (!EffectsController.knobEffects.ContainsKey(knobName)) return;

        Preset currentPreset = DataManager.dataManager.presetData.presets[DataManager.dataManager.data.presetID - 1];
        int effectIndex = (int)EffectsController.knobEffects[knobName];
        float normalizedValue = (angle - minRot) / (maxRot - minRot);

        if (!Mathf.Approximately(normalizedValue, currentPreset.valueCaps[effectIndex])) {
            DataManager.dataManager.data.isDirty = true;
            PresetDisplayController.displayController.SetDirty(true);
        }
    }

    public virtual void ResetKnob() {
        currentAngle = startAngle;
        targetAngle = startAngle;
        hasTargetAngle = false;
        SetKnobRotation(currentAngle);

        if (displayParameter && EffectsController.knobEffects.ContainsKey(knobName)) {
            UpdateDisplayValue((currentAngle - minRot) / (maxRot - minRot), true);
        }
    }

    public virtual bool IsRotationValid() {
        float expectedY = (initialRotation * Quaternion.AngleAxis(currentAngle, Vector3.up)).eulerAngles.y;
        float actualY = transform.rotation.eulerAngles.y;
        float difference = Mathf.DeltaAngle(expectedY, actualY);
        return Mathf.Abs(difference) < 1f;
    }

    protected virtual void UpdateDisplayValue(float normalizedValue, bool forceUpdate) {
        if (!displayParameter) return;

        if (!forceUpdate && Mathf.Abs(normalizedValue - lastDisplayedValue) < DISPLAY_UPDATE_THRESHOLD) {
            return;
        }

        lastDisplayedValue = normalizedValue;
        displayController.UpdateDisplayParameter(EffectsController.knobEffects[knobName], normalizedValue);
    }

    protected virtual void OnRotationComplete() {
        if (displayParameter) {
            float normalizedValue = (currentAngle - minRot) / (maxRot - minRot);
            UpdateDisplayValue(normalizedValue, false);
        }
    }

    protected virtual void OnKnobReleased() {
        CheckDirty(currentAngle);
    }
}