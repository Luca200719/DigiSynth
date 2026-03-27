using TMPro;
using UnityEngine;

public class EffectsDisplayController : MonoBehaviour {
    TextMeshProUGUI effectName;
    TextMeshProUGUI effectIndex;
    TextMeshProUGUI totalActiveEffects;
    TextMeshProUGUI knobName_1;
    TextMeshProUGUI knobName_2;
    TextMeshProUGUI knobName_3;
    TextMeshProUGUI knobName_4;
    TextMeshProUGUI knobValue_1;
    TextMeshProUGUI knobValue_2;
    TextMeshProUGUI knobValue_3;
    TextMeshProUGUI knobValue_4;

    public int currentEffectIndex = -1;
    float[] currentKnobValues = new float[4];

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Effects Display");

        effectName = transform.Find("Effect Name").GetComponent<TextMeshProUGUI>();
        effectIndex = transform.Find("Effect Index").GetComponent<TextMeshProUGUI>();
        totalActiveEffects = transform.Find("Total Active Effects").GetComponent<TextMeshProUGUI>();

        knobName_1 = transform.Find("Knob Name 1").GetComponent<TextMeshProUGUI>();
        knobName_2 = transform.Find("Knob Name 2").GetComponent<TextMeshProUGUI>();
        knobName_3 = transform.Find("Knob Name 3").GetComponent<TextMeshProUGUI>();
        knobName_4 = transform.Find("Knob Name 4").GetComponent<TextMeshProUGUI>();

