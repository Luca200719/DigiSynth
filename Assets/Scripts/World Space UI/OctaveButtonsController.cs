using UnityEngine;
using System.Collections;

public class OctaveButtonsController : MonoBehaviour {
    KeyboardController keyboardController;

    GameObject octaveUpButton;
    GameObject octaveDownButton;
    Animator _anim;

    bool isUpAnimating = false;
    bool isDownAnimating = false;

    static readonly int upImpressionHash = Animator.StringToHash("OctaveUp_I");
    static readonly int upDepressionHash = Animator.StringToHash("OctaveUp_O");
    static readonly int downImpressionHash = Animator.StringToHash("OctaveDown_I");
    static readonly int downDepressionHash = Animator.StringToHash("OctaveDown_O");

    void Start() {
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();
        
        _anim = GetComponent<Animator>();

        octaveUpButton = transform.Find("Octave Up").gameObject;
        octaveDownButton = transform.Find("Octave Down").gameObject;
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == octaveUpButton.transform) {
            if (!isUpAnimating && keyboardController.CanOctaveUp()) {
                StartCoroutine(HandleUpButtonPress());
            }
        } else if (hit.transform == octaveDownButton.transform) {
            if (!isDownAnimating && keyboardController.CanOctaveDown()) {
                StartCoroutine(HandleDownButtonPress());
            }
        }
    }

    IEnumerator HandleUpButtonPress() {
        DataManager.dataManager.data.isDirty = true;
        PresetDisplayController.displayController.SetDirty(true);

        isUpAnimating = true;

        _anim.CrossFade(upImpressionHash, 0f);

        yield return new WaitForSeconds(0.1f);

        keyboardController.OctaveUp();

        yield return new WaitForSeconds(0.05f);

        _anim.CrossFade(upDepressionHash, 0f);

        yield return new WaitForSeconds(0.1f);

        isUpAnimating = false;
    }

    IEnumerator HandleDownButtonPress() {
        DataManager.dataManager.data.isDirty = true;
        PresetDisplayController.displayController.SetDirty(true);

        isDownAnimating = true;

        _anim.CrossFade(downImpressionHash, 0f);

        yield return new WaitForSeconds(0.1f);

        keyboardController.OctaveDown();

        yield return new WaitForSeconds(0.05f);

        _anim.CrossFade(downDepressionHash, 0f);

        yield return new WaitForSeconds(0.1f);

        isDownAnimating = false;
    }
}