using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HelpGuideController : MonoBehaviour {
    KeyboardController keyboardController;

    public float transitionDuration = 0.4f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public Sprite page1Sprite;
    public Sprite page2Sprite;
    public Image pageImage;

    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button closeButton;

    public Image page1IndicatorImage;
    public Image page2IndicatorImage;
    Color activeDotColor = new Color(0.5882353f, 0.482353f, 0.4235294f);
    Color inactiveDotColor = new Color(0.2901961f, 0.2196079f, 0.1882353f);
    const float indicatorTransitionSpeed = 0.2f;

    int currentPageIndex = 0;
    bool isTransitioning = false;
    RectTransform pageRectTransform;
    Vector2 centerPosition;

    CanvasGroup helpGuide;
    const float FADE_DURATION = 0.3f;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Help Guide");
    }

    void Start() {
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();

        pageRectTransform = pageImage.transform.parent.GetComponent<RectTransform>();
        centerPosition = pageRectTransform.anchoredPosition;

        helpGuide = transform.GetComponent<CanvasGroup>();

        UpdatePage(0, false);
    }

    public void NextPage() {
        if (isTransitioning) return;

        int nextIndex = (currentPageIndex + 1) % 2;
        StartCoroutine(TransitionToPage(nextIndex, true));
    }

    public void PreviousPage() {
        if (isTransitioning) return;

        int prevIndex = (currentPageIndex - 1 + 2) % 2;
        StartCoroutine(TransitionToPage(prevIndex, false));
    }

    public void CloseHelpGuide() {
        keyboardController.ToggleHelpGuide();
        StartCoroutine(FadeOutHelpGuide());
    }

    IEnumerator TransitionToPage(int newPageIndex, bool moveRight) {
        isTransitioning = true;

        leftArrowButton.interactable = false;
        rightArrowButton.interactable = false;
        closeButton.interactable = false;

        float direction = moveRight ? -1f : 1f;
        Vector2 startPos = centerPosition;
        Vector2 offScreenPos = centerPosition + new Vector2(Screen.width * direction, 0);

        float elapsed = 0f;
        while (elapsed < transitionDuration / 2f) {
            elapsed += Time.deltaTime;
            float t = animationCurve.Evaluate(elapsed / (transitionDuration / 2f));
            pageRectTransform.anchoredPosition = Vector2.Lerp(startPos, offScreenPos, t);

            Color color = pageImage.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            pageImage.color = color;

            yield return null;
        }

        pageImage.sprite = (newPageIndex == 0) ? page1Sprite : page2Sprite;
        currentPageIndex = newPageIndex;
        UpdatePageIndicators();

        Vector2 oppositeOffScreen = centerPosition + new Vector2(Screen.width * direction * -1f, 0);
        pageRectTransform.anchoredPosition = oppositeOffScreen;

        elapsed = 0f;
        while (elapsed < transitionDuration / 2f) {
            elapsed += Time.deltaTime;
            float t = animationCurve.Evaluate(elapsed / (transitionDuration / 2f));
            pageRectTransform.anchoredPosition = Vector2.Lerp(oppositeOffScreen, startPos, t);

            Color color = pageImage.color;
            color.a = Mathf.Lerp(0f, 1f, t);
            pageImage.color = color;

            yield return null;
        }

        pageRectTransform.anchoredPosition = centerPosition;
        Color finalColor = pageImage.color;
        finalColor.a = 1f;
        pageImage.color = finalColor;

        leftArrowButton.interactable = true;
        rightArrowButton.interactable = true;
        closeButton.interactable = true;

        isTransitioning = false;
    }

    void UpdatePage(int pageIndex, bool animate) {
        if (animate) {
            bool moveRight = (pageIndex == 1);
            StartCoroutine(TransitionToPage(pageIndex, moveRight));
        } else {
            pageImage.sprite = (pageIndex == 0) ? page1Sprite : page2Sprite;
            currentPageIndex = pageIndex;
            UpdatePageIndicators();
        }
    }

    void UpdatePageIndicators() {
        if (currentPageIndex == 0) {
            StartCoroutine(AnimateDot(page1IndicatorImage, activeDotColor));
            StartCoroutine(AnimateDot(page2IndicatorImage, inactiveDotColor));
        } else {
            StartCoroutine(AnimateDot(page1IndicatorImage, inactiveDotColor));
            StartCoroutine(AnimateDot(page2IndicatorImage, activeDotColor));
        }
    }

    IEnumerator AnimateDot(Image dot, Color targetColor) {
        Transform dotTransform = dot.transform;

        Vector3 startScale = dotTransform.localScale;

        Color startColor = dot.color;

        float elapsed = 0f;
        while (elapsed < indicatorTransitionSpeed) {
            elapsed += Time.deltaTime;
            float t = elapsed / indicatorTransitionSpeed;
            dot.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        dot.color = targetColor;
    }

    IEnumerator FadeOutHelpGuide() {
        isTransitioning = true;
        helpGuide.alpha = 1f;

        float elapsedTime = 0f;
        while (elapsedTime < FADE_DURATION) {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Pow(elapsedTime / FADE_DURATION, 3);
            helpGuide.alpha = Mathf.Clamp01(1f - t);
            yield return null;
        }

        helpGuide.alpha = 0f;
        helpGuide.blocksRaycasts = false;
        helpGuide.interactable = false;
        isTransitioning = false;
    }
}