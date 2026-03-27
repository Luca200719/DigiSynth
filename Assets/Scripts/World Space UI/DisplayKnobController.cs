using UnityEngine;

public class DisplayKnobController : KnobController {
    public bool changingState;
    public float newStateAngle = -1f;

    public KnobProperties properties;
    bool shouldSnapToClickValue = false;
    float targetClickAngle = 0f;
    bool isSnappingToClickValue = false;

    EffectsDisplayController effectsDisplayController;
    int knobIndex = -1;

    public Material litMaterial;
    public Material unlitMaterial;

    protected override void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Display Knob");
        base.Awake();
    }

    protected override void Start() {
        base.Start();
        effectsDisplayController = ObjectRegistry.registry.GetObjectList("Effects Display")[0].GetComponent<EffectsDisplayController>();

        knobIndex = transform.parent.name[4] - '1';
    }

    protected override void Update() {
        HandleInput();
        HandleClickValues();
        HandleRotation();
    }

    protected override void HandleInput() {
        base.HandleInput();

        if (_justActivated && isActive) {
            isSnappingToClickValue = false;
            shouldSnapToClickValue = false;
        }

        if (isActive && !_justActivated) {
            if (shouldSnapToClickValue || isSnappingToClickValue) {
                shouldSnapToClickValue = false;
                isSnappingToClickValue = false;
            }
            UpdateKnobDisplay(targetAngle);
        }
    }

    protected override float GetEffectiveSensitivity() {
        return isSnappingToClickValue ? _effectiveSensitivity : base.GetEffectiveSensitivity();
    }

    void HandleClickValues() {
        if (shouldSnapToClickValue && !isActive) {
            SetTargetAngle(targetClickAngle);
            shouldSnapToClickValue = false;
            isSnappingToClickValue = true;
            UpdateKnobDisplay(targetClickAngle);

            CheckDirtyDisplay(targetClickAngle);
        }
    }

    protected override void HandleRotation() {
        if (!hasTargetAngle && !changingState) return;

        float activeTargetAngle = changingState ? newStateAngle : targetAngle;
        float angleDifference = activeTargetAngle - currentAngle;

        if (Mathf.Abs(angleDifference) < MIN_ANGLE) {
            FinalizeRotation(activeTargetAngle);
            return;
        }

        if (changingState && isActive && !_justActivated) {
            FinalizeRotation(activeTargetAngle);
            return;
        }

        float cappedDeltaTime = Mathf.Min(Time.deltaTime, maxDeltaTime);
        float normalizedDifference = Mathf.Abs(angleDifference) / (maxRot - minRot);
        float easingMultiplier = 1f + (normalizedDifference * 19f);

        float rotationAmount = angleDifference * rotationSpeed * easingMultiplier * cappedDeltaTime;
        rotationAmount = Mathf.Sign(angleDifference) * Mathf.Min(Mathf.Abs(rotationAmount), Mathf.Abs(angleDifference));

        currentAngle = Mathf.Clamp(currentAngle + rotationAmount, minRot, maxRot);
        SetKnobRotation(currentAngle);

        if (Mathf.Abs(activeTargetAngle - currentAngle) < MIN_ANGLE) {
            FinalizeRotation(activeTargetAngle);
        }
    }

    void FinalizeRotation(float target) {
        currentAngle = target;
        targetAngle = target;
        hasTargetAngle = false;
        changingState = false;
        isSnappingToClickValue = false;

        SetKnobRotation(currentAngle);
        OnRotationComplete();
    }

    void CheckDirtyDisplay(float angle) {
        int effectIndex = effectsDisplayController.currentEffectIndex;
        if (effectIndex < 0) return;
        if (DataManager.dataManager.data.isDirty) return;

        Preset currentPreset = DataManager.dataManager.presetData.presets[DataManager.dataManager.data.presetID - 1];
        float normalizedValue = (angle - minRot) / (maxRot - minRot);
        float presetValue = currentPreset.displayKnobParameters[effectIndex * 4 + knobIndex];
        if (!Mathf.Approximately(normalizedValue, presetValue))
            DataManager.dataManager.data.isDirty = true;
    }

    protected override void OnKnobReleased() {
        if (properties.GetClickValues != null && properties.GetClickValues.Length > 0) {
            float threshold = properties.GetClickThreshold;
            if (threshold > 0) {
                float normalizedValue = (currentAngle - minRot) / (maxRot - minRot);

                float[] clickValues = properties.GetClickValues;
                float closestValue = clickValues[0];
                float closestDistance = Mathf.Abs(normalizedValue - closestValue);

                for (int i = 1; i < clickValues.Length; i++) {
                    float distance = Mathf.Abs(normalizedValue - clickValues[i]);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestValue = clickValues[i];
                    }
                }

                if (closestDistance <= threshold) {
                    targetClickAngle = minRot + (closestValue * (maxRot - minRot));
                    shouldSnapToClickValue = true;
                    return;
                }
            }
        }

        UpdateKnobDisplay(currentAngle);
        base.OnKnobReleased();
    }

    protected override void OnRotationComplete() {
        if (!isActive) UpdateKnobDisplay(currentAngle);
    }

    void UpdateKnobDisplay(float angle) {
        if (properties.GetComputation == null) return;

        float normalizedValue = (angle - minRot) / (maxRot - minRot);
        properties.SetValue(normalizedValue);

        if (knobIndex >= 0 && knobIndex < 4) {
            effectsDisplayController.UpdateKnobValue(knobIndex, normalizedValue);
        }
    }

    public void UpdateClickLights() {
        TurnOffAllLights();

        Transform lightsParent = transform.parent.GetChild(1);

        foreach (float clickValue in properties.GetClickValues) {
            string lightName = clickValue.ToString("G7");
            Transform lightTransform = lightsParent.Find(lightName);
            Renderer renderer = lightTransform.GetComponent<Renderer>();
            renderer.material = litMaterial;
        }
    }

    public void TurnOffAllLights() {
        Transform lightsParent = transform.parent.GetChild(1);

        for (int i = 0; i < lightsParent.childCount; i++) {
            Renderer renderer = lightsParent.GetChild(i).GetComponent<Renderer>();
            renderer.material = unlitMaterial;
        }
    }
}