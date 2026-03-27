using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuitMenuController : MonoBehaviour {
    KeyboardController keyboardController;

    CanvasGroup quitMenu;
    const float FADE_DURATION = 0.3f;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Quit Menu");
    }

    void Start() {
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();
        quitMenu = GetComponent<CanvasGroup>();
    }

    public void CloseQuitMenu() {
        keyboardController.ToggleQuitMenu();
        StartCoroutine(FadeOutQuitMenu());
    }

    public void Quit() {
        Application.Quit();
    }

    IEnumerator FadeOutQuitMenu() {
        quitMenu.alpha = 1f;
        quitMenu.interactable = false;

        float elapsedTime = 0f;

        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / FADE_DURATION;
            t = t == 0f ? 0f : Mathf.Pow(2f, -10f * t);

            quitMenu.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        quitMenu.alpha = 0f;
        quitMenu.blocksRaycasts = false;
    }
}