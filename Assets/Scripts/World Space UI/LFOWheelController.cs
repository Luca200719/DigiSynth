using UnityEngine;

public class LFOWheelController : WheelController {
    AudioController _audioController;

    public int oscillatorIndex;

    protected override void Awake() {
        ObjectRegistry.registry.Register(gameObject, "LFO Wheel");

        base.Awake();

        minRot = -45f;
        maxRot = 45f;

        oscillatorIndex = transform.name[6] - '1';
    }

    protected override void Start() {
        _audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();

        base.Start();

        UpdateOscillatorLevel();
    }

    public void LoadFromCurrentState() {
        float level = DataManager.dataManager.data.currentState.oscillatorLevel[oscillatorIndex];
        float normalizedAngle = 1f - level;
        float angle = minRot + normalizedAngle * (maxRot - minRot);
        SetTargetAngle(angle);
        UpdateOscillatorLevel();
    }

    protected override void HandleInput() {
        base.HandleInput();

        if (isActive && !_justActivated) {
            UpdateOscillatorLevel();
        }
    }

    protected override void OnRotationComplete() {
        base.OnRotationComplete();
        UpdateOscillatorLevel();
    }

    protected override void OnKnobReleased() {
        CheckDirtyLFO();
    }

    protected void CheckDirtyLFO() {
        if (DataManager.dataManager.data.isDirty) return;

        Preset currentPreset = DataManager.dataManager.presetData.presets[DataManager.dataManager.data.presetID - 1];
        float presetLevel = currentPreset.oscillatorLevel[oscillatorIndex];
        float currentLevel = GetNormalizedValue();
        if (!Mathf.Approximately(currentLevel, presetLevel)) {
            DataManager.dataManager.data.isDirty = true;
            PresetDisplayController.displayController.SetDirty(true);
        }
    }

    void UpdateOscillatorLevel() {
        if (oscillatorIndex >= 0 && oscillatorIndex < 4) {
            float level = GetNormalizedValue();
            _audioController.oscillatorLevel[oscillatorIndex] = level;
            DataManager.dataManager.data.currentState.oscillatorLevel[oscillatorIndex] = level;
        }
    }

    public override float GetNormalizedValue() {
        return 1f - ((currentAngle - minRot) / (maxRot - minRot));
    }
}