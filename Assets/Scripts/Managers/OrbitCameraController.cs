using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCameraController : MonoBehaviour {
    Mouse mouse;
    ScreenSpaceUIController screenSpaceUIController;
    CameraAspectExpander cameraExpander;

    readonly Vector3 orbitCenter = new Vector3(0, 0, -0.065f);
    readonly float orbitDistance = 10.155f;
    readonly float orbitSensitivity = 0.4f;

    bool isOrbiting = false;
    bool isExiting = false;
    Vector2 clickPosition;

    float currentPitch, currentYaw;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Orbit Camera Controller");
    }

    void Start() {
        mouse = Mouse.current;

        screenSpaceUIController = ObjectRegistry.registry.GetObjectList("Screen Space UI Controller")[0].GetComponent<ScreenSpaceUIController>();
        cameraExpander = ObjectRegistry.registry.GetObjectList("Camera Expander")[0].GetComponent<CameraAspectExpander>();
    }

    void Update() {
        if (mouse.leftButton.isPressed && isOrbiting) {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            OrbitCamera(mouseDelta);
        }

        if (mouse.leftButton.wasReleasedThisFrame && isOrbiting) {
            StopOrbit();
        }
    }

    void OrbitCamera(Vector2 mouseDelta) {
        currentYaw += mouseDelta.x * orbitSensitivity;
        currentPitch -= mouseDelta.y * orbitSensitivity;

        currentPitch = Mathf.Clamp(currentPitch, 1f, 179f);

        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Sin(pitchRad) * Mathf.Sin(yawRad), Mathf.Cos(pitchRad), Mathf.Sin(pitchRad) * Mathf.Cos(yawRad));

        transform.position = orbitCenter + offset * orbitDistance;

        transform.LookAt(orbitCenter, Vector3.up);
    }

    public void StartOrbit() {
        if (isExiting) return;
        isOrbiting = true;

        cameraExpander.DisableTransitions();

        screenSpaceUIController.GoToOrbit();

        Vector3 relativePos = transform.position - orbitCenter;
        currentPitch = Vector3.Angle(Vector3.up, relativePos);
        Vector3 flatDir = new Vector3(relativePos.x, 0, relativePos.z);
        currentYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;

        clickPosition = mouse.position.ReadValue();
        CursorManager.cursorManager.lockPosition = true;
    }

    public void StopOrbit() {
        mouse.WarpCursorPosition(clickPosition);
        CursorManager.cursorManager.UnlockPositionNextFrame();

        isOrbiting = false;
        isExiting = true;
        cameraExpander.EnableTransitionsFromOrbit();
        cameraExpander.SetCameraLocation(cameraExpander.lastLocation);
        StartCoroutine(DelayedExitOrbit());
    }

    IEnumerator DelayedExitOrbit() {
        yield return new WaitForSeconds(0.5f);
        screenSpaceUIController.ExitOrbit();
        isExiting = false;
    }
}