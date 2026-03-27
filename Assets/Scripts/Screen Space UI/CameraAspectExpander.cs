using UnityEngine;

public class CameraAspectExpander : MonoBehaviour {
    public enum CameraLocation {
        Keyboard,
        UpperUI,
        RightUI,
        Orbit
    }

    Camera mainCam;
    Camera skyboxCam;

    CanvasAspectExpander canvasExpander;

    public CameraLocation currentLocation = CameraLocation.Keyboard;
    public CameraLocation lastLocation = CameraLocation.Keyboard;

    public readonly float keyboardAspectRatio = 1.888889f; // 17/9
    readonly float keyboardFOV = 60f;
    readonly float keyboardClamp = 130f;
    readonly Vector3 keyboardPosition = new Vector3(0, 10f, 1.7f);
    readonly Vector3 keyboardRotation = new Vector3(80f, 180f);

    public readonly float upperUIAspectRatio = 3.5f; // 7/2
    readonly float upperUIFOV = 16f;
    readonly float upperUIClamp = 115f;
    Vector3 upperUIPosition = new Vector3(-1.16f, 19f, 0f);
    Vector3 upperUIRotation = new Vector3(82.3f, 180f);

    public readonly float rightUIAspectRatio = 0.6f; // 6/10
    readonly float rightUIFOV = 60f;
    readonly float rightUIClamp = 97.5f;
    readonly Vector3 rightUIPosition = new Vector3(-6.35f, 6f, 1.6f);
    readonly Vector3 rightUIRotation = new Vector3(80f, 180f);

    readonly Vector3 orbitCenter = new Vector3(0, 0, -0.065f);
    readonly float orbitDistance = 10.155f;

    public float transitionDuration = 0.5f;
    public float orbitTransitionDuration = 1.0f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve orbitTransitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    float lastAspect = -1f;

    bool isTransitioning = false;
    float transitionTimer = 0f;
    bool orbitControlActive = false;
    bool isTransitioningFromOrbit;
    bool hasTriggeredUITransition = false;
    bool hasSeededOrbitStartRotation = false;

    float startFOV = 0f;
    float targetFOV = 0f;
    Vector3 startPosition;
    Vector3 targetPosition;
    Quaternion startRotation;
    Quaternion targetRotation;

