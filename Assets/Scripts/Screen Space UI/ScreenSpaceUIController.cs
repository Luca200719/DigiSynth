using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScreenSpaceUIController : MonoBehaviour {
    CameraAspectExpander cameraExpander;
    KeyboardController keyboardController;

    public MeshRenderer upperUIPassepartout;
    public MeshRenderer rightUIPassepartout;

    GameObject upperUIButton;
    GameObject rightUIButton;
    GameObject exitButton;

    GameObject offButton;
    GameObject helpButton;

    CanvasGroup helpGuide;
    CanvasGroup quitMenu;

    const float FADE_DURATION = 0.3f;
    CanvasGroup transitionBlocker;
    bool isTransitioning = false;

    public Collider upperUIRestrictionCollider;
    public Collider rightUIRestrictionCollider;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Screen Space UI Controller");
    }

    void Start() {
        cameraExpander = ObjectRegistry.registry.GetObjectList("Camera Expander")[0].GetComponent<CameraAspectExpander>();
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();

        upperUIButton = transform.GetChild(0).gameObject;
        rightUIButton = transform.GetChild(1).gameObject;
        exitButton = transform.GetChild(2).gameObject;

        offButton = transform.parent.GetChild(2).GetChild(0).gameObject;
        helpButton = transform.parent.GetChild(1).GetChild(0).gameObject;

        helpGuide = transform.parent.GetChild(3).GetComponent<CanvasGroup>();
        quitMenu = transform.parent.GetChild(4).GetComponent<CanvasGroup>();

        SetupTransitionBlocker();
        UpdateButtonVisibility(cameraExpander.currentLocation);
    }

    void SetupTransitionBlocker() {
        GameObject blockerObject = new GameObject("TransitionBlocker");
        blockerObject.transform.SetParent(transform, false);
        blockerObject.transform.SetAsLastSibling();

        RectTransform rectTransform = blockerObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        transitionBlocker = blockerObject.AddComponent<CanvasGroup>();
        transitionBlocker.alpha = 0f;
        transitionBlocker.interactable = false;
        transitionBlocker.blocksRaycasts = false;

        Image blockerImage = blockerObject.AddComponent<Image>();
        blockerImage.color = new Color(1, 1, 1, 0);
    }

    public void GoToKeyboard() {
        if (isTransitioning) return;
        cameraExpander.SetCameraLocation(CameraAspectExpander.CameraLocation.Keyboard);
        UpdateButtonVisibility(CameraAspectExpander.CameraLocation.Keyboard);
    }

    public void GoToUpperUI() {
        if (isTransitioning) return;
        cameraExpander.SetCameraLocation(CameraAspectExpander.CameraLocation.UpperUI);
        UpdateButtonVisibility(CameraAspectExpander.CameraLocation.UpperUI);
    }

    public void GoToRightUI() {
        if (isTransitioning) return;
        cameraExpander.SetCameraLocation(CameraAspectExpander.CameraLocation.RightUI);
        UpdateButtonVisibility(CameraAspectExpander.CameraLocation.RightUI);
    }

    public void GoToOrbit() {
        cameraExpander.SetCameraLocation(CameraAspectExpander.CameraLocation.Orbit);
        UpdateButtonVisibility(CameraAspectExpander.CameraLocation.Orbit);
    }

    public void ExitOrbit() {
        StopAllCoroutines();

        cameraExpander.EnableTransitionsFromOrbit();

        cameraExpander.SetCameraLocation(CameraAspectExpander.CameraLocation.Keyboard);
        UpdateButtonVisibility(CameraAspectExpander.CameraLocation.Keyboard);
    }

    public void OpenHelpGuide() {
        keyboardController.ToggleHelpGuide();
        StartCoroutine(FadeInHelpGuide());
    }

    public void OpenQuitMenu() {
        keyboardController.ToggleQuitMenu();
        StartCoroutine(FadeInQuitMenu());
    }

    public void Quit() {
        keyboardController.ToggleQuitMenu();
        StartCoroutine(FadeInQuitMenu());
    }

    void UpdateButtonVisibility(CameraAspectExpander.CameraLocation location) {
        switch (location) {
            case CameraAspectExpander.CameraLocation.Keyboard:
                SetIconAlpha(upperUIButton, true);
                SetIconAlpha(rightUIButton, true);
                SetIconAlpha(exitButton, false);

                SetIconAlpha(offButton, true);
                SetIconAlpha(helpButton, true);

                if (upperUIPassepartout != null) SetPassepartoutAlpha(upperUIPassepartout, false);
                if (rightUIPassepartout != null) SetPassepartoutAlpha(rightUIPassepartout, false);

                upperUIRestrictionCollider.enabled = false;
                rightUIRestrictionCollider.enabled = false;
                break;

            case CameraAspectExpander.CameraLocation.UpperUI:
                SetIconAlpha(upperUIButton, false);
                SetIconAlpha(rightUIButton, true);
                SetIconAlpha(exitButton, true);
                upperUIRestrictionCollider.enabled = true;
                rightUIRestrictionCollider.enabled = false;
                if (upperUIPassepartout != null) SetPassepartoutAlpha(upperUIPassepartout, true);
                if (rightUIPassepartout != null) SetPassepartoutAlpha(rightUIPassepartout, false);
                break;

            case CameraAspectExpander.CameraLocation.RightUI:
                SetIconAlpha(upperUIButton, true);
                SetIconAlpha(rightUIButton, false);
                SetIconAlpha(exitButton, true);
                upperUIRestrictionCollider.enabled = false;
                rightUIRestrictionCollider.enabled = true;
                if (upperUIPassepartout != null) SetPassepartoutAlpha(upperUIPassepartout, false);
                if (rightUIPassepartout != null) SetPassepartoutAlpha(rightUIPassepartout, true);
                break;
            case CameraAspectExpander.CameraLocation.Orbit:
                SetIconAlpha(upperUIButton, false);
                SetIconAlpha(rightUIButton, false);

                SetIconAlpha(offButton, false);
                SetIconAlpha(helpButton, false);
                break;
        }
    }

    void SetIconAlpha(GameObject icon, bool visible) {
        CanvasGroup iconCanvasGroup = icon.GetComponent<CanvasGroup>();
        iconCanvasGroup.blocksRaycasts = visible;
        StartCoroutine(FadeIcon(iconCanvasGroup, visible ? 1f : 0f));
    }

    void SetPassepartoutAlpha(MeshRenderer passepartout, bool visible) {
        StartCoroutine(FadePassepartout(passepartout, visible ? 0.70f : 0f));
    }

    IEnumerator FadeIcon(CanvasGroup iconCanvasGroup, float targetAlpha) {
        isTransitioning = true;
        transitionBlocker.blocksRaycasts = true;

        float startAlpha = iconCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;
            iconCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / FADE_DURATION);
            yield return null;
        }

        iconCanvasGroup.alpha = targetAlpha;
        transitionBlocker.blocksRaycasts = false;
        isTransitioning = false;
    }

    IEnumerator FadePassepartout(MeshRenderer passepartoutRenderer, float targetAlpha) {
        isTransitioning = true;
        transitionBlocker.blocksRaycasts = true;

        Material mat = passepartoutRenderer.material;
        Color col = passepartoutRenderer.material.color;

        float startAlpha = col.a;
        float elapsedTime = 0f;

        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;
            col.a = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / FADE_DURATION);
            mat.color = col;
            yield return null;
        }

        col.a = targetAlpha;
        mat.color = col;

        transitionBlocker.blocksRaycasts = false;
        isTransitioning = false;
    }

    IEnumerator FadeInHelpGuide() {
        isTransitioning = true;
        helpGuide.blocksRaycasts = true;
        helpGuide.alpha = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / FADE_DURATION;
            t = t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

            helpGuide.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        helpGuide.alpha = 1f;
        helpGuide.interactable = true;
        isTransitioning = false;
    }

    IEnumerator FadeInQuitMenu() {
        isTransitioning = true;
        quitMenu.blocksRaycasts = true;
        quitMenu.alpha = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / FADE_DURATION;
            t = t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

            quitMenu.alpha = Mathf.Clamp01(t);
            yield return null;
        }

        quitMenu.alpha = 1f;
        quitMenu.interactable = true;
        isTransitioning = false;
    }
}