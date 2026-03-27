using UnityEngine.InputSystem;

public struct Note {
    public Key key;
    public float phase;
    public float increment;
    public float multiplier;
    public float previousMultiplier;
    public int currentStage; // 0: attack, 1: decay, 2: sustain, 3: release, 4: off
    public float sustainLevel;
    public float sampleRate;

    public float attackTime;
    public float attackDuration;
    public float decayTime;
    public float decayDuration;
    public float releaseTime;
    public float releaseDuration;
    public float releaseStartLevel;

    public void release() {
        if (currentStage < 3) {
            currentStage = 3;
            releaseTime = 0f;
            releaseStartLevel = multiplier;
        }
    }

    public bool isOver() => (currentStage == 4);
}