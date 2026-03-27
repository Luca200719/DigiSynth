using UnityEngine;

public class CanvasAspectExpander : MonoBehaviour {
    RectTransform panelRect;
    CameraAspectExpander cameraExpander;

    readonly float padding = 0f;

    RectTransform[] scalableElements;

    float keyboardAspectRatio;
    float upperUIAspectRatio;
    float rightUIAspectRatio;

    float transitionTimer = 0f;
    bool isTransitioning = false;
    Vector2 startSize;
    Vector2 targetSize;
    float lastAspect = -1f;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Canvas Expander");
    }

    void Start() {
        panelRect = GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        cameraExpander = ObjectRegistry.registry.GetObjectList("Camera Expander")[0].GetComponent<CameraAspectExpander>();

        keyboardAspectRatio = cameraExpander.keyboardAspectRatio;
        upperUIAspectRatio = cameraExpander.upperUIAspectRatio;
        rightUIAspectRatio = cameraExpander.rightUIAspectRatio;

        int rectTransformCount = 0;
        foreach (Transform child in transform) {
            if (child.CompareTag("Passepartout")) {
                foreach (Transform passepartoutChild in child) {
                    if (passepartoutChild.GetComponent<RectTransform>() != null) {
                        rectTransformCount++;
                    }
                }
                continue;
            }
            if (child.GetComponent<RectTransform>() != null) {
                rectTransformCount++;
            }
        }

        scalableElements = new RectTransform[rectTransformCount];

        int i = 0;
        foreach (Transform child in transform) {
            if (child.CompareTag("Passepartout")) {
                foreach (Transform passepartoutChild in child) {
                    RectTransform rectTransform = passepartoutChild.GetComponent<RectTransform>();
                    if (rectTransform != null) {
                        scalableElements[i] = rectTransform;
                        i++;
                    }
                }
                continue;
            }
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null) {
                scalableElements[i] = rt;
                i++;
            }
        }

        AdjustCanvasImmediate();
    }

    void Update() {
        if (isTransitioning) {
            UpdateCanvasTransition();
            return;
        }

        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Abs(currentAspect - lastAspect) > 0.01f) {
            Vector2 newSize = GetIdealSizeForLocation(cameraExpander.currentLocation);
            panelRect.sizeDelta = newSize;
            lastAspect = currentAspect;
            UpdateScalableElements();
        }
    }

    void AdjustCanvasImmediate() {
        Vector2 size = GetIdealSizeForLocation(cameraExpander.currentLocation);
        panelRect.sizeDelta = size;
        lastAspect = (float)Screen.width / Screen.height;
        UpdateScalableElements();
    }

    Vector2 GetIdealSizeForLocation(CameraAspectExpander.CameraLocation location) {
        float targetAspect = location switch {
            CameraAspectExpander.CameraLocation.UpperUI => upperUIAspectRatio,
            CameraAspectExpander.CameraLocation.RightUI => rightUIAspectRatio,
            _ => keyboardAspectRatio
        };

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float currentAspect = screenWidth / screenHeight;

        Vector2 size;
        if (currentAspect >= targetAspect) {
            float width = screenHeight * targetAspect;
            size = new Vector2(width, screenHeight);
        }
        else {
            float height = screenWidth / targetAspect;
            size = new Vector2(screenWidth, height);
        }

        size.x *= (1f - padding * 2f);
        size.y *= (1f - padding * 2f);

        size.x = Mathf.Min(size.x, screenWidth * (1f - padding * 2f));
        size.y = Mathf.Min(size.y, screenHeight * (1f - padding * 2f));

        return size;
    }

    public void OnCameraLocationChanged(CameraAspectExpander.CameraLocation newLocation) {
        startSize = panelRect.sizeDelta;
        targetSize = GetIdealSizeForLocation(newLocation);

        transitionTimer = 0f;
        isTransitioning = true;
    }

    void UpdateCanvasTransition() {
        float transitionDuration = cameraExpander.transitionDuration;
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);

        float curvedT = cameraExpander.transitionCurve.Evaluate(t);

        panelRect.sizeDelta = Vector2.Lerp(startSize, targetSize, curvedT);
        UpdateScalableElements();

        if (t >= 1f) {
            isTransitioning = false;
            panelRect.sizeDelta = targetSize;
        }
    }

    void UpdateScalableElements() {
        Vector2 panelSize = panelRect.sizeDelta;

        for (int i = 0; i < scalableElements.Length; i++) {
            float size = Mathf.Min(panelSize.x, panelSize.y) * 0.15f;
            scalableElements[i].sizeDelta = new Vector2(size, size);
        }
    }
}