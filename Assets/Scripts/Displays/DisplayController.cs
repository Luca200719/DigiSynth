using UnityEngine;
using System.Collections.Generic;

public enum EffectType {
    Compression = 0,
    Saturation = 1,
    Drive = 2,
    Chorus = 3,
    Phaser = 4,
    Vibrato = 5,
    Tremolo = 6,
    Reverb = 7,
    Delay = 8
}

public enum EffectParameter {
    Param0 = 0,
    Param1 = 1,
    Param2 = 2,
    Param3 = 3
}

public delegate void KnobComputation(float knobValue);

public struct KnobProperties {
    float knobValue;
    KnobComputation computation;
    float[] clickValues;
    float clickThreshold;

    public KnobProperties(float knobValue, KnobComputation computation) {
        this.knobValue = knobValue;
        this.computation = computation;
        clickValues = null;
        clickThreshold = -1f;
    }

    public KnobProperties(float knobValue, KnobComputation computation, float[] clickValues, float clickThreshold) {
        this.knobValue = knobValue;
        this.computation = computation;
        this.clickValues = clickValues;
        this.clickThreshold = clickThreshold;
    }

    public void SetValue(float newKnobValue) {
        knobValue = newKnobValue;
        computation?.Invoke(knobValue);
    }

    public float GetValue => knobValue;
    public float[] GetClickValues => clickValues;
    public float GetClickThreshold => clickThreshold;
    public KnobComputation GetComputation => computation;
}

public class DisplayController : MonoBehaviour {
    EffectsController effectsController;
    AudioController audioController;
    EffectsDisplayController effectsDisplayController;
    EQDisplayController eqDisplayController;
    EnvelopeDisplayController envelopeDisplayController;
    GlobalKnobsDisplayController globalKnobsDisplayController;

    int currentEffectIndex = -1;
    int currentActiveIndex = -1;
    bool[] effectEnabledStates = new bool[EFFECT_COUNT];
    List<int> activeEffects = new List<int>();

    static readonly string[] effectNames = {
        "Compression", "Saturation", "Drive", "Chorus",
        "Phaser", "Vibrato", "Tremolo", "Reverb", "Delay"
    };

    static readonly string[,] knobNames = new string[9, 4] {
        { "Threshold", "Ratio", "Attack", "Output" },
        { "Drive", "Character", "Warmth", "Output" },
        { "Drive", "Character", "Tone", "Output" },
        { "Rate", "Depth", "Delay", "Mix" },
        { "Rate", "Depth", "Feedback", "Mix" },
        { "Rate", "Depth", "Shape", "Intensity" },
        { "Rate", "Depth", "Shape", "Intensity" },
        { "Size", "Decay", "Damping", "Mix" },
        { "Time", "Feedback", "Tone", "Mix" }
    };

    float[,] internalParameters = new float[EFFECT_COUNT, PARAM_COUNT];
    static KnobProperties[,] knobStates;
    DisplayKnobController[] displayKnobs = new DisplayKnobController[PARAM_COUNT];

