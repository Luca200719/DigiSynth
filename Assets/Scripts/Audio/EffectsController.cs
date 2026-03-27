using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectsController : MonoBehaviour {
    public enum SwitchedEffect {
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
        Compression = 0,
        Saturation = 1,
        Overdrive = 2,
        Distortion = 3,
        Drive = 4,
        Chorus = 5,
        Phaser = 6,
        Vibrato = 7,
        Tremolo = 8,
        Reverb = 9,
        Delay = 10,

        Volume = 11,
        Pitch = 12,
        Detune = 13,
        Mix = 14,

        EQLow = 15,
        EQLowMid = 16,
        EQHighMid = 17,
        EQHigh = 18,
        EQResonance = 19,

        Attack = 20,
        Decay = 21,
        Sustain = 22,
        Release = 23,

        SaturationCharacter = 24,
        SaturationWarmth = 25,
        DriveTone = 26,
        ChorusDelayTime = 27,
        PhaserFeedback = 28,
        ReverbSize = 29,
        ReverbDecay = 30,
        ReverbDamping = 31,
        DelayTime = 32,
        DelayFeedback = 33,
        DelayTone = 34
    }

    public static readonly Dictionary<string, SwitchedEffect> switchedEffects = new Dictionary<string, SwitchedEffect> {
        { "Compression", SwitchedEffect.Compression },
        { "Saturation", SwitchedEffect.Saturation },
        { "Drive", SwitchedEffect.Drive },
        { "Chorus", SwitchedEffect.Chorus },
        { "Phaser", SwitchedEffect.Phaser },
        { "Vibrato", SwitchedEffect.Vibrato },
        { "Tremolo", SwitchedEffect.Tremolo },
        { "Reverb", SwitchedEffect.Reverb },
        { "Delay", SwitchedEffect.Delay }
    };

    public static readonly Dictionary<string, EffectParameter> knobEffects = new Dictionary<string, EffectParameter> {
        { "Compression", EffectParameter.Compression },
        { "Saturation", EffectParameter.Saturation },
        { "Overdrive", EffectParameter.Overdrive },
        { "Distortion", EffectParameter.Distortion },
        { "Drive", EffectParameter.Drive },
        { "Chorus", EffectParameter.Chorus },
        { "Phaser", EffectParameter.Phaser },
        { "Vibrato", EffectParameter.Vibrato },
        { "Tremolo", EffectParameter.Tremolo },
        { "Reverb", EffectParameter.Reverb },
        { "Delay", EffectParameter.Delay },

        { "Volume", EffectParameter.Volume },
        { "Pitch", EffectParameter.Pitch },
        { "Detune", EffectParameter.Detune },
        { "Mix", EffectParameter.Mix },

        { "EQLow", EffectParameter.EQLow },
        { "EQLowMid", EffectParameter.EQLowMid },
        { "EQHighMid", EffectParameter.EQHighMid },
        { "EQHigh", EffectParameter.EQHigh },
        { "EQResonance", EffectParameter.EQResonance },

        { "Attack", EffectParameter.Attack },
        { "Decay", EffectParameter.Decay },
        { "Sustain", EffectParameter.Sustain },
        { "Release", EffectParameter.Release },

        { "SaturationCharacter", EffectParameter.SaturationCharacter },
        { "SaturationWarmth", EffectParameter.SaturationWarmth },
        { "DriveTone", EffectParameter.DriveTone },
        { "ChorusDelayTime", EffectParameter.ChorusDelayTime },
        { "PhaserFeedback", EffectParameter.PhaserFeedback },
        { "ReverbSize", EffectParameter.ReverbSize },
        { "ReverbDecay", EffectParameter.ReverbDecay },
        { "ReverbDamping", EffectParameter.ReverbDamping },
        { "DelayTime", EffectParameter.DelayTime },
        { "DelayFeedback", EffectParameter.DelayFeedback },
        { "DelayTone", EffectParameter.DelayTone }
    };

    public static readonly Dictionary<string, int> knobNames = new Dictionary<string, int> {
        { "Volume", 0 },
        { "Pitch", 1 },
        { "Detune", 2 },
        { "Mix", 3 },

        { "EQLow", 4 },
        { "EQLowMid", 5 },
        { "EQHighMid", 6 },
        { "EQHigh", 7 },
        { "EQResonance", 8 },

        { "Attack", 9 },
        { "Decay", 10 },
        { "Sustain", 11 },
        { "Release", 12 },

        { "DSP_1", 13 },
        { "DSP_2", 14 },
        { "DSP_3", 15 },
        { "DSP_4", 16 }
    };

    public static readonly Dictionary<string, int> switchNames = new Dictionary<string, int> {
        { "Compression", 0 },
        { "Saturation", 1 },
        { "Drive", 2 },
        { "Chorus", 3 },
        { "Phaser", 4 },
        { "Vibrato", 5 },
        { "Tremolo", 6 },
        { "Reverb", 7 },
        { "Delay", 8 }
    };

    public KnobController[] knobs;
    public SwitchController[] switches;

    public float[] valueCaps;
    float[] previousValueCaps;
    public float[] effectValues;

    bool[] switchStates;
    float[] knobAngleCache;

    float lastUpdateTime = 0f;
    const float UPDATE_INTERVAL = 0.0083f;
    const float SMOOTHING_FACTOR = 0.5f;
    const float CHANGE_THRESHOLD = 0.0005f;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Effects");

        knobs = new KnobController[knobNames.Count];
        switches = new SwitchController[switchNames.Count];
        valueCaps = new float[Enum.GetValues(typeof(EffectParameter)).Length];
        previousValueCaps = new float[Enum.GetValues(typeof(EffectParameter)).Length];
        effectValues = new float[Enum.GetValues(typeof(EffectParameter)).Length];

        switchStates = new bool[switchNames.Count];
        knobAngleCache = new float[knobNames.Count];
    }

    void Start() {
        foreach (GameObject k in ObjectRegistry.registry.GetObjectList("Knob")) {
            if (knobNames.ContainsKey(k.transform.parent.name)) {
                knobs[knobNames[k.transform.parent.name]] = k.GetComponent<KnobController>();
            }
        }

        foreach (GameObject s in ObjectRegistry.registry.GetObjectList("Switch")) {
            if (switchNames.ContainsKey(s.transform.parent.name)) {
                switches[switchNames[s.transform.parent.name]] = s.GetComponent<SwitchController>();
            }
        }

        for (int i = 0; i < valueCaps.Length; i++) {
            previousValueCaps[i] = valueCaps[i];
            effectValues[i] = 0f;
        }

        if (DataManager.dataManager.data.firstTime) {
            foreach (var item in knobNames) {
                int knobIndex = item.Value;
                string knobName = item.Key;
                if (knobs[knobIndex] == null) continue;
                if (!knobEffects.ContainsKey(knobName)) continue;
                KnobController knob = knobs[knobIndex];
                float val = (knob.currentAngle - knob.minRot) / (knob.maxRot - knob.minRot);
                EffectParameter effectParam = knobEffects[knobName];
                int effectIndex = (int)effectParam;
                valueCaps[effectIndex] = val;
                knobAngleCache[knobIndex] = knob.currentAngle;
            }
            Array.Copy(valueCaps, previousValueCaps, valueCaps.Length);
        }
        else {
            LoadFromCurrentState();
        }
    }

    public void SaveToCurrentState() {
        Array.Copy(valueCaps, DataManager.dataManager.data.currentState.valueCaps, valueCaps.Length);

        for (int i = 0; i < switches.Length; i++) {
            DataManager.dataManager.data.currentState.switchStates[i] = switches[i].isActive;
        }
    }

    public void LoadFromCurrentState() {
        Array.Copy(DataManager.dataManager.data.currentState.valueCaps, valueCaps, valueCaps.Length);

        foreach (var kvp in knobNames) {
            int knobIndex = kvp.Value;
            if (knobs[knobIndex] == null) continue;
            if (!knobEffects.ContainsKey(kvp.Key)) continue;

            KnobController knob = knobs[knobIndex];
            int effectIndex = (int)knobEffects[kvp.Key];
            float normalizedValue = valueCaps[effectIndex];
            float angle = knob.minRot + normalizedValue * (knob.maxRot - knob.minRot);
            knob.LoadAngle(angle);
        }

        for (int i = 0; i < switches.Length; i++) {
            switches[i].SetState(DataManager.dataManager.data.currentState.switchStates[i]);
        }
    }

    void Update() {
        if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
        lastUpdateTime = Time.time;

        for (int i = 0; i < switches.Length; i++) {
            switchStates[i] = switches[i].isActive;
        }

        foreach (var kvp in knobNames) {
            int knobIndex = kvp.Value;
            if (knobs[knobIndex] == null) continue;

            KnobController knob = knobs[knobIndex];
            float currentAngle = knob.currentAngle;

            if (Mathf.Abs(currentAngle - knobAngleCache[knobIndex]) < 0.001f)
                continue;

            knobAngleCache[knobIndex] = currentAngle;

            if (!knobEffects.ContainsKey(kvp.Key)) continue;

            float newKnobValue = (currentAngle - knob.minRot) / (knob.maxRot - knob.minRot);
            EffectParameter effectParam = knobEffects[kvp.Key];
            int effectIndex = (int)effectParam;

            float smoothedValue = previousValueCaps[effectIndex] * SMOOTHING_FACTOR + newKnobValue * (1f - SMOOTHING_FACTOR);

            if (Mathf.Abs(smoothedValue - valueCaps[effectIndex]) > CHANGE_THRESHOLD) {
                valueCaps[effectIndex] = smoothedValue;
                previousValueCaps[effectIndex] = smoothedValue;
            }
        }
    }

    public bool IsEffectActive(string effectName) {
        SwitchedEffect effect = switchedEffects[effectName];
        return switchStates[(int)effect];
    }

    public float GetEffectValue(EffectParameter effectParam) => valueCaps[(int)effectParam];

    public void SetEffectValue(EffectParameter effectParam, float value) {
        valueCaps[(int)effectParam] = value;
    }

    public float GetLFOValue(EffectParameter effectParam) => effectValues[(int)effectParam];

    public void SetLFOValue(EffectParameter effectParam, float value) {
        effectValues[(int)effectParam] = value;
    }
}