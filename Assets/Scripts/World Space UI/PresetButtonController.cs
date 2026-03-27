using UnityEngine;
using UnityEngine.InputSystem;

public class PresetButtonController : MonoBehaviour {
    Animator _anim;
    Mouse _mouse;
    bool isHeld = false;

    static int presetID;

    public enum ButtonType {
        LeftArrow = 0,
        RightArrow = 1,
        Save = 2,
        Load = 3
    }

    public ButtonType buttonType;

    static readonly int animIn = Animator.StringToHash("Button_I");
    static readonly int animOut = Animator.StringToHash("Button_O");

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Preset Button");
        _anim = GetComponent<Animator>();
        _mouse = Mouse.current;

        presetID = DataManager.dataManager.data.presetID;
    }

    void Update() {
        if (isHeld && _mouse.leftButton.wasReleasedThisFrame) {
            _anim.CrossFade(animOut, 0f);
            isHeld = false;
            PresetDisplayController.displayController.letGo = true;
        }
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            Press();
        }
    }

    void Press() {
        _anim.CrossFade(animIn, 0f);
        isHeld = true;

        switch (buttonType) {
            case ButtonType.LeftArrow:
                presetID = (presetID + 7) % 8;
                DataManager.dataManager.data.presetID = presetID;
                PresetDisplayController.displayController.SetID(presetID + 1);

                DataManager.dataManager.data.isDirty = true;
                PresetDisplayController.displayController.SetDirty(true);
                break;
            case ButtonType.RightArrow:
                presetID = (presetID + 1) % 8;
                DataManager.dataManager.data.presetID = presetID;
                PresetDisplayController.displayController.SetID(presetID + 1);

                DataManager.dataManager.data.isDirty = true;
                PresetDisplayController.displayController.SetDirty(true);
                break;
            case ButtonType.Save:
                PresetDisplayController.displayController.Save();
                break;
            case ButtonType.Load:
                PresetDisplayController.displayController.Load();
                break;
        }
    }
}