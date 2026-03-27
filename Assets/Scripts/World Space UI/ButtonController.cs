using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonController : MonoBehaviour {
    Animator _anim;

    AudioController audioController;

    public int oscillatorIndex;

    int _prev = 0;
    int _curr = 0;

    Mouse _mouse;

    static readonly string[] buttonAnimationsIn = new string[] {
        "", "Button_1_I", "Button_2_I", "Button_3_I", "Button_4_I", "Button_5_I"
    };

    static readonly string[] buttonAnimationsOut = new string[] {
        "", "Button_1_O", "Button_2_O", "Button_3_O", "Button_4_O", "Button_5_O"
    };

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Button");

        _anim = GetComponent<Animator>();
        _mouse = Mouse.current;
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();

        oscillatorIndex = gameObject.name[4] - '1';
    }

    public void LoadFromCurrentState() {
        bool enabled = DataManager.dataManager.data.currentState.oscillatorEnabled[oscillatorIndex];
        int waveType = DataManager.dataManager.data.currentState.oscillatorWaveType[oscillatorIndex];

        if (enabled && waveType > 0) {
            _prev = waveType;
            _anim.Play(buttonAnimationsIn[waveType]);
            audioController.SetOscillator(oscillatorIndex, true, waveType);
        }
        else {
            _prev = 0;
            _anim.Play(buttonAnimationsOut[1]);
            audioController.SetOscillator(oscillatorIndex, false, 0);
        }
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform.parent == transform) {
            if (int.TryParse(hit.collider.name, out _curr)) {
                ButtonPress();
            }
        }
    }

    void ButtonPress() {
        if (_prev == _curr) {
            if (_prev > 0 && _prev <= buttonAnimationsOut.Length) {
                _anim.CrossFade(buttonAnimationsOut[_prev], 0f);
            }
            audioController.SetOscillator(oscillatorIndex, false, 0);
            _prev = 0;
        } else {
            if (_curr > 0 && _curr <= buttonAnimationsIn.Length) {
                _anim.CrossFade(buttonAnimationsIn[_curr], 0f);
            }

            if (_prev > 0 && _prev <= buttonAnimationsOut.Length) {
                _anim.CrossFade(buttonAnimationsOut[_prev], 0f);
            }

            audioController.SetOscillator(oscillatorIndex, true, _curr);
            _prev = _curr;
        }

        DataManager.dataManager.data.isDirty = true;
        PresetDisplayController.displayController.SetDirty(true);
    }
}