using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardController : MonoBehaviour {
    AudioController audioController;
    EnvelopeController envelopeController;
    OctaveDisplayController octaveDisplayController;
    RaycastManager raycastManager;

    ScreenSpaceUIController screenSpaceUIController;
    HelpGuideController helpGuideController;
    QuitMenuController quitMenuController;

    public int currOctave = 3;
    int minOctave = 0;
    int maxOctave = 6;

    float[] _incrementTable;
    bool[] _activeKeys;

    Keyboard _keyboard;

    Dictionary<Key, List<KeyIdentity>> _keyIdentityMap;

    static readonly Key[] _keyboardMap = new Key[] {
        Key.Z, Key.S, Key.X, Key.D, Key.C, Key.V, Key.G, Key.B, Key.H, Key.N,
        Key.J, Key.M, Key.Comma, Key.Digit3, Key.E, Key.Digit4, Key.R, Key.T,
        Key.Digit6, Key.Y, Key.Digit7, Key.U, Key.Digit8, Key.I, Key.O
    };

    Key alternateC = Key.W;
    const int CIndex = 12;

    Key octaveUpKey = Key.Equals;
    Key octaveDownKey = Key.Minus;

    float _sampleRate;
    const float BaseFrq = 440f;
    const float TwoPi = 2f * math.PI;
    const float Inv12 = 1f / 12f;

    bool helpGuideOpen = false;
    bool quitMenuOpen = false;
    public bool isMenuOpen = true;
    public bool openingSequence = true;

    public GameObject pianoRoll;

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Keyboard");
        _activeKeys = new bool[_keyboardMap.Length];
        _incrementTable = new float[88];
        _keyboard = Keyboard.current;
    }

    void Start() {
        envelopeController = ObjectRegistry.registry.GetObjectList("Envelope")[0].GetComponent<EnvelopeController>();
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        octaveDisplayController = ObjectRegistry.registry.GetObjectList("Octave Display")[0].GetComponent<OctaveDisplayController>();
        raycastManager = ObjectRegistry.registry.GetObjectList("Raycast Manager")[0].GetComponent<RaycastManager>();
        screenSpaceUIController = ObjectRegistry.registry.GetObjectList("Screen Space UI Controller")[0].GetComponent<ScreenSpaceUIController>();
        helpGuideController = ObjectRegistry.registry.GetObjectList("Help Guide")[0].GetComponent<HelpGuideController>();
        quitMenuController = ObjectRegistry.registry.GetObjectList("Quit Menu")[0].GetComponent<QuitMenuController>();

        _sampleRate = AudioSettings.outputSampleRate;
        float invSampleRate = 1f / _sampleRate;

        for (int i = 0; i < _incrementTable.Length; i++) {
            float frequency = BaseFrq * math.pow(2f, (i - 48f) * Inv12);
            _incrementTable[i] = frequency * TwoPi * invSampleRate;
        }

        InitializeKeyIdentityMap();

        if (!DataManager.dataManager.data.firstTime) {
            currOctave = DataManager.dataManager.data.currentState.octave;
            octaveDisplayController.UpdateOctaveDisplay(currOctave);
            ReleaseAllActiveKeys();
        }
        else {
            SaveToCurrentState();
        }
    }
    
    public void SaveToCurrentState() {
        DataManager.dataManager.data.currentState.octave = currOctave;
    }

    public void LoadFromCurrentState() {
        currOctave = DataManager.dataManager.data.currentState.octave;
        octaveDisplayController.UpdateOctaveDisplay(currOctave);
        ReleaseAllActiveKeys();
    }

    void InitializeKeyIdentityMap() {
        _keyIdentityMap = new Dictionary<Key, List<KeyIdentity>>();

        KeyIdentity[] allKeys = pianoRoll.GetComponentsInChildren<KeyIdentity>();

        foreach (KeyIdentity keyIdentity in allKeys) {
            if (!_keyIdentityMap.ContainsKey(keyIdentity.keyCode)) {
                _keyIdentityMap[keyIdentity.keyCode] = new List<KeyIdentity>();
            }
            _keyIdentityMap[keyIdentity.keyCode].Add(keyIdentity);
        }
    }

    void Update() {
        if (_keyboard[Key.Escape].wasPressedThisFrame && !openingSequence) {
            HandleEscape();
        }

        if (!isMenuOpen) {
            HandleOctaveSwitch();
            HandleKeyboardInput();
        }
    }

    void HandleEscape() {
        if (quitMenuOpen) {
            quitMenuController.CloseQuitMenu();
        }
        else if (helpGuideOpen) {
            helpGuideController.CloseHelpGuide();
        }
        else {
            screenSpaceUIController.OpenQuitMenu();
        }
    }

    void HandleOctaveSwitch() {
        if (_keyboard[octaveUpKey].wasPressedThisFrame && currOctave < maxOctave) {
            currOctave++;
            ReleaseAllActiveKeys();
            octaveDisplayController.UpdateOctaveDisplay(currOctave);
        } else if (_keyboard[octaveDownKey].wasPressedThisFrame && currOctave > minOctave) {
            currOctave--;
            ReleaseAllActiveKeys();
            octaveDisplayController.UpdateOctaveDisplay(currOctave);
        }
    }

    void HandleKeyboardInput() {
        for (int k = 0; k < _keyboardMap.Length; k++) {
            Key key = _keyboardMap[k];
            bool keyPressed = _keyboard[key].isPressed;

            if (_keyboard[key].wasPressedThisFrame && keyPressed && !_activeKeys[k]) {
                PressKey(key, k);
            } else if (_keyboard[key].wasReleasedThisFrame && !keyPressed && _activeKeys[k]) {
                ReleaseKey(key, k);
            }
        }

        bool cCommaPressed = _keyboard[Key.Comma].isPressed;
        bool cAltPressed = _keyboard[alternateC].isPressed;
        bool cPressed = cCommaPressed || cAltPressed;

        if (cPressed && !_activeKeys[CIndex]) {
            PressKey(Key.Comma, CIndex);
        } else if (!cPressed && _activeKeys[CIndex]) {
            ReleaseKey(Key.Comma, CIndex);
        }
    }

    void PressKey(Key keyCode, int keyIndex) {
        int noteIndex = keyIndex + (12 * (currOctave - 1)) + 3;

        if (noteIndex >= 0 && noteIndex < _incrementTable.Length) {
            Note newNote = CreateNote(keyCode, noteIndex);
            audioController.AddNote(newNote);

            _activeKeys[keyIndex] = true;

            if (_keyIdentityMap.ContainsKey(keyCode)) {
                foreach (KeyIdentity keyIdentity in _keyIdentityMap[keyCode]) {
                    keyIdentity.AnimatePress();
                }
            }
        }
    }

    void ReleaseKey(Key keyCode, int keyIndex) {
        if (raycastManager.activeKey != null && raycastManager.activeKey.keyIndex == keyIndex) {
            return;
        }

        audioController.ReleaseKey(keyCode);
        _activeKeys[keyIndex] = false;

        if (_keyIdentityMap.ContainsKey(keyCode)) {
            foreach (KeyIdentity keyIdentity in _keyIdentityMap[keyCode]) {
                keyIdentity.AnimateRelease();
            }
        }
    }

    Note CreateNote(Key keyCode, int noteIndex) {
        return new Note {
            key = keyCode,
            phase = 0f,
            increment = _incrementTable[noteIndex],
            multiplier = 0f,
            currentStage = 0,
            sustainLevel = envelopeController.ADSR.sustain,
            sampleRate = _sampleRate,
            attackTime = 0f,
            attackDuration = envelopeController.ADSR.attack,
            decayTime = 0f,
            decayDuration = envelopeController.ADSR.decay,
            releaseTime = 0f,
            releaseDuration = envelopeController.ADSR.release,
            releaseStartLevel = 0f
        };
    }

    void ReleaseAllActiveKeys() {
        for (int k = 0; k < _keyboardMap.Length; k++) {
            if (_activeKeys[k]) {
                ReleaseKey(_keyboardMap[k], k);
            }
        }

        KeyIdentity active = raycastManager.activeKey;
        if (active != null) {
            ReleaseKeyByIdentity(active);
            active = null;
        }
    }

    public void ToggleHelpGuide() {
        helpGuideOpen = !helpGuideOpen;

        isMenuOpen = helpGuideOpen || quitMenuOpen;

        if (isMenuOpen) {
            ReleaseAllActiveKeys();
        }
    }

    public void ToggleQuitMenu() {
        quitMenuOpen = !quitMenuOpen;

        isMenuOpen = helpGuideOpen || quitMenuOpen;

        if (isMenuOpen && !helpGuideOpen) {
            ReleaseAllActiveKeys();
        }
    }

    public void PressKeyByIdentity(KeyIdentity keyIdentity) {
        int keyIndex = keyIdentity.keyIndex;

        if (!_activeKeys[keyIndex]) {
            PressKey(keyIdentity.keyCode, keyIndex);
        }
    }

    public void ReleaseKeyByIdentity(KeyIdentity keyIdentity) {
        int keyIndex = keyIdentity.keyIndex;
        Key keyCode = keyIdentity.keyCode;

        if (!_activeKeys[keyIndex]) {
            return;
        }

        if (keyIndex == CIndex) {
            bool cCommaPressed = _keyboard[Key.Comma].isPressed;
            bool cAltPressed = _keyboard[alternateC].isPressed;
            if (cCommaPressed || cAltPressed) {
                return;
            }
            audioController.ReleaseKey(Key.Comma);
            audioController.ReleaseKey(alternateC);
        } else {
            if (_keyboard[keyCode].isPressed) {
                return;
            }
            audioController.ReleaseKey(keyCode);
        }

        _activeKeys[keyIndex] = false;

        keyIdentity.AnimateRelease();

        if (_keyIdentityMap.ContainsKey(keyCode)) {
            foreach (KeyIdentity ki in _keyIdentityMap[keyCode]) {
                if (ki != keyIdentity) {
                    ki.AnimateRelease();
                }
            }
        }
    }

    public void OctaveUp() {
        if (currOctave < maxOctave) {
            currOctave++;
            ReleaseAllActiveKeys();
            octaveDisplayController.UpdateOctaveDisplay(currOctave);
        }
    }

    public void OctaveDown() {
        if (currOctave > minOctave) {
            currOctave--;
            ReleaseAllActiveKeys();
            octaveDisplayController.UpdateOctaveDisplay(currOctave);
        }
    }

    public bool CanOctaveUp() => currOctave < maxOctave;
    public bool CanOctaveDown() => currOctave > minOctave;
}