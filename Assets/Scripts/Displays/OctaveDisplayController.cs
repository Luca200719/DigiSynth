using TMPro;
using UnityEngine;

public class OctaveDisplayController : MonoBehaviour {
    TextMeshProUGUI upperBoundValue;
    TextMeshProUGUI lowerBoundValue;
    static readonly (string lowerBound, string upperBound)[] octaveValues = new (string, string)[] {
        ("A0", "C2"),
        ("C1", "C3"),
        ("C2", "C4"),
        ("C3", "C5"),
        ("C4", "C6"),
        ("C5", "C7"),
        ("C6", "C8")
    };


    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Octave Display");

        upperBoundValue = transform.Find("Upper Bound Value").GetComponent<TextMeshProUGUI>();
        lowerBoundValue = transform.Find("Lower Bound Value").GetComponent<TextMeshProUGUI>();
    }

    void Start() {
        UpdateOctaveDisplay(3);
    }

    public void UpdateOctaveDisplay(int octave) {
        upperBoundValue.text = octaveValues[octave].Item2;
        lowerBoundValue.text = octaveValues[octave].Item1;
    }
}
