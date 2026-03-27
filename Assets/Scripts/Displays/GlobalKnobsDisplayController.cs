using TMPro;
using UnityEngine;

public class GlobalKnobsDisplayController : MonoBehaviour {
    TextMeshProUGUI volumeValue;
    TextMeshProUGUI mixValue;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Global Knobs Display");

        volumeValue = transform.Find("Volume Value").GetComponent<TextMeshProUGUI>();
        mixValue = transform.Find("Mix Value").GetComponent<TextMeshProUGUI>();
    }

    void Start() {
        UpdateVolumeValue(1f);
        UpdateMixValue(0.5f);
    }

    public void UpdateVolumeValue(float normalizedValue) {
        volumeValue.text = $"{normalizedValue * 100:F0}%";
    }

    public void UpdateMixValue(float normalizedValue) {
        mixValue.text = $"{normalizedValue * 100:F0}%";
    }
}