        knobValue_1 = transform.Find("Knob Value 1")?.GetComponent<TextMeshProUGUI>();
        knobValue_2 = transform.Find("Knob Value 2")?.GetComponent<TextMeshProUGUI>();
        knobValue_3 = transform.Find("Knob Value 3")?.GetComponent<TextMeshProUGUI>();
        knobValue_4 = transform.Find("Knob Value 4")?.GetComponent<TextMeshProUGUI>();
    }

    public void UpdateDisplay(string name, int index, int totalActive, string[] knobNames, float[] knobValues) {
        effectName.text = name.ToUpper();
        effectIndex.text = index.ToString();
        totalActiveEffects.text = totalActive.ToString();

        knobName_1.text = knobNames[0];
        knobName_2.text = knobNames[1];
        knobName_3.text = knobNames[2];
        knobName_4.text = knobNames[3];

        if (name == "No Effect") {
            currentEffectIndex = -1;
        } else {
            currentEffectIndex = (int)EffectsController.switchedEffects[name];
        }

        for (int i = 0; i < 4; i++) {
            currentKnobValues[i] = knobValues[i];
        }

        knobValue_1.text = FormatKnobValue(currentEffectIndex, 0, knobValues[0]);
        knobValue_2.text = FormatKnobValue(currentEffectIndex, 1, knobValues[1]);
        knobValue_3.text = FormatKnobValue(currentEffectIndex, 2, knobValues[2]);
        knobValue_4.text = FormatKnobValue(currentEffectIndex, 3, knobValues[3]);
    }

    public void UpdateKnobValue(int knobIndex, float value) {
        if (currentEffectIndex == -1 || knobIndex < 0 || knobIndex >= 4) return;

        currentKnobValues[knobIndex] = value;

        string formattedValue = FormatKnobValue(currentEffectIndex, knobIndex, value);

        switch (knobIndex) {
            case 0:
                knobValue_1.text = formattedValue;
                break;
            case 1:
                knobValue_2.text = formattedValue;
                break;
            case 2:
                knobValue_3.text = formattedValue;
                break;
            case 3:
                knobValue_4.text = formattedValue;
                break;
        }
    }

    public void UpdateEffectNumbers(int index, int totalActive) {
        effectIndex.text = index.ToString();
        totalActiveEffects.text = totalActive.ToString();
    }

    string FormatKnobValue(int effectIndex, int paramIndex, float value) {
        if (effectIndex == -1) return "--";

        switch (effectIndex) {
            case 0: // Compression
                switch (paramIndex) {
                    case 0: // Threshold
                        float threshold = 0.3f + (value * 0.6f);
                        return $"{(threshold * 100f):F0}%";
                    case 1: // Ratio
                        float ratio = 1f + (value * 7f);
                        return $"{ratio:F1}:1";
                    case 2: // Attack
                        float attack = 0.001f + (value * 0.02f);
                        return $"{(attack * 1000f):F1}ms";
                    case 3: // Output
                        float makeupGain = 1f + (value * 2f);
                        return $"{(makeupGain * 100f):F0}%";
                }
                break;

            case 1: // Saturation
                switch (paramIndex) {
                    case 0: // Drive
                        return $"{(value * 100f):F0}%";
                    case 1: // Character
                        if (value < 0.25f) return "Even";
                        else if (value < 0.5f) return "Warm";
                        else if (value < 0.75f) return "Bright";
                        else return "Harsh";
                    case 2: // Warmth
                        return $"{(value * 100f):F0}%";
                    case 3: // Output
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 2: // Drive
                switch (paramIndex) {
                    case 0: // Drive
                        return $"{(value * 100f):F0}%";
                    case 1: // Character
                        if (value < 0.25f) return "Clean";
                        else if (value < 0.5f) return "Overdive";
                        else if (value < 0.75f) return "Distortion";
                        else return "Fuzz";
                    case 2: // Tone
                        if (value < 0.4f) return "Dark";
                        else if (value < 0.6f) return "Mid";
                        else return "Bright";
                    case 3: // Output
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 3: // Chorus
                switch (paramIndex) {
                    case 0: // Rate
                        float chorusRate = 0.1f + (value * 2.9f);
                        return $"{chorusRate:F2}Hz";
                    case 1: // Depth
                        return $"{(value * 100f):F0}%";
                    case 2: // Delay
                        if (value < 0.25f) return "Short";
                        else if (value < 0.5f) return "Medium";
                        else if (value < 0.75f) return "Long";
                        else return "Max";
                    case 3: // Mix
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 4: // Phaser
                switch (paramIndex) {
                    case 0: // Rate
                        float phaserRate = 0.1f + (value * 4.9f);
                        return $"{phaserRate:F2}Hz";
                    case 1: // Depth
                        return $"{(value * 100f):F0}%";
                    case 2: // Feedback
                        if (value < 0.25f) return "Low";
                        else if (value < 0.5f) return "Medium";
                        else if (value < 0.75f) return "High";
                        else return "Max";
                    case 3: // Mix
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 5: // Vibrato
                switch (paramIndex) {
                    case 0: // Rate
                        float vibratoRate = 0.5f + (value * 11.5f);
                        return $"{vibratoRate:F1}Hz";
                    case 1: // Depth
                        return $"{(value * 100f):F0}%";
                    case 2: // Shape
                        if (value < 0.25f) return "Sine";
                        else if (value < 0.5f) return "Triangle";
                        else if (value < 0.75f) return "Saw";
                        else return "Square";
                    case 3: // Intensity
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 6: // Tremolo
                switch (paramIndex) {
                    case 0: // Rate
                        float tremoloRate = 0.5f + (value * 11.5f);
                        return $"{tremoloRate:F1}Hz";
                    case 1: // Depth
                        return $"{(value * 100f):F0}%";
                    case 2: // Shape
                        if (value < 0.25f) return "Sine";
                        else if (value < 0.5f) return "Triangle";
                        else if (value < 0.75f) return "Saw";
                        else return "Square";
                    case 3: // Intensity
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 7: // Reverb
                switch (paramIndex) {
                    case 0: // Size
                        if (value < 0.2f) return "Tiny";
                        else if (value < 0.4f) return "Small";
                        else if (value < 0.6f) return "Medium";
                        else if (value < 0.8f) return "Large";
                        else return "Hall";
                    case 1: // Decay
                        return $"{(value * 100f):F0}%";
                    case 2: // Damping
                        if (value < 0.25f) return "Bright";
                        else if (value < 0.5f) return "Medium";
                        else if (value < 0.75f) return "Warm";
                        else return "Dark";
                    case 3: // Mix
                        return $"{(value * 100f):F0}%";
                }
                break;

            case 8: // Delay
                switch (paramIndex) {
                    case 0: // Time
                        float delayMs = 50f + (value * 1950f);
                        if (delayMs < 100f) return $"{delayMs:F0}ms";
                        else if (delayMs < 1000f) return $"{delayMs:F0}ms";
                        else return $"{(delayMs / 1000f):F2}s";
                    case 1: // Feedback
                        if (value < 0.25f) return "Single";
                        else if (value < 0.5f) return "Echo";
                        else if (value < 0.75f) return "Repeat";
                        else return "Infinite";
                    case 2: // Tone
                        if (value < 0.4f) return "Dark";
                        else if (value < 0.6f) return "Mid";
                        else return "Bright";
                    case 3: // Mix
                        return $"{(value * 100f):F0}%";
                }
                break;
        }

        return "--";
    }
}