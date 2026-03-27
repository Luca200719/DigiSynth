using UnityEngine;

public class DisplayPanel : MonoBehaviour {
    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Display Panel");
    }
}
