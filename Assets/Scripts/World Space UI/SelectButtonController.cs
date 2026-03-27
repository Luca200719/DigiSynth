using UnityEngine;
using System.Collections;

public class SelectButtonController : MonoBehaviour {
    DisplayController displayController;
    Animator _anim;

    bool isAnimating = false;

    static readonly int pressHash = Animator.StringToHash("Toggle_I");
    static readonly int depressionHash = Animator.StringToHash("Toggle_O");

    void Start() {
        displayController = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();

        _anim = GetComponent<Animator>();
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            if (!isAnimating) {
                StartCoroutine(HandleButtonPress());
            }
        }
    }

    IEnumerator HandleButtonPress() {
        isAnimating = true;

        _anim.CrossFade(pressHash, 0f);

        yield return new WaitForSeconds(0.1f);

        displayController.OnSelectButtonPressed();

        yield return new WaitForSeconds(0.05f);

        _anim.CrossFade(depressionHash, 0f);

        yield return new WaitForSeconds(0.1f);

        isAnimating = false;
    }
}