    ScreenSpaceUIController screenSpaceUIController;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Camera Expander");
    }

    void Start() {
        mainCam = transform.GetChild(0).GetComponent<Camera>();
        skyboxCam = transform.GetChild(1).GetComponent<Camera>();
        canvasExpander = ObjectRegistry.registry.GetObjectList("Canvas Expander")[0].GetComponent<CanvasAspectExpander>();
        screenSpaceUIController = ObjectRegistry.registry.GetObjectList("Screen Space UI Controller")[0].GetComponent<ScreenSpaceUIController>();

        AdjustCameraImmediate();
    }

    void Update() {
        if (orbitControlActive) {
            return;
        }

        if (isTransitioning) {
            UpdateTransition();
            return;
        }

        float currentAspect = (float)Screen.width / Screen.height;

        if (Mathf.Abs(currentAspect - lastAspect) > 0.01f) {
            float fov = CalculateTargetFOV(currentAspect);

            mainCam.fieldOfView = fov;
            skyboxCam.fieldOfView = fov;

            lastAspect = currentAspect;
        }
    }

    void UpdateTransition() {
        transitionTimer += Time.deltaTime;

        float duration = isTransitioningFromOrbit ? orbitTransitionDuration : transitionDuration;
        float t = Mathf.Clamp01(transitionTimer / duration);

        if (isTransitioningFromOrbit && !hasTriggeredUITransition && t >= 0.5f) {
            hasTriggeredUITransition = true;
            if (screenSpaceUIController != null) {
                screenSpaceUIController.ExitOrbit();
            }
        }

        float curvedT = isTransitioningFromOrbit ? orbitTransitionCurve.Evaluate(t) : transitionCurve.Evaluate(t);

        mainCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curvedT);
        skyboxCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curvedT);

        Vector3 lerpedPosition = Vector3.Lerp(startPosition, targetPosition, curvedT);

        if (isTransitioningFromOrbit) {
            Vector3 fromCenter = lerpedPosition - orbitCenter;
            float distance = fromCenter.magnitude;

            if (distance < orbitDistance) {
                lerpedPosition = orbitCenter + fromCenter.normalized * orbitDistance;
            }
        }

        transform.position = lerpedPosition;

        if (isTransitioningFromOrbit && !hasSeededOrbitStartRotation) {
            startRotation = Quaternion.LookRotation(orbitCenter - startPosition, Vector3.up);
            hasSeededOrbitStartRotation = true;
        }

        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);

        if (t >= 1f) {
            isTransitioning = false;
            isTransitioningFromOrbit = false;
            hasTriggeredUITransition = false;
            hasSeededOrbitStartRotation = false;
            mainCam.fieldOfView = targetFOV;
            skyboxCam.fieldOfView = targetFOV;

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else {
            float currentAspect = (float)Screen.width / Screen.height;
            if (Mathf.Abs(currentAspect - lastAspect) > 0.01f) {
                targetFOV = CalculateTargetFOV(currentAspect);
                lastAspect = currentAspect;
            }
        }
    }

    void AdjustCameraImmediate() {
        float currentAspect = (float)Screen.width / Screen.height;
        float fov = CalculateTargetFOV(currentAspect);

        mainCam.fieldOfView = fov;
        skyboxCam.fieldOfView = fov;

        Vector3 pos, rot;
        GetCurrentTransform(out pos, out rot);
        transform.position = pos;
        transform.rotation = Quaternion.Euler(rot);

        lastAspect = currentAspect;
    }

    float CalculateTargetFOV(float currentAspect) {
        float targetFOV, targetAspect, clamp;

        switch (currentLocation) {
            case CameraLocation.UpperUI:
                targetFOV = upperUIFOV;
                targetAspect = upperUIAspectRatio;

                clamp = upperUIClamp;
                break;
            case CameraLocation.RightUI:
                targetFOV = rightUIFOV;
                targetAspect = rightUIAspectRatio;

                clamp = rightUIClamp;
                break;
            default:
                targetFOV = keyboardFOV;
                targetAspect = keyboardAspectRatio;

                clamp = keyboardClamp;
                break;
        }

        if (currentAspect >= targetAspect) {
            return targetFOV;
        }
        else {
            float aspectRatio = targetAspect / currentAspect;
            return Mathf.Clamp(targetFOV * Mathf.Pow(aspectRatio, 0.8f), targetFOV, clamp);
        }
    }

    void GetCurrentTransform(out Vector3 position, out Vector3 rotation) {
        switch (currentLocation) {
            case CameraLocation.UpperUI:
                position = upperUIPosition;
                rotation = upperUIRotation;
                break;
            case CameraLocation.RightUI:
                position = rightUIPosition;
                rotation = rightUIRotation;
                break;
            default: // Keyboard
                position = keyboardPosition;
                rotation = keyboardRotation;
                break;
        }
    }

    public void SetCameraLocation(CameraLocation location) {
        if (currentLocation == location) return;

        if (location == CameraLocation.Orbit && currentLocation != CameraLocation.Orbit) {
            lastLocation = currentLocation;
        }

        isTransitioningFromOrbit = (currentLocation == CameraLocation.Orbit);

        currentLocation = location;

        startFOV = mainCam.fieldOfView;
        startPosition = transform.position;
        startRotation = transform.rotation;

        targetFOV = CalculateTargetFOV((float)Screen.width / Screen.height);
        GetCurrentTransform(out targetPosition, out Vector3 rot);
        targetRotation = Quaternion.Euler(rot);

        transitionTimer = 0f;
        isTransitioning = true;
        hasTriggeredUITransition = false;
        hasSeededOrbitStartRotation = false;

        if (location != CameraLocation.Orbit) {
            lastLocation = currentLocation;
        }

        if (canvasExpander != null) {
            canvasExpander.OnCameraLocationChanged(currentLocation);
        }
    }

    public void DisableTransitions() {
        orbitControlActive = true;
        isTransitioning = false;
    }

    public void EnableTransitions() {
        orbitControlActive = false;
    }

    public void EnableTransitionsFromOrbit() {
        orbitControlActive = false;
        isTransitioningFromOrbit = true;
    }
}