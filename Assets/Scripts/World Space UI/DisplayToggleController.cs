using System.Collections;
using UnityEngine;

public class DisplayToggleController : MonoBehaviour {
    AudioController audioController;
    Animator _anim;

    bool isAnimating;

    static readonly int spectrumAnalyzer = Animator.StringToHash("Toggle_I");
    static readonly int waveform = Animator.StringToHash("Toggle_O");

    void Start() {
        audioController = ObjectRegistry.registry.GetObjectList("Audio")[0].GetComponent<AudioController>();
        _anim = GetComponent<Animator>();

        if (!DataManager.dataManager.data.displayToggleState) {
            StartCoroutine(ApplyToggleState());
        }
    }

    IEnumerator ApplyToggleState() {
        yield return null;
        while (!_anim.isInitialized) {
            yield return null;
        }
        _anim.Play(spectrumAnalyzer);
    }

    public void OnRaycastHit(RaycastHit hit) {
        if (hit.transform == transform) {
            if (!isAnimating) {
                AudioController.DisplayMode currentMode = audioController.GetDisplayMode();

                if (currentMode == AudioController.DisplayMode.Waveform) {
                    _anim.CrossFade(spectrumAnalyzer, 0f);
                    audioController.SetDisplayMode(AudioController.DisplayMode.SpectrumAnalyzer);
                    DataManager.dataManager.data.displayToggleState = false;
                } else {
                    _anim.CrossFade(waveform, 0f);
                    audioController.SetDisplayMode(AudioController.DisplayMode.Waveform);
                    DataManager.dataManager.data.displayToggleState = true;
                }
            }
        }
    }
}