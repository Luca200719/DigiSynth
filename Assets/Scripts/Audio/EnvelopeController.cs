using UnityEngine;

[System.Serializable]
public struct ADSR {
    public float attack;
    public float decay;
    public float sustain;
    public float release;
}

public class EnvelopeController : MonoBehaviour {
    EffectsController effectsController;

    public ADSR ADSR = new ADSR { attack = 0.01f, decay = 0.3f, sustain = 0.5f, release = 0.8f };
    public ADSR normalizedADSR = new ADSR { attack = 0.333f, decay = 0.492f, sustain = 0.5f, release = 0.726f };

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Envelope");
    }

    void Start() {
        effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();

        float attack = effectsController.GetEffectValue(EffectsController.knobEffects["Attack"]);
        if (normalizedADSR.attack != attack) {
            normalizedADSR.attack = attack;
            ADSR.attack = Mathf.Pow(10, attack * 3.699f - 3f);
        }

        float decay = effectsController.GetEffectValue(EffectsController.knobEffects["Decay"]);
        if (normalizedADSR.decay != decay) {
            normalizedADSR.decay = decay;
            ADSR.decay = Mathf.Pow(10, decay * 3f - 2f);
        }

        float sustain = effectsController.GetEffectValue(EffectsController.knobEffects["Sustain"]);
        if (ADSR.sustain != sustain) {
            ADSR.sustain = sustain;
        }

        float release = effectsController.GetEffectValue(EffectsController.knobEffects["Release"]);
        if (normalizedADSR.release != release) {
            normalizedADSR.release = release;
            ADSR.release = Mathf.Pow(10, release * 4f - 3f);
        }
    }

    public void SaveToCurrentState() {
        DataManager.dataManager.data.currentState.envelopeAttack = normalizedADSR.attack;
        DataManager.dataManager.data.currentState.envelopeDecay = normalizedADSR.decay;
        DataManager.dataManager.data.currentState.envelopeSustain = normalizedADSR.sustain;
        DataManager.dataManager.data.currentState.envelopeRelease = normalizedADSR.release;
    }

    public void LoadFromCurrentState() {
        normalizedADSR.attack = DataManager.dataManager.data.currentState.envelopeAttack;
        normalizedADSR.decay = DataManager.dataManager.data.currentState.envelopeDecay;
        normalizedADSR.sustain = DataManager.dataManager.data.currentState.envelopeSustain;
        normalizedADSR.release = DataManager.dataManager.data.currentState.envelopeRelease;
    }

    void Update() {
        float attack = effectsController.GetEffectValue(EffectsController.knobEffects["Attack"]);
        if (normalizedADSR.attack != attack) {
            normalizedADSR.attack = attack;
            ADSR.attack = Mathf.Pow(10, attack * 3.699f - 3f);
        }

        float decay = effectsController.GetEffectValue(EffectsController.knobEffects["Decay"]);
        if (normalizedADSR.decay != decay) {
            normalizedADSR.decay = decay;
            ADSR.decay = Mathf.Pow(10, decay * 3f - 2f);
        }

        float sustain = effectsController.GetEffectValue(EffectsController.knobEffects["Sustain"]);
        if (ADSR.sustain != sustain) {
            ADSR.sustain = sustain;
        }

        float release = effectsController.GetEffectValue(EffectsController.knobEffects["Release"]);
        if (normalizedADSR.release != release) {
            normalizedADSR.release = release;
            ADSR.release = Mathf.Pow(10, release * 4f - 3f);
        }
    }
}