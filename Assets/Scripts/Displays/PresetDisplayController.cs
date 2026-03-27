using System.Collections;
using TMPro;
using UnityEngine;

public class PresetDisplayController : MonoBehaviour {
    public static PresetDisplayController displayController;

    TextMeshProUGUI header;
    TextMeshProUGUI dirtyIndicator;
    TextMeshProUGUI presetID;

    public bool letGo;

    Coroutine loadCoroutine;
    Coroutine saveCoroutine;
    Coroutine resultCoroutine;

    void Awake() {
        if (displayController != null && displayController != this) {
            Destroy(gameObject);
            return;
        }

        displayController = this;
        ObjectRegistry.registry.Register(gameObject, "Preset Display");
    }

    void Start() {
        header = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        dirtyIndicator = transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        presetID = transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        SetID(DataManager.dataManager.data.presetID + 1);
        SetDirty(DataManager.dataManager.data.isDirty);
    }

    public void SetHeader(string text) {
        header.text = text;
    }

    public void SetID(int ID) {
        presetID.text = ID.ToString();
    }

    public void SetDirty(bool isDirty) {
        dirtyIndicator.text = isDirty ? "." : "";
    }

    public void Load() {
        if (loadCoroutine != null) StopCoroutine(loadCoroutine);
        if (saveCoroutine != null) StopCoroutine(saveCoroutine);
        if (resultCoroutine != null) StopCoroutine(resultCoroutine);

        string currID = (DataManager.dataManager.data.presetID + 1).ToString();
        loadCoroutine = StartCoroutine(LoadPreset(currID));
    }

    IEnumerator LoadPreset(string currID) {
        SetHeader("LOADING");
        dirtyIndicator.fontSize = 1.25f;
        dirtyIndicator.text = "PRESET";
        presetID.text = currID;

        DataManager.dataManager.LoadPreset();

        yield return new WaitForSeconds(2f);

        SetHeader("PRESET");
        dirtyIndicator.text = "";
        dirtyIndicator.fontSize = 2f;
        loadCoroutine = null;
    }

    public void Save() {
        letGo = false;
        string currID = (DataManager.dataManager.data.presetID + 1).ToString();
        string currDirty = (DataManager.dataManager.data.isDirty) ? "." : "";

        if (loadCoroutine != null) StopCoroutine(loadCoroutine);
        if (saveCoroutine != null) StopCoroutine(saveCoroutine);
        if (resultCoroutine != null) StopCoroutine(resultCoroutine);

        saveCoroutine = StartCoroutine(SavePreset(currID, currDirty));
    }

    IEnumerator SavePreset(string currID, string currDirty) {
        SetHeader("SAVING");
        dirtyIndicator.fontSize = 1.25f;
        dirtyIndicator.text = "HOLD FOR:";
        float elapsedTime = 0f;

        while (elapsedTime < 6f && !letGo) {
            elapsedTime += Time.deltaTime;
            SetID(Mathf.FloorToInt(6f - elapsedTime));
            yield return null;
        }

        if (!letGo) {
            resultCoroutine = StartCoroutine(AbleToSave(currID));
            DataManager.dataManager.SavePreset();
        }
        else {
            resultCoroutine = StartCoroutine(UnableToSave(currID, currDirty));
        }

        saveCoroutine = null;

        yield return null;
    }

    IEnumerator AbleToSave(string currID) {
        SetHeader("SAVED");
        dirtyIndicator.text = "TO SLOT";
        presetID.text = currID;

        yield return new WaitForSeconds(2f);

        SetHeader("PRESET");
        dirtyIndicator.fontSize = 2f;
        dirtyIndicator.text = "";

        yield return null;
    }

    IEnumerator UnableToSave(string currID, string currDirty) {
        SetHeader("UNABLE");
        dirtyIndicator.text = "TO SAVE";
        presetID.text = "PRESET";

        yield return new WaitForSeconds(2f);

        SetHeader("PRESET");
        presetID.text = currID;
        dirtyIndicator.fontSize = 2f;
        dirtyIndicator.text = currDirty;

        yield return null;
    }
}