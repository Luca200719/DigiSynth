using UnityEngine;

public class SwitchController : MonoBehaviour {
    public bool isActive;
    public bool justActivated;

    public bool displaySwitch;
    public int effectIndex = -1;

    DisplayController displayController;
    Animator _anim;

    static readonly int animInHash = Animator.StringToHash("Switch_I");
    static readonly int animOutHash = Animator.StringToHash("Switch_O");


    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Switch");

        _anim = GetComponent<Animator>();

        string parentName = transform.parent.name;
        if (EffectsController.switchNames.ContainsKey(parentName)) {
            displaySwitch = true;
            effectIndex = EffectsController.switchNames[parentName];
        }
    }

    void Start() {
        displayController = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();

        if (!isActive) {
            _anim.CrossFade(animOutHash, 0f);
        }
    }

    public void SetState(bool state) {
        isActive = state;
        if (state) {
            _anim.CrossFade(animInHash, 0f);
            if (displaySwitch && effectIndex >= 0)
                displayController.ActivateEffect(effectIndex);
        }
        else {
            _anim.CrossFade(animOutHash, 0f);
            if (displaySwitch && effectIndex >= 0)
                displayController.DeactivateEffect(effectIndex);
        }
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            ToggleSwitch();
        }
    }

    private void ToggleSwitch() {
        if (!isActive) {
            _anim.CrossFade(animInHash, 0f);
            isActive = true;
            justActivated = true;

            if (displaySwitch && effectIndex >= 0) {
                displayController.ActivateEffect(effectIndex);
            }
        } else {
            _anim.CrossFade(animOutHash, 0f);
            isActive = false;
            justActivated = false;

            if (displaySwitch && effectIndex >= 0) {
                displayController.DeactivateEffect(effectIndex);
            }
        }
    }
}