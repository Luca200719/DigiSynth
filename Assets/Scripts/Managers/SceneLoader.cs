using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SceneLoader : MonoBehaviour {
    public Slider loadingBar;
    public float fillSpeed = 2f;
    float displayProgress = 0f;
    float targetProgress = 0f;

    bool sceneLoaded = false;

    public float fadeOutDelay = 0.5f;

    Keyboard keyboard;

    public CanvasGroup backgroundPanel;
    public CanvasGroup transitionPanel;

    public CanvasGroup loadingBarPanel;

    public CanvasGroup videoPanel;
    public VideoPlayer videoPlayer;

    public CanvasGroup thumbnailPanel;
    public CanvasGroup skipVideoPanel;

    public bool firstTime = false;
    public bool canSkip = false; 

    void Awake() {
        DontDestroyOnLoad(transform.parent);
    }

    void Start() {
        keyboard = Keyboard.current;

        Cursor.lockState = CursorLockMode.Confined;
        CursorManager.cursorManager.lockPosition = true;

        videoPlayer.Play();
        videoPlayer.Pause();

        videoPlayer.Prepare();

        transitionPanel.alpha = 0f;
        StartCoroutine(FadeInAndLoad());
    }

    void Update() {
        if (canSkip) {
            if (keyboard.anyKey.wasPressedThisFrame) {
                skipVideoPanel.alpha = 0f;
                thumbnailPanel.alpha = 1f;
                videoPlayer.Pause();
                StartCoroutine(FadeOutSkipVideo());
            }
        }
    }

    public IEnumerator LoadScene() {
        AsyncOperation operation = SceneManager.LoadSceneAsync(1);

        while (operation.progress < 0.80f) {
            targetProgress = Mathf.Clamp01(operation.progress / 0.80f) * 0.5f;
            displayProgress = Mathf.Lerp(displayProgress, targetProgress, fillSpeed * Time.deltaTime);
            loadingBar.value = displayProgress;
            yield return null;
        }

        targetProgress = 0.90f;
        while (displayProgress < 0.89f) {
            displayProgress = Mathf.Lerp(displayProgress, targetProgress, fillSpeed * Time.deltaTime);
            loadingBar.value = displayProgress;
            yield return null;
        }

        operation.allowSceneActivation = true;

        while (SceneManager.GetActiveScene().buildIndex != 1 && !videoPlayer.isPrepared) {
            yield return null;
        }

        sceneLoaded = true;

        targetProgress = 1f;
        while (displayProgress < 0.99f) {
            displayProgress = Mathf.Lerp(displayProgress, targetProgress, fillSpeed * 3f * Time.deltaTime);
            loadingBar.value = displayProgress;
            yield return null;
        }

        loadingBar.value = 1f;
    }

    public IEnumerator LoadSynth() {
        StartCoroutine(LoadScene());

        while (!sceneLoaded) {
            yield return null;
        }

        yield return null;

        DataManager.dataManager.Load();

        if (DataManager.dataManager.data.firstTime) {
            firstTime = true;
        }

        yield return null;

        if (DataManager.dataManager.data.firstTime) {
            Preset blank = DataManager.dataManager.presetData.blankPreset;

            EffectsController effects = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();
            EnvelopeController envelope = ObjectRegistry.registry.GetObjectList("Envelope")[0].GetComponent<EnvelopeController>();
            AudioController audio = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
            DisplayController display = ObjectRegistry.registry.GetObjectList("Display")[0].GetComponent<DisplayController>();
            KeyboardController keyboard = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();

            Array.Copy(effects.valueCaps, blank.valueCaps, effects.valueCaps.Length);

            blank.envelopeAttack = envelope.normalizedADSR.attack;
            blank.envelopeDecay = envelope.normalizedADSR.decay;
            blank.envelopeSustain = envelope.normalizedADSR.sustain;
            blank.envelopeRelease = envelope.normalizedADSR.release;

            blank.oscillatorEnabled = (bool[])audio.oscillatorEnabled.Clone();
            blank.oscillatorWaveType = (int[])audio.oscillatorWaveType.Clone();
            blank.oscillatorLevel = (float[])audio.oscillatorLevel.Clone();

            blank.octave = keyboard.currOctave;

            display.WriteDefaultsToBlankPreset();

            DataManager.dataManager.InitializeFromBlankPreset();
            DataManager.dataManager.SavePresets();
        }

        DataManager.dataManager.LoadFromCurrentState();
    }

    IEnumerator FadeInSkipVideo() {
        canSkip = true;
        float alpha = 0f;
        while (alpha < 1f) {
            alpha = Mathf.MoveTowards(alpha, 1f, 5f * Time.deltaTime);
            skipVideoPanel.alpha = alpha;
            yield return null;
        }
    }

    IEnumerator FadeOutSkipVideo() {
        canSkip = false;
        float alpha = 1f;
        while (alpha > 0f) {
            alpha = Mathf.MoveTowards(alpha, 0f, 5f * Time.deltaTime);
            skipVideoPanel.alpha = alpha;
            yield return null;
        }
    }

    IEnumerator FadeInAndLoad() {
        float alpha = 1f;
        while (alpha > 0f) {
            alpha = Mathf.MoveTowards(alpha, 0f, 2f * Time.deltaTime);
            transitionPanel.alpha = alpha;
            yield return null;
        }

        yield return StartCoroutine(LoadSynth());

        yield return PlayVideoThenDestroy();
    }

    IEnumerator PlayVideoThenDestroy() {
        float alpha = 0f;
        while (alpha < 1f) {
            alpha = Mathf.MoveTowards(alpha, 1f, 2f * Time.deltaTime);
            transitionPanel.alpha = alpha;
            yield return null;
        }

        videoPanel.alpha = 1;
        yield return new WaitForSeconds(fadeOutDelay);

        alpha = 1f;
        while (alpha > 0f) {
            alpha = Mathf.MoveTowards(alpha, 0f, 2f * Time.deltaTime);
            transitionPanel.alpha = alpha;
            yield return null;
        }

        videoPlayer.Play();
        
        if (!firstTime) {
            StartCoroutine(FadeInSkipVideo());
        }

        bool fadedSkipVideo = false;
        while (videoPlayer.isPlaying) {
            if (!fadedSkipVideo && videoPlayer.frame >= 800 && !firstTime) {
                fadedSkipVideo = true;
                StartCoroutine(FadeOutSkipVideo());
            }

            yield return null;
        }

        yield return new WaitForSeconds(fadeOutDelay * 2);

        alpha = 0f;
        while (alpha < 1f) {
            alpha = Mathf.MoveTowards(alpha, 1f, 2f * Time.deltaTime);
            transitionPanel.alpha = alpha;
            yield return null;
        }

        backgroundPanel.alpha = 0f;
        loadingBarPanel.alpha = 0f;
        videoPanel.alpha = 0f;

        yield return new WaitForSeconds(fadeOutDelay);

        alpha = 1f;
        while (alpha > 0f) {
            alpha = Mathf.MoveTowards(alpha, 0f, 2f * Time.deltaTime);
            transitionPanel.alpha = alpha;
            yield return null;
        }

        Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2f, Screen.height / 2));
        Cursor.lockState = CursorLockMode.Confined;
        CursorManager.cursorManager.lockPosition = false;

        KeyboardController keyboard = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();
        keyboard.isMenuOpen = false;
        keyboard.openingSequence = false;

        Destroy(gameObject);
    }
}