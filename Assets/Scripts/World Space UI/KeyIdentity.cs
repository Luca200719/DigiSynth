using UnityEngine;
using UnityEngine.InputSystem;

public class KeyIdentity : MonoBehaviour {
    public Key keyCode;
    public int keyIndex;
    public bool isBlackKey;

    const float transitionSpeed = 15f;

    const float whiteKeyYOffset = -0.0015f;
    const float whiteKeyRotationX = 1.4f;

    const float blackKeyYOffset = -0.0020f;
    const float blackKeyRotationX = 2.5f;

    const float snapThreshold = 0.001f;

    float targetValue;
    float currentValue;

    Vector3 basePosition;
    Quaternion baseRotation;
    Quaternion pressedRotation;

    void Awake() {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        float rotationX = isBlackKey ? blackKeyRotationX : whiteKeyRotationX;
        pressedRotation = baseRotation * Quaternion.Euler(rotationX, 0f, 0f);
    }

    void Update() {
        float diff = targetValue - currentValue;
        if (Mathf.Abs(diff) < snapThreshold) {
            if (currentValue == targetValue) return;
            currentValue = targetValue;
        }
        else {
            currentValue = Mathf.Lerp(currentValue, targetValue, transitionSpeed * Time.deltaTime);
        }

        float smoothValue = currentValue * currentValue * (3f - 2f * currentValue);

        float yOffset = isBlackKey ? blackKeyYOffset : whiteKeyYOffset;

        transform.localPosition = new Vector3(basePosition.x, basePosition.y + yOffset * smoothValue, basePosition.z);

        transform.localRotation = Quaternion.SlerpUnclamped(baseRotation, pressedRotation, smoothValue);
    }

    public void AnimatePress() => targetValue = 1f;
    public void AnimateRelease() => targetValue = 0f;
}