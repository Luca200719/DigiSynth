using TMPro;
using UnityEngine;

public class EnvelopeDisplayController : MonoBehaviour {
    TextMeshProUGUI attackValue;
    TextMeshProUGUI decayValue;
    TextMeshProUGUI sustainValue;
    TextMeshProUGUI releaseValue;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Envelope Display");

        attackValue = transform.Find("Attack Value").GetComponent<TextMeshProUGUI>();
        decayValue = transform.Find("Decay Value").GetComponent<TextMeshProUGUI>();
        sustainValue = transform.Find("Sustain Value").GetComponent<TextMeshProUGUI>();
        releaseValue = transform.Find("Release Value").GetComponent<TextMeshProUGUI>();
    }

    void Start() {
        UpdateAttackValue(0.5f);
        UpdateDecayValue(0.5f);
        UpdateSustainValue(0.5f);
        UpdateReleaseValue(0.5f);
    }

    public void UpdateAttackValue(float normalizedValue) {
        float seconds = Mathf.Pow(10, normalizedValue * 3 - 3);
        attackValue.text = seconds < 1 ? $"{seconds * 1000:F0} ms" : $"{seconds:F2} s";
    }

    public void UpdateDecayValue(float normalizedValue) {
        float seconds = Mathf.Pow(10, normalizedValue * 3 - 2);
        decayValue.text = seconds < 1 ? $"{seconds * 1000:F0} ms" : $"{seconds:F2} s";
    }

    public void UpdateSustainValue(float normalizedValue) {
        sustainValue.text = $"{normalizedValue * 100:F0}%";
    }

    public void UpdateReleaseValue(float normalizedValue) {
        float seconds = Mathf.Pow(10, normalizedValue * 4 - 3);
        releaseValue.text = seconds < 1 ? $"{seconds * 1000:F0} ms" : $"{seconds:F2} s";
    }
}
