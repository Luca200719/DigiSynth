using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RaycastManager : MonoBehaviour {
    OrbitCameraController orbitCamera;
    KeyboardController keyboardController;

    Camera _cam;
    Mouse _mouse;
    Ray _ray;
    RaycastHit _hit;

    bool isTransitioning = false;

    bool overKeyboard = false;
    public KeyIdentity activeKey = null;
    KeyIdentity currentKey = null;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Raycast Manager");

        _cam = Camera.main;
        _mouse = Mouse.current;
    }

    void Start() {
        orbitCamera = ObjectRegistry.registry.GetObjectList("Orbit Camera Controller")[0].GetComponent<OrbitCameraController>();
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();
    }

    void Update() {
        if (_mouse.leftButton.wasPressedThisFrame && !isTransitioning) {
            if (!IsMouseOverUI()) {
                PerformRaycast();
            }
        }

        if (_mouse.leftButton.isPressed && overKeyboard) {
            Vector2 mousePos = _mouse.position.ReadValue();
            _ray = _cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(_ray, out _hit, 100f)) {
                currentKey = _hit.transform.childCount > 0 ? _hit.transform.GetChild(0).GetComponent<KeyIdentity>() : null;

                if (currentKey != null && activeKey != currentKey) {
                    keyboardController.ReleaseKeyByIdentity(activeKey);
                    keyboardController.PressKeyByIdentity(currentKey);

                    activeKey = currentKey;
                }
            }
        }

        if (_mouse.leftButton.wasReleasedThisFrame && overKeyboard) {
            keyboardController.ReleaseKeyByIdentity(activeKey);
            activeKey = null;
            overKeyboard = false;
        }
    }

    bool IsMouseOverUI() {
        Vector2 mousePos = _mouse.position.ReadValue();

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = mousePos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results) {
            if (result.gameObject.CompareTag("Screen Space UI")) {
                return true;
            }
        }

        return false;
    }

    void PerformRaycast() {
        Vector2 mousePos = _mouse.position.ReadValue();
        _ray = _cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(_ray, out _hit, 100f)) {
            HandleRaycastHit(_hit);
        } else {
            OrbitCamera();
        }
    }

    void HandleRaycastHit(RaycastHit hit) {
        var component = hit.transform.GetComponent<MonoBehaviour>();

        if (component is ButtonController buttonController) {
            buttonController.OnRaycastHit(hit);
            return;
        }

        if (component is KnobController knobController) {
            knobController.OnRaycastHit(hit);
            return;
        }

        if (component is WheelController wheelController) {
            wheelController.OnRaycastHit(hit);
            return;
        }

        if (component is SwitchController switchController) {
            switchController.OnRaycastHit(hit);
            return;
        }

        if (component is SelectButtonController selectController) {
            selectController.OnRaycastHit(hit);
            return;
        }

        if (component is DisplayToggleController displayToggleController) {
            displayToggleController.OnRaycastHit(hit);
            return;
        }

        if (component is OctaveButtonsController octaveButtonsController) {
            octaveButtonsController.OnRaycastHit(hit);
            return;
        }

        if (component is PresetButtonController presetButtonController) {
            presetButtonController.OnRaycastHit(hit);
            return;
        }

        ButtonController parentButtonController = hit.transform.parent?.GetComponent<ButtonController>();
        if (parentButtonController != null) {
            parentButtonController.OnRaycastHit(hit);
            return;
        }

        OctaveButtonsController parentOctaveController = hit.transform.parent?.GetComponent<OctaveButtonsController>();
        if (parentOctaveController != null) {
            parentOctaveController.OnRaycastHit(hit);
            return;
        }

        KeyIdentity childKeyIdentity = _hit.transform.childCount > 0 ? _hit.transform.GetChild(0).GetComponent<KeyIdentity>() : null;
        if (childKeyIdentity != null) {
            activeKey = childKeyIdentity;
            keyboardController.PressKeyByIdentity(activeKey);
            overKeyboard = true;
            return;
        }
    }

    public void SetTransitioning(bool transitioning) {
        isTransitioning = transitioning;
    }

    public bool IsTransitioning() {
        return isTransitioning;
    }

    void OrbitCamera() {
        if (!isTransitioning) {
            orbitCamera.StartOrbit();
        }
    }
}