using System.IO;
using UnityEngine;

[System.Serializable]
public class Preset {
    public float[] valueCaps = new float[35];

    public bool[] switchStates = new bool[9];

    public float[] displayKnobParameters = new float[36];
    public int selectedEffectIndex = -1;

    public float envelopeAttack;
    public float envelopeDecay;
    public float envelopeSustain;
    public float envelopeRelease;

    public bool[] oscillatorEnabled = new bool[4];
    public int[] oscillatorWaveType = new int[4];
    public float[] oscillatorLevel = new float[4];

    public int octave;
}

[System.Serializable]
public class SaveData {
    public bool firstTime = true;
    public bool playTutorial = true;

    public bool displayToggleState = true; // true = Waveform, false = SpectrumAnalyzer

    public bool isDirty = false;
    public int presetID = 0;

    public Preset currentState = new Preset();
}

[System.Serializable]
public class PresetData {
    public Preset blankPreset = new Preset();
    public Preset[] presets = new Preset[8];
}

public class DataManager : MonoBehaviour {
    public static DataManager dataManager { get; private set; }

    public SaveData data = new SaveData();
    public PresetData presetData = new PresetData();

    private string SavePath {
        get {
            #if UNITY_EDITOR
                        return Path.Combine(Application.persistentDataPath, "SaveData.json");
            #else
                        return Path.Combine(Application.dataPath, "..", "SaveData.json");
            #endif
        }
    }

    private string PresetPath {
        get {
            #if UNITY_EDITOR
                        return Path.Combine(Application.persistentDataPath, "Presets.json");
            #else
                        return Path.Combine(Application.dataPath, "..", "Presets.json");
            #endif
        }
    }

    public float autosaveInterval = 60f;
    float timer;

    void Awake() {
        if (dataManager != null && dataManager != this) {
            Destroy(gameObject);
            return;
        }
        dataManager = this;
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= autosaveInterval) {
            Save();
            timer = 0f;
        }
    }

    void OnApplicationQuit() {
        Save();
    }

    public void Save() {
        if (ObjectRegistry.registry == null) {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            return;
        }

        var effects = ObjectRegistry.registry.GetObjectList("Effects");
        if (effects == null || effects.Count == 0) {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            return;
        }

        ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>().SaveToCurrentState();
        ObjectRegistry.registry.GetObjectList("Envelope")[0].GetComponent<EnvelopeController>().SaveToCurrentState();
        ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>().SaveToCurrentState();
        ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>().SaveToCurrentState();
        ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>().SaveToCurrentState();

        string json2 = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json2);
    }

    public void SavePresets() {
        string json = JsonUtility.ToJson(presetData, prettyPrint: true);
        File.WriteAllText(PresetPath, json);
    }

    public void InitializeFromBlankPreset() {
        string blankJson = JsonUtility.ToJson(presetData.blankPreset);

        data.currentState = JsonUtility.FromJson<Preset>(blankJson);

        for (int i = 0; i < presetData.presets.Length; i++) {
            presetData.presets[i] = JsonUtility.FromJson<Preset>(blankJson);
        }

        data.firstTime = false;
        Save();
        SavePresets();
    }

    public void ClearPreset(int index) {
        presetData.presets[index] = JsonUtility.FromJson<Preset>(JsonUtility.ToJson(presetData.blankPreset));
        SavePresets();
    }

    public void FactoryReset() {
        data.currentState = JsonUtility.FromJson<Preset>(JsonUtility.ToJson(presetData.blankPreset));
        Save();
    }

    public void Load() {
        if (!File.Exists(SavePath)) {
            Save();
        }
        else {
            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<SaveData>(json);
        }

        if (!File.Exists(PresetPath)) {
            SavePresets();
        }
        else {
            string json = File.ReadAllText(PresetPath);
            presetData = JsonUtility.FromJson<PresetData>(json);
        }
    }

    public void SavePreset() {
        dataManager.data.isDirty = false;

        Save();
        presetData.presets[dataManager.data.presetID] = JsonUtility.FromJson<Preset>(JsonUtility.ToJson(data.currentState));
        SavePresets();
    }

    public void LoadPreset() {
        dataManager.data.isDirty = false;

        data.currentState = JsonUtility.FromJson<Preset>(JsonUtility.ToJson(presetData.presets[dataManager.data.presetID]));

        foreach (GameObject b in ObjectRegistry.registry.GetObjectList("Button")) {
            b.GetComponent<ButtonController>().LoadFromCurrentState();
        }

        foreach (GameObject w in ObjectRegistry.registry.GetObjectList("LFO Wheel")) {
            w.GetComponent<LFOWheelController>().LoadFromCurrentState();
        }

        EffectsController effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();
        EnvelopeController envelopeController = ObjectRegistry.registry.GetObjectList("Envelope")[0].GetComponent<EnvelopeController>();
        AudioController audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        DisplayController displayController = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();
        KeyboardController keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();

        effectsController.LoadFromCurrentState();
        envelopeController.LoadFromCurrentState();
        audioController.LoadFromCurrentState();
        displayController.LoadFromCurrentState();
        keyboardController.LoadFromCurrentState();

        Save();
    }

    public void LoadFromCurrentState() {
        foreach (GameObject b in ObjectRegistry.registry.GetObjectList("Button")) {
            b.GetComponent<ButtonController>().LoadFromCurrentState();
        }

        foreach (GameObject w in ObjectRegistry.registry.GetObjectList("LFO Wheel")) {
            w.GetComponent<LFOWheelController>().LoadFromCurrentState();
        }

        EffectsController effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();
        EnvelopeController envelopeController = ObjectRegistry.registry.GetObjectList("Envelope")[0].GetComponent<EnvelopeController>();
        AudioController audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        DisplayController displayController = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();
        KeyboardController keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();

        effectsController.LoadFromCurrentState();
        envelopeController.LoadFromCurrentState();
        audioController.LoadFromCurrentState();
        displayController.LoadFromCurrentState();
        keyboardController.LoadFromCurrentState();

        Save();
    }
}