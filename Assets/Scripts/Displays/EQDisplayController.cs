using TMPro;
using UnityEngine;

public class EQDisplayController : MonoBehaviour {
    TextMeshProUGUI lowShelfValue;
    TextMeshProUGUI lowMidFilterValue;
    TextMeshProUGUI highMidFilterValue;
    TextMeshProUGUI highShelfValue;
    TextMeshProUGUI resonanceValue;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "EQ Display");

        lowShelfValue = transform.Find("Low Shelf Value").GetComponent<TextMeshProUGUI>();
        lowMidFilterValue = transform.Find("Low Mid Filter Value").GetComponent<TextMeshProUGUI>();
        highMidFilterValue = transform.Find("High Mid Filter Value").GetComponent<TextMeshProUGUI>();
        highShelfValue = transform.Find("High Shelf Value").GetComponent<TextMeshProUGUI>();
        resonanceValue = transform.Find("Resonance Value").GetComponent<TextMeshProUGUI>();
    }

    void Start() {
        UpdateLowShelfValue(0.5f);
        UpdateLowMidFilterValue(0.5f);
        UpdateHighMidFilterValue(0.5f);
        UpdateHighShelfValue(0.5f);
        UpdateResonanceValue(0.5f);
    }

    public void UpdateLowShelfValue(float normalizedValue) {
        float gainDB = (normalizedValue - 0.5f) * 24f;
        lowShelfValue.text = $"{gainDB:+0.0;-0.0;+0.0} dB";
    }

    public void UpdateLowMidFilterValue(float normalizedValue) {
        float gainDB = (normalizedValue - 0.5f) * 24f;
        lowMidFilterValue.text = $"{gainDB:+0.0;-0.0;+0.0} dB";
    }

    public void UpdateHighMidFilterValue(float normalizedValue) {
        float gainDB = (normalizedValue - 0.5f) * 24f;
        highMidFilterValue.text = $"{gainDB:+0.0;-0.0;+0.0} dB";
    }

    public void UpdateHighShelfValue(float normalizedValue) {
        float gainDB = (normalizedValue - 0.5f) * 24f;
        highShelfValue.text = $"{gainDB:+0.0;-0.0;+0.0} dB";
    }

    public void UpdateResonanceValue(float normalizedValue) {
        float shelfQ = Mathf.Clamp(0.5f + (normalizedValue * 1.0f), 0.3f, 3.0f);
        float peakQ = Mathf.Clamp(0.5f + (normalizedValue * 4.5f), 0.3f, 10.0f);
        resonanceValue.text = $"{shelfQ:F2} | {peakQ:F2}";
    }
}