    static readonly float[] OFF_UNITY_MAX = { 0f, 0.5f, 1f };
    static readonly float[] FIVE_STEPS = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    static readonly float[] FOUR_STEPS = { 0f, 0.333333f, 0.666667f, 1f };
    static readonly float[] WAVEFORMS = { 0f, 0.333333f, 0.666667f, 1f };
    static readonly float[] MIX_LEVELS = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    static readonly float[] RATIO_STEPS = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };

    const int EFFECT_COUNT = 9;
    const int PARAM_COUNT = 4;

    private string[] cachedKnobNames = new string[PARAM_COUNT];
    private float[] cachedKnobValues = new float[PARAM_COUNT];

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Display");
        InitializeKnobStates();

        if (DataManager.dataManager.data.firstTime) {
            for (int i = 0; i < EFFECT_COUNT; i++) {
                for (int j = 0; j < PARAM_COUNT; j++) {
                    DataManager.dataManager.presetData.blankPreset.displayKnobParameters[i * PARAM_COUNT + j] = knobStates[i, j].GetValue;
                }
            }
            DataManager.dataManager.presetData.blankPreset.selectedEffectIndex = -1;
        }
    }

    void Start() {
        CacheDependencies();
        InitializeDisplayKnobs();
        InitializeEffectStates();
        SelectNoEffect();
    }

    public void WriteDefaultsToBlankPreset() {
        for (int i = 0; i < EFFECT_COUNT; i++) {
            for (int j = 0; j < PARAM_COUNT; j++) {
                DataManager.dataManager.presetData.blankPreset.displayKnobParameters[i * PARAM_COUNT + j] = knobStates[i, j].GetValue;
            }
        }
    }

    public void SaveToCurrentState() {
        for (int i = 0; i < EFFECT_COUNT; i++)
            for (int j = 0; j < PARAM_COUNT; j++)
                DataManager.dataManager.data.currentState.displayKnobParameters[i * PARAM_COUNT + j] =
                    effectEnabledStates[i] ? internalParameters[i, j] : knobStates[i, j].GetValue;

        DataManager.dataManager.data.currentState.selectedEffectIndex = currentEffectIndex;
    }

    public void LoadFromCurrentState() {
        for (int i = 0; i < EFFECT_COUNT; i++)
            for (int j = 0; j < PARAM_COUNT; j++)
                internalParameters[i, j] = DataManager.dataManager.data.currentState.displayKnobParameters[i * PARAM_COUNT + j];

        for (int i = 0; i < EFFECT_COUNT; i++) {
            if (effectEnabledStates[i]) {
                for (int j = 0; j < PARAM_COUNT; j++) {
                    float val = internalParameters[i, j];
                    knobStates[i, j] = new KnobProperties(val, knobStates[i, j].GetComputation,
                        knobStates[i, j].GetClickValues, knobStates[i, j].GetClickThreshold);
                }
                ComputeEffectOutput((EffectType)i);
            }
        }

        int savedIndex = DataManager.dataManager.data.currentState.selectedEffectIndex;
        if (savedIndex >= 0 && effectEnabledStates[savedIndex]) {
            currentEffectIndex = savedIndex;
            currentActiveIndex = activeEffects.IndexOf(savedIndex);
            SelectKnobState(savedIndex);
        }
        else if (activeEffects.Count > 0) {
            currentEffectIndex = activeEffects[0];
            currentActiveIndex = 0;
            SelectKnobState(activeEffects[0]);
        }
        else {
            SelectNoEffect();
        }
    }

    void CacheDependencies() {
        effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        effectsDisplayController = ObjectRegistry.registry.GetObjectList("Effects Display")[0].GetComponent<EffectsDisplayController>();
        eqDisplayController = ObjectRegistry.registry.GetObjectList("EQ Display")[0].GetComponent<EQDisplayController>();
        envelopeDisplayController = ObjectRegistry.registry.GetObjectList("Envelope Display")[0].GetComponent<EnvelopeDisplayController>();
        globalKnobsDisplayController = ObjectRegistry.registry.GetObjectList("Global Knobs Display")[0].GetComponent<GlobalKnobsDisplayController>();
    }

    void InitializeDisplayKnobs() {
        foreach (GameObject dK in ObjectRegistry.registry.GetObjectList("Display Knob")) {
            displayKnobs[dK.transform.parent.name[4] - '1'] = dK.GetComponent<DisplayKnobController>();
        }
    }

    void InitializeEffectStates() {
        for (int i = 0; i < EFFECT_COUNT; i++) {
            effectEnabledStates[i] = false;
            for (int j = 0; j < PARAM_COUNT; j++) {
                internalParameters[i, j] = 0f;
            }
        }
    }

    void InitializeKnobStates() {
        float[,] defaultPresets = {
            { 0.6f, 0.3f, 0.2f, 0.8f },
            { 0.3f, 0.4f, 0.3f, 0.7f },
            { 0.3f, 0.4f, 0.5f, 0.7f },
            { 0.3f, 0.4f, 0.4f, 0.5f },
            { 0.4f, 0.3f, 0.2f, 0.6f },
            { 0.3f, 0.2f, 0.0f, 0.5f },
            { 0.4f, 0.3f, 0.0f, 0.6f },
            { 0.4f, 0.6f, 0.3f, 0.4f },
            { 0.25f, 0.3f, 0.6f, 0.3f }
        };

        knobStates = new KnobProperties[EFFECT_COUNT, PARAM_COUNT] {
            {
                new KnobProperties(defaultPresets[0,0], (val) => UpdateParameter(EffectType.Compression, EffectParameter.Param0, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[0,1], (val) => UpdateParameter(EffectType.Compression, EffectParameter.Param1, val), RATIO_STEPS, 0.12f),
                new KnobProperties(defaultPresets[0,2], (val) => UpdateParameter(EffectType.Compression, EffectParameter.Param2, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[0,3], (val) => UpdateParameter(EffectType.Compression, EffectParameter.Param3, val), OFF_UNITY_MAX, 0.15f)
            },
            {
                new KnobProperties(defaultPresets[1,0], (val) => UpdateParameter(EffectType.Saturation, EffectParameter.Param0, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[1,1], (val) => UpdateParameter(EffectType.Saturation, EffectParameter.Param1, val), FOUR_STEPS, 0.45f),
                new KnobProperties(defaultPresets[1,2], (val) => UpdateParameter(EffectType.Saturation, EffectParameter.Param2, val), OFF_UNITY_MAX, 0.10f),
                new KnobProperties(defaultPresets[1,3], (val) => UpdateParameter(EffectType.Saturation, EffectParameter.Param3, val), OFF_UNITY_MAX, 0.15f)
            },
            {
                new KnobProperties(defaultPresets[2,0], (val) => UpdateParameter(EffectType.Drive, EffectParameter.Param0, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[2,1], (val) => UpdateParameter(EffectType.Drive, EffectParameter.Param1, val), FOUR_STEPS, 0.45f),
                new KnobProperties(defaultPresets[2,2], (val) => UpdateParameter(EffectType.Drive, EffectParameter.Param2, val), OFF_UNITY_MAX, 0.10f),
                new KnobProperties(defaultPresets[2,3], (val) => UpdateParameter(EffectType.Drive, EffectParameter.Param3, val), OFF_UNITY_MAX, 0.15f)
            },
            {
                new KnobProperties(defaultPresets[3,0], (val) => UpdateParameter(EffectType.Chorus, EffectParameter.Param0, val), FIVE_STEPS, 0.10f),
                new KnobProperties(defaultPresets[3,1], (val) => UpdateParameter(EffectType.Chorus, EffectParameter.Param1, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[3,2], (val) => UpdateParameter(EffectType.Chorus, EffectParameter.Param2, val), FOUR_STEPS, 0.12f),
                new KnobProperties(defaultPresets[3,3], (val) => UpdateParameter(EffectType.Chorus, EffectParameter.Param3, val), MIX_LEVELS, 0.08f)
            },
            {
                new KnobProperties(defaultPresets[4,0], (val) => UpdateParameter(EffectType.Phaser, EffectParameter.Param0, val), FIVE_STEPS, 0.10f),
                new KnobProperties(defaultPresets[4,1], (val) => UpdateParameter(EffectType.Phaser, EffectParameter.Param1, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[4,2], (val) => UpdateParameter(EffectType.Phaser, EffectParameter.Param2, val), FOUR_STEPS, 0.12f),
                new KnobProperties(defaultPresets[4,3], (val) => UpdateParameter(EffectType.Phaser, EffectParameter.Param3, val), MIX_LEVELS, 0.08f)
            },
            {
                new KnobProperties(defaultPresets[5,0], (val) => UpdateParameter(EffectType.Vibrato, EffectParameter.Param0, val), FIVE_STEPS, 0.10f),
                new KnobProperties(defaultPresets[5,1], (val) => UpdateParameter(EffectType.Vibrato, EffectParameter.Param1, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[5,2], (val) => UpdateParameter(EffectType.Vibrato, EffectParameter.Param2, val), WAVEFORMS, 0.45f),
                new KnobProperties(defaultPresets[5,3], (val) => UpdateParameter(EffectType.Vibrato, EffectParameter.Param3, val), OFF_UNITY_MAX, 0.15f)
            },
            {
                new KnobProperties(defaultPresets[6,0], (val) => UpdateParameter(EffectType.Tremolo, EffectParameter.Param0, val), FIVE_STEPS, 0.10f),
                new KnobProperties(defaultPresets[6,1], (val) => UpdateParameter(EffectType.Tremolo, EffectParameter.Param1, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[6,2], (val) => UpdateParameter(EffectType.Tremolo, EffectParameter.Param2, val), WAVEFORMS, 0.45f),
                new KnobProperties(defaultPresets[6,3], (val) => UpdateParameter(EffectType.Tremolo, EffectParameter.Param3, val), OFF_UNITY_MAX, 0.15f)
            },
            {
                new KnobProperties(defaultPresets[7,0], (val) => UpdateParameter(EffectType.Reverb, EffectParameter.Param0, val), FIVE_STEPS, 0.10f),
                new KnobProperties(defaultPresets[7,1], (val) => UpdateParameter(EffectType.Reverb, EffectParameter.Param1, val), FIVE_STEPS, 0.08f),
                new KnobProperties(defaultPresets[7,2], (val) => UpdateParameter(EffectType.Reverb, EffectParameter.Param2, val), FOUR_STEPS, 0.12f),
                new KnobProperties(defaultPresets[7,3], (val) => UpdateParameter(EffectType.Reverb, EffectParameter.Param3, val), MIX_LEVELS, 0.08f)
            },
            {
                new KnobProperties(defaultPresets[8,0], (val) => UpdateParameter(EffectType.Delay, EffectParameter.Param0, val), FIVE_STEPS, 0.12f),
                new KnobProperties(defaultPresets[8,1], (val) => UpdateParameter(EffectType.Delay, EffectParameter.Param1, val), FOUR_STEPS, 0.10f),
                new KnobProperties(defaultPresets[8,2], (val) => UpdateParameter(EffectType.Delay, EffectParameter.Param2, val), OFF_UNITY_MAX, 0.10f),
                new KnobProperties(defaultPresets[8,3], (val) => UpdateParameter(EffectType.Delay, EffectParameter.Param3, val), MIX_LEVELS, 0.08f)
            }
        };
    }

    public void OnSelectButtonPressed() {
        UpdateActiveEffectsList();

        if (activeEffects.Count == 0) {
            SelectNoEffect();
        } else {
            currentActiveIndex = (currentActiveIndex + 1) % activeEffects.Count;
            SelectKnobState(activeEffects[currentActiveIndex]);
        }
    }

    void UpdateActiveEffectsList() {
        activeEffects.Clear();

        for (int i = 0; i < EFFECT_COUNT; i++) {
            if (effectEnabledStates[i]) {
                activeEffects.Add(i);
            }
        }

        if (currentEffectIndex != -1) {
            currentActiveIndex = activeEffects.IndexOf(currentEffectIndex);
            if (currentActiveIndex == -1) {
                currentEffectIndex = -1;
                currentActiveIndex = -1;
            }
        } else {
            currentActiveIndex = -1;
        }
    }

    void SelectNoEffect() {
        currentEffectIndex = -1;
        currentActiveIndex = -1;

        for (int i = 0; i < PARAM_COUNT; i++) {
            DisplayKnobController dK = displayKnobs[i];
            dK.changingState = true;
            dK.newStateAngle = dK.startAngle;
            dK.TurnOffAllLights();
            cachedKnobNames[i] = "Null";
            cachedKnobValues[i] = 0f;
        }

        UpdateDisplayText();
    }

    public void SelectKnobState(int effectIndex) {
        currentEffectIndex = effectIndex;

        for (int i = 0; i < PARAM_COUNT; i++) {
            DisplayKnobController dK = displayKnobs[i];
            dK.properties = knobStates[effectIndex, i];
            float targetAngle = dK.minRot + ((dK.maxRot - dK.minRot) * dK.properties.GetValue);
            dK.changingState = true;
            dK.newStateAngle = targetAngle;
            dK.UpdateClickLights();

            cachedKnobNames[i] = knobNames[effectIndex, i];
            cachedKnobValues[i] = knobStates[effectIndex, i].GetValue;
        }

        UpdateDisplayText();
    }

    void UpdateDisplayText() {
        if (currentEffectIndex == -1) {
            effectsDisplayController.UpdateDisplay("No Effect", 0, 0, cachedKnobNames, cachedKnobValues);
        } else {
            effectsDisplayController.UpdateDisplay(
                effectNames[currentEffectIndex],
                currentActiveIndex + 1,
                activeEffects.Count,
                cachedKnobNames,
                cachedKnobValues
            );
        }
    }

    void UpdateParameter(EffectType effectType, EffectParameter param, float value) {
        int effectIndex = (int)effectType;
        int paramIndex = (int)param;

        internalParameters[effectIndex, paramIndex] = value;
        knobStates[effectIndex, paramIndex] = new KnobProperties(value, knobStates[effectIndex, paramIndex].GetComputation, knobStates[effectIndex, paramIndex].GetClickValues, knobStates[effectIndex, paramIndex].GetClickThreshold);

        ComputeEffectOutput(effectType);

        DataManager.dataManager.data.currentState.displayKnobParameters[effectIndex * PARAM_COUNT + paramIndex] = value;

    }

    void SetLFOFrequency(EffectType effectType, float rate, float minFreq, float maxFreq) {
        float frequency = minFreq + (rate * (maxFreq - minFreq));
        int sampleRate = AudioSettings.outputSampleRate;

        switch (effectType) {
            case EffectType.Vibrato:
                audioController.vibratoLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Tremolo:
                audioController.tremoloLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Chorus:
                audioController.chorusLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Phaser:
                audioController.phaserLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Delay:
                audioController.delayLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Reverb:
                audioController.reverbLFO.setFrequency(frequency, sampleRate);
                break;
            case EffectType.Drive:
                audioController.driveLFO.setFrequency(frequency, sampleRate);
                break;
        }
    }

    void SetLFOWaveform(EffectType effectType, float shape) {
        AudioController.Wave waveform = shape < 0.25f ? audioController.SineWaveLUT :
                                        shape < 0.5f ? audioController.TriangleWaveLUT :
                                        shape < 0.75f ? audioController.SawWaveLUT :
                                        audioController.SquareWaveLUT;

        switch (effectType) {
            case EffectType.Vibrato:
                audioController.vibratoLFO.setType(waveform);
                break;
            case EffectType.Tremolo:
                audioController.tremoloLFO.setType(waveform);
                break;
            case EffectType.Chorus:
                audioController.chorusLFO.setType(waveform);
                break;
            case EffectType.Phaser:
                audioController.phaserLFO.setType(waveform);
                break;
            case EffectType.Delay:
                audioController.delayLFO.setType(waveform);
                break;
            case EffectType.Reverb:
                audioController.reverbLFO.setType(waveform);
                break;
            case EffectType.Drive:
                audioController.driveLFO.setType(waveform);
                break;
        }
    }

    void ComputeEffectOutput(EffectType effectType) {
        int effectIndex = (int)effectType;
        float p0 = internalParameters[effectIndex, 0];
        float p1 = internalParameters[effectIndex, 1];
        float p2 = internalParameters[effectIndex, 2];
        float p3 = internalParameters[effectIndex, 3];

        switch (effectType) {
            case EffectType.Compression: {
                    float threshold = 0.3f + (p0 * 0.6f);
                    float ratio = 1f + (p1 * 7f);
                    float makeupGain = 1f + (p3 * 2f);
                    float compressionAmount = (threshold * ratio * makeupGain * 0.1f) * p3;
                    effectsController.SetEffectValue(EffectsController.EffectParameter.Compression, compressionAmount);
                    break;
                }

            case EffectType.Saturation:
                effectsController.SetEffectValue(EffectsController.EffectParameter.Saturation, p0 * p3);
                effectsController.SetEffectValue(EffectsController.EffectParameter.SaturationCharacter, p1);
                effectsController.SetEffectValue(EffectsController.EffectParameter.SaturationWarmth, p2);
                break;

            case EffectType.Drive:
                if (p1 < 0.5f) {
                    effectsController.SetEffectValue(EffectsController.EffectParameter.Overdrive, p0 * p3 * (1f - p1 * 2f));
                    effectsController.SetEffectValue(EffectsController.EffectParameter.Distortion, 0f);
                } else {
                    effectsController.SetEffectValue(EffectsController.EffectParameter.Overdrive, 0f);
                    effectsController.SetEffectValue(EffectsController.EffectParameter.Distortion, p0 * p3 * ((p1 - 0.5f) * 2f));
                }
                effectsController.SetEffectValue(EffectsController.EffectParameter.DriveTone, p2);
                break;

            case EffectType.Chorus:
                SetLFOFrequency(EffectType.Chorus, p0, 0.1f, 3f);
                effectsController.SetEffectValue(EffectsController.EffectParameter.Chorus, p1 * p3);
                effectsController.SetEffectValue(EffectsController.EffectParameter.ChorusDelayTime, p2);
                break;

            case EffectType.Phaser:
                SetLFOFrequency(EffectType.Phaser, p0, 0.1f, 5f);
                effectsController.SetEffectValue(EffectsController.EffectParameter.Phaser, p1 * p3);
                effectsController.SetEffectValue(EffectsController.EffectParameter.PhaserFeedback, p2);
                break;

            case EffectType.Vibrato:
                SetLFOFrequency(EffectType.Vibrato, p0, 0.5f, 12f);
                SetLFOWaveform(EffectType.Vibrato, p2);
                effectsController.SetEffectValue(EffectsController.EffectParameter.Vibrato, p1 * p3);
                break;

            case EffectType.Tremolo:
                SetLFOFrequency(EffectType.Tremolo, p0, 0.5f, 12f);
                SetLFOWaveform(EffectType.Tremolo, p2);
                effectsController.SetEffectValue(EffectsController.EffectParameter.Tremolo, p1 * p3);
                break;

            case EffectType.Reverb:
                effectsController.SetEffectValue(EffectsController.EffectParameter.Reverb, p3);
                effectsController.SetEffectValue(EffectsController.EffectParameter.ReverbSize, p0);
                effectsController.SetEffectValue(EffectsController.EffectParameter.ReverbDecay, p1);
                effectsController.SetEffectValue(EffectsController.EffectParameter.ReverbDamping, p2);
                break;

            case EffectType.Delay:
                effectsController.SetEffectValue(EffectsController.EffectParameter.Delay, p3);
                effectsController.SetEffectValue(EffectsController.EffectParameter.DelayTime, p0);
                effectsController.SetEffectValue(EffectsController.EffectParameter.DelayFeedback, p1);
                effectsController.SetEffectValue(EffectsController.EffectParameter.DelayTone, p2);
                break;
        }
    }

    public void ActivateEffect(int effectIndex) {
        if (effectIndex < 0 || effectIndex >= EFFECT_COUNT || effectEnabledStates[effectIndex]) return;

        int previousActiveCount = activeEffects.Count;
        effectEnabledStates[effectIndex] = true;

        for (int param = 0; param < PARAM_COUNT; param++) {
            internalParameters[effectIndex, param] = knobStates[effectIndex, param].GetValue;
        }

        ComputeEffectOutput((EffectType)effectIndex);
        UpdateActiveEffectsList();

        if (previousActiveCount == 0 || currentEffectIndex == -1) {
            currentEffectIndex = effectIndex;
            currentActiveIndex = activeEffects.IndexOf(effectIndex);
            SelectKnobState(effectIndex);
        } else {
            effectsDisplayController.UpdateEffectNumbers(currentActiveIndex + 1, activeEffects.Count);
        }
    }

    public void DeactivateEffect(int effectIndex) {
        if (effectIndex < 0 || effectIndex >= EFFECT_COUNT || !effectEnabledStates[effectIndex]) return;

        effectEnabledStates[effectIndex] = false;

        for (int param = 0; param < PARAM_COUNT; param++) {
            internalParameters[effectIndex, param] = 0f;
        }

        ComputeEffectOutput((EffectType)effectIndex);
        UpdateActiveEffectsList();

        if (currentEffectIndex == effectIndex) {
            if (activeEffects.Count == 0) {
                SelectNoEffect();
            } else {
                currentActiveIndex = Mathf.Clamp(currentActiveIndex, 0, activeEffects.Count - 1);
                SelectKnobState(activeEffects[currentActiveIndex]);
            }
        } else {
            if (currentEffectIndex == -1) {
                if (activeEffects.Count > 0) {
                    currentActiveIndex = 0;
                    SelectKnobState(activeEffects[0]);
                } else {
                    SelectNoEffect();
                }
            } else {
                currentActiveIndex = activeEffects.IndexOf(currentEffectIndex);
                effectsDisplayController.UpdateEffectNumbers(currentActiveIndex + 1, activeEffects.Count);
            }
        }
    }

    public bool IsEffectEnabled(int effectIndex) {
        return effectIndex >= 0 && effectIndex < EFFECT_COUNT && effectEnabledStates[effectIndex];
    }

    public int[] GetEnabledEffects() {
        return activeEffects.ToArray();
    }

    public void UpdateDisplayParameter(EffectsController.EffectParameter param, float normalizedValue) {
        switch (param) {
            case EffectsController.EffectParameter.EQLow:
                eqDisplayController.UpdateLowShelfValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.EQLowMid:
                eqDisplayController.UpdateLowMidFilterValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.EQHighMid:
                eqDisplayController.UpdateHighMidFilterValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.EQHigh:
                eqDisplayController.UpdateHighShelfValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.EQResonance:
                eqDisplayController.UpdateResonanceValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Attack:
                envelopeDisplayController.UpdateAttackValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Decay:
                envelopeDisplayController.UpdateDecayValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Sustain:
                envelopeDisplayController.UpdateSustainValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Release:
                envelopeDisplayController.UpdateReleaseValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Volume:
                globalKnobsDisplayController.UpdateVolumeValue(normalizedValue);
                break;
            case EffectsController.EffectParameter.Mix:
                globalKnobsDisplayController.UpdateMixValue(normalizedValue);
                break;
        }
    }
}