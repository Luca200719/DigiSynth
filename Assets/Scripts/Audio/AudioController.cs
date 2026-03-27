using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public struct LFO {
    AudioController.Wave _wave;
    public float phase;
    public float frequency;
    public float increment;

    public LFO(AudioController.Wave wave, float freq, float sampleRate) {
        _wave = wave;
        phase = 0f;
        frequency = freq;
        increment = freq * 2f * Mathf.PI / sampleRate;
    }

    public void setType(AudioController.Wave type) => _wave = type;

    public void setFrequency(float freq, float sampleRate) {
        frequency = freq;
        increment = freq * 2f * Mathf.PI / sampleRate;
    }

    public float calculate() {
        float value = _wave.Invoke(phase);
        phase += increment;
        if (phase >= 2f * Mathf.PI) {
            phase -= 2f * Mathf.PI;
        }
        return value;
    }
}

public struct BiquadFilter {
    public float a0, a1, a2, b1, b2;
    public float x1, x2, y1, y2;

    public void SetHighPass(float frequency, float sampleRate, float q = 0.707f) {
        float w = 2f * Mathf.PI * frequency / sampleRate;
        float cosw = Mathf.Cos(w);
        float sinw = Mathf.Sin(w);
        float alpha = sinw / (2f * q);
        float norm = 1f / (1f + alpha);

        a0 = ((1f + cosw) / 2f) * norm;
        a1 = -(1f + cosw) * norm;
        a2 = ((1f + cosw) / 2f) * norm;
        b1 = (-2f * cosw) * norm;
        b2 = (1f - alpha) * norm;
    }

    public void SetLowPass(float frequency, float sampleRate, float q = 0.707f) {
        float w = 2f * Mathf.PI * frequency / sampleRate;
        float cosw = Mathf.Cos(w);
        float sinw = Mathf.Sin(w);
        float alpha = sinw / (2f * q);
        float norm = 1f / (1f + alpha);

        a0 = ((1f - cosw) / 2f) * norm;
        a1 = (1f - cosw) * norm;
        a2 = ((1f - cosw) / 2f) * norm;
        b1 = (-2f * cosw) * norm;
        b2 = (1f - alpha) * norm;
    }

    public float Process(float input) {
        float output = input * a0 + x1 * a1 + x2 * a2 - y1 * b1 - y2 * b2;
        x2 = x1; x1 = input; y2 = y1; y1 = output;
        return output;
    }

    public void Reset() {
        x1 = x2 = y1 = y2 = 0f;
    }
}

public struct ParametricEQ {
    public BiquadFilter lowShelf;
    public BiquadFilter lowMidPeak;
    public BiquadFilter highMidPeak;
    public BiquadFilter highShelf;

    public void Initialize(float sampleRate) {
        lowShelf = new BiquadFilter();
        lowMidPeak = new BiquadFilter();
        highMidPeak = new BiquadFilter();
        highShelf = new BiquadFilter();

        SetShelf(ref lowShelf, 100f, sampleRate, 0.707f, 0f, true);
        SetPeaking(ref lowMidPeak, 400f, sampleRate, 1.0f, 0f);
        SetPeaking(ref highMidPeak, 2000f, sampleRate, 1.0f, 0f);
        SetShelf(ref highShelf, 8000f, sampleRate, 0.707f, 0f, false);
    }

    public void SetBandGainAndQ(int band, float gainValue, float sampleRate, float q) {
        float gainDB = (gainValue - 0.5f) * 24f;

        switch (band) {
            case 0: SetShelf(ref lowShelf, 100f, sampleRate, q, gainDB, true); break;
            case 1: SetPeaking(ref lowMidPeak, 400f, sampleRate, q, gainDB); break;
            case 2: SetPeaking(ref highMidPeak, 2000f, sampleRate, q, gainDB); break;
            case 3: SetShelf(ref highShelf, 8000f, sampleRate, q, gainDB, false); break;
        }
    }

    void SetShelf(ref BiquadFilter filter, float frequency, float sampleRate, float q, float gainDB, bool isLowShelf) {
        frequency = Mathf.Clamp(frequency, 20f, sampleRate * 0.45f);
        gainDB = Mathf.Clamp(gainDB, -24f, 24f);
        q = Mathf.Clamp(q, 0.1f, 10f);

        float A = Mathf.Pow(10f, gainDB / 40f);
        float w = 2f * Mathf.PI * frequency / sampleRate;
        float cosw = Mathf.Cos(w);
        float sinw = Mathf.Sin(w);
        float alpha = sinw / (2f * q);
        float beta = Mathf.Sqrt(A) / q;

        if (isLowShelf) {
            float norm = 1f / ((A + 1f) + (A - 1f) * cosw + beta * sinw);
            if (float.IsNaN(norm) || float.IsInfinity(norm) || Mathf.Abs(norm) > 100f) return;

            filter.a0 = (A * ((A + 1f) - (A - 1f) * cosw + beta * sinw)) * norm;
            filter.a1 = (2f * A * ((A - 1f) - (A + 1f) * cosw)) * norm;
            filter.a2 = (A * ((A + 1f) - (A - 1f) * cosw - beta * sinw)) * norm;
            filter.b1 = (-2f * ((A - 1f) + (A + 1f) * cosw)) * norm;
            filter.b2 = ((A + 1f) + (A - 1f) * cosw - beta * sinw) * norm;
        } else {
            float norm = 1f / ((A + 1f) - (A - 1f) * cosw + beta * sinw);
            if (float.IsNaN(norm) || float.IsInfinity(norm) || Mathf.Abs(norm) > 100f) return;

            filter.a0 = (A * ((A + 1f) + (A - 1f) * cosw + beta * sinw)) * norm;
            filter.a1 = (-2f * A * ((A - 1f) + (A + 1f) * cosw)) * norm;
            filter.a2 = (A * ((A + 1f) + (A - 1f) * cosw - beta * sinw)) * norm;
            filter.b1 = (2f * ((A - 1f) - (A + 1f) * cosw)) * norm;
            filter.b2 = ((A + 1f) - (A - 1f) * cosw - beta * sinw) * norm;
        }
    }

    void SetPeaking(ref BiquadFilter filter, float frequency, float sampleRate, float q, float gainDB) {
        frequency = Mathf.Clamp(frequency, 20f, sampleRate * 0.45f);
        gainDB = Mathf.Clamp(gainDB, -24f, 24f);
        q = Mathf.Clamp(q, 0.1f, 20f);

        float A = Mathf.Pow(10f, gainDB / 40f);
        float w = 2f * Mathf.PI * frequency / sampleRate;
        float cosw = Mathf.Cos(w);
        float sinw = Mathf.Sin(w);
        float alpha = sinw / (2f * q);
        float norm = 1f / (1f + alpha / A);

        if (float.IsNaN(norm) || float.IsInfinity(norm) || Mathf.Abs(norm) > 100f) return;

        filter.a0 = (1f + alpha * A) * norm;
        filter.a1 = (-2f * cosw) * norm;
        filter.a2 = (1f - alpha * A) * norm;
        filter.b1 = (-2f * cosw) * norm;
        filter.b2 = (1f - alpha / A) * norm;
    }

    public float Process(float input) {
        float output = lowShelf.Process(input);
        output = lowMidPeak.Process(output);
        output = highMidPeak.Process(output);
        return highShelf.Process(output);
    }

    public void Reset() {
        lowShelf.Reset();
        lowMidPeak.Reset();
        highMidPeak.Reset();
        highShelf.Reset();
    }
}

public struct WarmthFilter {
    public float y1;
    public float cutoff;

    public WarmthFilter(float cutoffFreq, float sampleRate) {
        y1 = 0f;
        cutoff = Mathf.Exp(-2f * Mathf.PI * cutoffFreq / sampleRate);
    }

    public float Process(float input) {
        y1 = input * (1f - cutoff) + y1 * cutoff;
        return y1;
    }
}

public struct NoteUpdate {
    public int operation;
    public Note note;
    public Key key;
}

public class AudioController : MonoBehaviour {
    KeyboardController keyboardController;
    EffectsController effectsController;
    EQOverlay eqOverlay;

    float _gain = .1f;
    public int oversamplingFactor = 4;

    public delegate float Wave(float phase);

    const int WaveTableSize = 65536;
    const int MaxNotes = 64;
    const int UpdateBufferSize = 256;
    const int MaxOscillators = 4;
    const int MODULATION_BLOCK_SIZE = 4;
    const int EFFECT_UPDATE_INTERVAL = 4;

    NativeArray<float> _sineWaveTable;
    NativeArray<float> _triangleWaveTable;
    NativeArray<float> _squareWaveTable;
    NativeArray<float> _sawWaveTable;
    NativeArray<float> _noiseWaveTable;

    NativeArray<Note> audioNotes;
    NativeArray<bool> audioNotesActive;
    NativeArray<int> activeNoteIndices;
    int activeNoteCount = 0;
    NativeArray<NoteUpdate> updateBuffer;
    volatile int updateWriteIndex = 0;
    int updateReadIndex = 0;

    public bool[] oscillatorEnabled = new bool[MaxOscillators];
    public int[] oscillatorWaveType = new int[MaxOscillators];
    public float[] oscillatorLevel = new float[MaxOscillators];
    float totalOscillatorLevel = 1f;

    ParametricEQ parametricEQ;
    WarmthFilter warmthFilter;
    float sampleRate;

    float smoothedVolumeModulator = 1f;
    float smoothedPitchModulator = 1f;
    float smoothedDetuneModulator = 1f;
    float smoothedTremoloModulator = 1f;
    float smoothedVibratoModulator = 1f;
    const float MODULATOR_SMOOTHING = 0.5f;

    float cachedVibratoLFO = 0f;
    float cachedTremoloLFO = 0f;
    float cachedChorusLFO = 0f;
    float cachedPhaserLFO = 0f;
    float cachedDelayLFO = 0f;
    float cachedReverbLFO = 0f;
    float cachedDriveLFO = 0f;

    float cachedVolume = 0f;
    float cachedPitch = 0f;
    float cachedDetune = 0f;
    float cachedMix = 0f;
    float cachedTremolo = 0f;
    float cachedVibrato = 0f;
    float cachedDrive = 0f;
    float cachedDistortion = 0f;
    float cachedDriveTone = 0f;
    float cachedSaturation = 0f;
    float cachedSaturationCharacter = 0f;
    float cachedSaturationWarmth = 0f;
    float cachedChorus = 0f;
    float cachedChorusDelayTime = 0f;
    float cachedPhaser = 0f;
    float cachedPhaserFeedback = 0f;
    float cachedDelay = 0f;
    float cachedDelayTime = 0f;
    float cachedDelayFeedback = 0f;
    float cachedDelayTone = 0f;
    float cachedReverb = 0f;
    float cachedReverbSize = 0f;
    float cachedReverbDecay = 0f;
    float cachedReverbDamping = 0f;
    float cachedCompression = 0f;

    float[] chorusDelayBuffer;
    int chorusBufferSize = 2048;
    int chorusWriteIndex = 0;

    float[] phaserDelayBuffer;
    int phaserBufferSize = 512;
    int phaserWriteIndex = 0;
    float phaserFeedback = 0f;

    float[] delayBuffer;
    int delayBufferSize = 88200;
    int delayWriteIndex = 0;
    float delayFeedbackSample = 0f;
    float delayToneLPF = 0f;

    float[] reverbBuffer;
    int reverbBufferSize = 65536;
    int reverbWriteIndex = 0;
    float[] reverbTaps = { 0.1f, 0.3f, 0.5f, 0.7f };
    float[] reverbGains = { 0.7f, 0.5f, 0.3f, 0.2f };

    float compressorEnvelope = 0f;
    float compressorGainReduction = 1f;

    float lastEQLow = 0.5f;
    float lastEQLowMid = 0.5f;
    float lastEQHighMid = 0.5f;
    float lastEQHigh = 0.5f;
    float lastEQResonance = 0.5f;
    int eqUpdateCounter = 0;
    const int EQ_UPDATE_INTERVAL = 64;

    float[] waveformBuffer;
    int waveformBufferSize = 2048;
    int waveformWriteIndex = 0;
    int waveformUpdateCounter = 0;
    const int WAVEFORM_UPDATE_INTERVAL = 32;

    float currentWaveformScale = 1f;
    int waveformScaleUpdateCounter = 0;
    const int WAVEFORM_SCALE_UPDATE_INTERVAL = 128;

    float[] spectrumBuffer;
    int spectrumBufferSize = 1024;
    int spectrumWriteIndex = 0;
    int spectrumUpdateCounter = 0;

    float[] frequencyBands;
    int spectrumBands = 32;

    float currentPeakLevel = 0f;
    const float PEAK_DECAY = 0.99f;

    public enum DisplayMode { Waveform, SpectrumAnalyzer }
    public DisplayMode currentDisplayMode = DisplayMode.Waveform;
    GameObject waveformDisplay;
    GameObject spectrumAnalyzerDisplay;

    float eqOverrideTimer = 0f;
    const float EQ_OVERRIDE_DURATION = 3f;
    DisplayMode previousDisplayMode;
    public bool isEQOverrideActive;
    float eqInitializeCount = 5;

    bool needsDisplayUpdate = false;
    DisplayMode pendingDisplayMode;

    bool hasPendingToggle = false;
    DisplayMode pendingToggleMode;

    bool isVibratoActive, isTremoloActive, isChorusActive, isPhaserActive;
    bool isDelayActive, isReverbActive, isDriveActive, isSaturationActive, isCompressionActive;

    float GetWaveTableValue(NativeArray<float> table, float phase) {
        float normalizedPhase = phase * 0.15915494309189535f;
        int index = (int)(normalizedPhase * WaveTableSize) & (WaveTableSize - 1);
        return table[index];
    }

    public static float Off(float phase) => 0;
    public float SineWaveLUT(float phase) => GetWaveTableValue(_sineWaveTable, phase);
    public float TriangleWaveLUT(float phase) => GetWaveTableValue(_triangleWaveTable, phase);
    public float SquareWaveLUT(float phase) => GetWaveTableValue(_squareWaveTable, phase);
    public float SawWaveLUT(float phase) => GetWaveTableValue(_sawWaveTable, phase);
    public float NoiseWaveLUT(float phase) => GetWaveTableValue(_noiseWaveTable, phase);

    public LFO vibratoLFO;
    public LFO tremoloLFO;
    public LFO chorusLFO;
    public LFO phaserLFO;
    public LFO delayLFO;
    public LFO reverbLFO;
    public LFO driveLFO;

    void InitializeEffectBuffers() {
        chorusDelayBuffer = new float[chorusBufferSize];
        phaserDelayBuffer = new float[phaserBufferSize];
        delayBuffer = new float[delayBufferSize];
        reverbBuffer = new float[reverbBufferSize];
    }

    void UpdateActiveNotesList() {
        activeNoteCount = 0;
        for (int i = 0; i < MaxNotes; i++) {
            if (audioNotesActive[i]) {
                activeNoteIndices[activeNoteCount++] = i;
            }
        }
    }

    void RecalculateTotalOscillatorLevel() {
        totalOscillatorLevel = 0f;
        for (int i = 0; i < MaxOscillators; i++) {
            if (oscillatorEnabled[i]) {
                totalOscillatorLevel += oscillatorLevel[i];
            }
        }
        if (totalOscillatorLevel <= 0f) totalOscillatorLevel = 1f;
    }

    void CacheEffectValues() {
        cachedVolume = effectsController.GetEffectValue(EffectsController.knobEffects["Volume"]);
        cachedPitch = 0.9f + ((1f - effectsController.GetEffectValue(EffectsController.knobEffects["Pitch"])) * 0.2f);
        cachedDetune = 1f + (((1f - effectsController.GetEffectValue(EffectsController.knobEffects["Detune"])) - 0.5f) * 0.05776f);
        cachedMix = effectsController.GetEffectValue(EffectsController.knobEffects["Mix"]);
        cachedTremolo = effectsController.GetEffectValue(EffectsController.knobEffects["Tremolo"]);
        cachedVibrato = effectsController.GetEffectValue(EffectsController.knobEffects["Vibrato"]);
        cachedDrive = effectsController.GetEffectValue(EffectsController.knobEffects["Overdrive"]);
        cachedDistortion = effectsController.GetEffectValue(EffectsController.knobEffects["Distortion"]);
        cachedDriveTone = effectsController.GetEffectValue(EffectsController.knobEffects["DriveTone"]);
        cachedSaturation = effectsController.GetEffectValue(EffectsController.knobEffects["Saturation"]);
        cachedSaturationCharacter = effectsController.GetEffectValue(EffectsController.knobEffects["SaturationCharacter"]);
        cachedSaturationWarmth = effectsController.GetEffectValue(EffectsController.knobEffects["SaturationWarmth"]);
        cachedChorus = effectsController.GetEffectValue(EffectsController.knobEffects["Chorus"]);
        cachedChorusDelayTime = effectsController.GetEffectValue(EffectsController.knobEffects["ChorusDelayTime"]);
        cachedPhaser = effectsController.GetEffectValue(EffectsController.knobEffects["Phaser"]);
        cachedPhaserFeedback = effectsController.GetEffectValue(EffectsController.knobEffects["PhaserFeedback"]);
        cachedDelay = effectsController.GetEffectValue(EffectsController.knobEffects["Delay"]);
        cachedDelayTime = effectsController.GetEffectValue(EffectsController.knobEffects["DelayTime"]);
        cachedDelayFeedback = effectsController.GetEffectValue(EffectsController.knobEffects["DelayFeedback"]);
        cachedDelayTone = effectsController.GetEffectValue(EffectsController.knobEffects["DelayTone"]);
        cachedReverb = effectsController.GetEffectValue(EffectsController.knobEffects["Reverb"]);
        cachedReverbSize = effectsController.GetEffectValue(EffectsController.knobEffects["ReverbSize"]);
        cachedReverbDecay = effectsController.GetEffectValue(EffectsController.knobEffects["ReverbDecay"]);
        cachedReverbDamping = effectsController.GetEffectValue(EffectsController.knobEffects["ReverbDamping"]);
        cachedCompression = effectsController.GetEffectValue(EffectsController.knobEffects["Compression"]);
    }

    float ProcessDrive(float input) {
        if (cachedDrive < 0.001f && cachedDistortion < 0.001f) return input;

        float processedSample = input;
        float lfoModulation = 1f + (cachedDriveLFO * 0.3f);

        if (cachedDrive > 0.001f) {
            float drive = 1f + (cachedDrive * lfoModulation) * 4f;
            float driven = input * drive;
            float clipped = math.tanh(driven * 0.7f) * 1.4f;
            processedSample = input * (1f - cachedDrive * 0.8f) + clipped * cachedDrive * 0.8f;
        }

        if (cachedDistortion > 0.001f) {
            float drive = 1f + (cachedDistortion * lfoModulation) * 8f;
            float driven = processedSample * drive;
            float clipped = math.clamp(driven, -0.6f, 0.6f);
            if (driven > 0.6f) clipped = 0.6f + (driven - 0.6f) * 0.1f;
            else if (driven < -0.6f) clipped = -0.6f + (driven + 0.6f) * 0.1f;

            clipped += clipped * clipped * clipped * 0.1f;
            processedSample = clipped;
        }

        if (cachedDriveTone != 0.5f) {
            float toneAmount = (cachedDriveTone - 0.5f) * 2f;
            processedSample += processedSample * processedSample * processedSample * 0.1f * toneAmount;
        }

        return processedSample;
    }

    float ProcessSaturation(float input) {
        if (cachedSaturation < 0.001f) return input;

        float driven = input * (1f + cachedSaturation * 2f);
        float asymmetry = cachedSaturationCharacter * 0.3f;
        driven *= (driven > 0f) ? (1f + asymmetry) : (1f - asymmetry * 0.5f);

        float saturated = math.tanh(driven * 0.8f) * 1.25f;

        if (cachedSaturationWarmth > 0.01f) {
            saturated = warmthFilter.Process(saturated) * cachedSaturationWarmth + saturated * (1f - cachedSaturationWarmth);
        }

        return input * (1f - cachedSaturation * 0.6f) + saturated * cachedSaturation * 0.6f;
    }

    float ProcessEQ(float input) {
        if (++eqUpdateCounter >= EQ_UPDATE_INTERVAL) {
            eqUpdateCounter = 0;

            float lowGain = effectsController.GetEffectValue(EffectsController.knobEffects["EQLow"]);
            float lowMidGain = effectsController.GetEffectValue(EffectsController.knobEffects["EQLowMid"]);
            float highMidGain = effectsController.GetEffectValue(EffectsController.knobEffects["EQHighMid"]);
            float highGain = effectsController.GetEffectValue(EffectsController.knobEffects["EQHigh"]);
            float resonance = effectsController.GetEffectValue(EffectsController.knobEffects["EQResonance"]);

            const float threshold = 0.01f;
            bool needsUpdate =
                Mathf.Abs(lowGain - lastEQLow) > threshold ||
                Mathf.Abs(lowMidGain - lastEQLowMid) > threshold ||
                Mathf.Abs(highMidGain - lastEQHighMid) > threshold ||
                Mathf.Abs(highGain - lastEQHigh) > threshold ||
                Mathf.Abs(resonance - lastEQResonance) > threshold;

            if (needsUpdate) {
                lastEQLow = lowGain;
                lastEQLowMid = lowMidGain;
                lastEQHighMid = highMidGain;
                lastEQHigh = highGain;
                lastEQResonance = resonance;

                if (eqInitializeCount > 0) {
                    eqInitializeCount--;
                } else {
                    TriggerEQOverride();
                }

                float shelfQ = Mathf.Clamp(0.5f + (resonance * 1.0f), 0.3f, 3.0f);
                float peakQ = Mathf.Clamp(0.5f + (resonance * 4.5f), 0.3f, 10.0f);

                parametricEQ.SetBandGainAndQ(0, lowGain, sampleRate, shelfQ);
                parametricEQ.SetBandGainAndQ(1, lowMidGain, sampleRate, peakQ);
                parametricEQ.SetBandGainAndQ(2, highMidGain, sampleRate, peakQ);
                parametricEQ.SetBandGainAndQ(3, highGain, sampleRate, shelfQ);
            }
        }

        return parametricEQ.Process(input);
    }

    float ProcessChorus(float input) {
        if (cachedChorus < 0.001f) return input;

        float baseDelay = 0.015f + (cachedChorusDelayTime * 0.015f);
        float delayTime = baseDelay + (cachedChorusLFO * 0.005f * cachedChorus);

        int delaySamples = Mathf.Clamp((int)(delayTime * sampleRate), 1, chorusBufferSize - 1);

        chorusDelayBuffer[chorusWriteIndex] = input;
        int readIndex = (chorusWriteIndex - delaySamples + chorusBufferSize) % chorusBufferSize;
        float delayedSample = chorusDelayBuffer[readIndex];
        chorusWriteIndex = (chorusWriteIndex + 1) % chorusBufferSize;

        float wetLevel = cachedChorus * 0.5f;
        return input * (1f - wetLevel) + delayedSample * wetLevel;
    }

    float ProcessCompression(float input) {
        if (cachedCompression < 0.001f) return input;

        float inputLevel = Mathf.Abs(input);
        float threshold = 0.3f + (cachedCompression * 0.4f);
        float ratio = 1f + (cachedCompression * 6f);
        float attack = 0.001f + (cachedCompression * 0.01f);
        float release = (0.05f + (cachedCompression * 0.1f)) / sampleRate;

        compressorEnvelope += (inputLevel - compressorEnvelope) * (inputLevel > compressorEnvelope ? attack : release);

        float gainReduction = 1f;
        if (compressorEnvelope > threshold) {
            float overThreshold = compressorEnvelope - threshold;
            gainReduction = (threshold + overThreshold / ratio) / compressorEnvelope;
        }

        compressorGainReduction = compressorGainReduction * 0.99f + gainReduction * 0.01f;

        float compressedSample = input * compressorGainReduction * (1f + cachedCompression * 1.5f);
        return input * (1f - cachedCompression) + compressedSample * cachedCompression;
    }

    float ProcessPhaser(float input) {
        if (cachedPhaser < 0.001f) return input;

        float delayTime = 0.001f + (cachedPhaserLFO * 0.002f * cachedPhaser);
        int delaySamples = Mathf.Clamp((int)(delayTime * sampleRate), 1, phaserBufferSize - 1);

        phaserDelayBuffer[phaserWriteIndex] = input + (phaserFeedback * cachedPhaserFeedback * 0.4f);
        int readIndex = (phaserWriteIndex - delaySamples + phaserBufferSize) % phaserBufferSize;
        float delayedSample = phaserDelayBuffer[readIndex];

        phaserFeedback = delayedSample;
        phaserWriteIndex = (phaserWriteIndex + 1) % phaserBufferSize;

        float wetLevel = cachedPhaser * 0.5f;
        return input * (1f - wetLevel) + (input + delayedSample) * wetLevel;
    }

    float ProcessDelay(float input) {
        if (cachedDelay < 0.001f) return input;

        float modulatedDelayMs = (50f + cachedDelayTime * 1950f) * (1f + cachedDelayLFO * 0.05f);
        int delaySamples = Mathf.Clamp((int)((modulatedDelayMs / 1000f) * sampleRate), 1, delayBufferSize - 1);

        delayBuffer[delayWriteIndex] = input + (delayFeedbackSample * cachedDelayFeedback * 0.7f);

        int readIndex = (delayWriteIndex - delaySamples + delayBufferSize) % delayBufferSize;
        float delayedSample = delayBuffer[readIndex];

        if (cachedDelayTone < 0.99f) {
            float cutoff = 0.05f + (cachedDelayTone * 0.4f);
            delayToneLPF = delayedSample * cutoff + delayToneLPF * (1f - cutoff);
            delayedSample = delayToneLPF;
        }

        delayFeedbackSample = delayedSample;
        delayWriteIndex = (delayWriteIndex + 1) % delayBufferSize;

        return input * (1f - cachedDelay) + delayedSample * cachedDelay;
    }

    float ProcessReverb(float input) {
        if (cachedReverb < 0.001f) return input;

        float modulatedRoomScale = (0.2f + cachedReverbSize * 0.8f) * (1f + cachedReverbLFO * 0.1f);

        reverbBuffer[reverbWriteIndex] = input;

        float reverbSum = 0f;
        for (int tap = 0; tap < 4; tap++) {
            int tapSamples = Mathf.Clamp((int)(reverbTaps[tap] * modulatedRoomScale * reverbBufferSize), 1, reverbBufferSize - 1);
            int readIndex = (reverbWriteIndex - tapSamples + reverbBufferSize) % reverbBufferSize;
            float tapSample = reverbBuffer[readIndex];

            float tapGain = reverbGains[tap] * Mathf.Pow(cachedReverbDecay, tap + 1);
            if (cachedReverbDamping > 0.01f) tapSample *= (1f - cachedReverbDamping * (tap + 1) * 0.2f);

            reverbSum += tapSample * tapGain;
        }

        reverbBuffer[reverbWriteIndex] += reverbSum * cachedReverbDecay * 0.25f;
        if (Mathf.Abs(reverbBuffer[reverbWriteIndex]) > 2f) reverbBuffer[reverbWriteIndex] *= 0.5f;

        reverbWriteIndex = (reverbWriteIndex + 1) % reverbBufferSize;

        return input * (1f - cachedReverb) + reverbSum * cachedReverb;
    }

    public float[] GetWaveformData() {
        float[] outputBuffer = new float[waveformBufferSize];
        for (int i = 0; i < waveformBufferSize; i++) {
            outputBuffer[i] = waveformBuffer[(waveformWriteIndex + i) % waveformBufferSize];
        }
        return outputBuffer;
    }

    float CalculateWaveformDisplayScale() {
        float totalWeight = 0f;
        float weightedScale = 0f;

        for (int osc = 0; osc < MaxOscillators; osc++) {
            if (!oscillatorEnabled[osc]) continue;

            float weight = oscillatorLevel[osc];
            float inverseScale = oscillatorWaveType[osc] switch {
                1 => 1f / 1.0f,
                2 => 1f / 0.95f,
                3 => 1f / 0.50f,
                4 => 1f / 0.71f,
                5 => 1f / 0.60f,
                _ => 1f
            };

            weightedScale += inverseScale * weight;
            totalWeight += weight;
        }

        return totalWeight > 0f ? weightedScale / totalWeight : 1f;
    }

    public float[] GetSpectrumData() {
        float[] bands = new float[32];

        if (currentDisplayMode == DisplayMode.SpectrumAnalyzer) {
            float[] unitySpectrum = new float[1024];
            AudioListener.GetSpectrumData(unitySpectrum, 0, FFTWindow.BlackmanHarris);

            int binIndex = 1;
            for (int band = 0; band < 32; band++) {
                int binCount = Mathf.Max(1, (int)Mathf.Pow(2, band * 0.14f));
                float sum = 0f;

                for (int bin = 0; bin < binCount && binIndex < 512; bin++) {
                    sum += unitySpectrum[binIndex++];
                }

                bands[band] = sum / binCount;
                if (binIndex >= 512) break;
            }
        }

        return bands;
    }

    void UpdateFrequencyBands() {
        const float logMin = 1.30103f; // log10(20)
        const float logMax = 4.30103f; // log10(20000)
        const float logRange = logMax - logMin;

        for (int band = 0; band < spectrumBands; band++) {
            float logStart = logMin + (band / (float)spectrumBands) * logRange;
            float logEnd = logMin + ((band + 1) / (float)spectrumBands) * logRange;

            float freqStart = Mathf.Pow(10f, logStart);
            float freqEnd = Mathf.Pow(10f, logEnd);

            int startSample = Mathf.Clamp(Mathf.FloorToInt(freqStart / (22050f) * spectrumBufferSize), 0, spectrumBufferSize - 1);
            int endSample = Mathf.Clamp(Mathf.FloorToInt(freqEnd / (22050f) * spectrumBufferSize), startSample + 1, spectrumBufferSize);

            float bandEnergy = 0f;
            for (int i = startSample; i < endSample; i++) {
                bandEnergy += spectrumBuffer[i] * spectrumBuffer[i];
            }

            int sampleCount = endSample - startSample;
            frequencyBands[band] = (sampleCount > 0) ? Mathf.Sqrt(bandEnergy / sampleCount) : 0f;
        }
    }

    public DisplayMode GetDisplayMode() {
        if (isEQOverrideActive) {
            return hasPendingToggle ? pendingToggleMode : previousDisplayMode;
        }
        return currentDisplayMode;
    }

    public void SetDisplayMode(DisplayMode mode) {
        if (isEQOverrideActive) {
            hasPendingToggle = true;
            pendingToggleMode = mode;
            return;
        }

        if (currentDisplayMode != mode) {
            waveformUpdateCounter = 0;
            waveformScaleUpdateCounter = 0;
            spectrumUpdateCounter = 0;
            currentDisplayMode = mode;
            ClearInactiveBuffers();
            UpdateDisplayMaterial();
        }
    }

    void ClearInactiveBuffers() {
        if (currentDisplayMode == DisplayMode.Waveform) {
            System.Array.Clear(spectrumBuffer, 0, spectrumBufferSize);
            System.Array.Clear(frequencyBands, 0, spectrumBands);
            spectrumWriteIndex = 0;
        } else {
            System.Array.Clear(waveformBuffer, 0, waveformBufferSize);
            waveformWriteIndex = 0;
        }
    }

    public void TriggerEQOverride() {
        if (currentDisplayMode != DisplayMode.SpectrumAnalyzer) {
            previousDisplayMode = currentDisplayMode;
            currentDisplayMode = DisplayMode.SpectrumAnalyzer;
            isEQOverrideActive = true;
            ClearInactiveBuffers();
            UpdateDisplayMaterial();
        }
        eqOverrideTimer = EQ_OVERRIDE_DURATION;
        eqOverlay.LightEQ();
    }

    void EndEQOverride() {
        isEQOverrideActive = false;

        if (hasPendingToggle) {
            currentDisplayMode = pendingToggleMode;
            hasPendingToggle = false;
        } else {
            currentDisplayMode = previousDisplayMode;
        }

        ClearInactiveBuffers();
        UpdateDisplayMaterial();
    }

    void UpdateDisplayMaterial() {
        needsDisplayUpdate = true;
        pendingDisplayMode = currentDisplayMode;
    }

    public float GetCurrentPeakLevel() {
        return currentPeakLevel;
    }

    void Awake() {
        ObjectRegistry.registry.Register(gameObject, "Audio");

        _sineWaveTable = new NativeArray<float>(WaveTableSize, Allocator.Persistent);
        _triangleWaveTable = new NativeArray<float>(WaveTableSize, Allocator.Persistent);
        _squareWaveTable = new NativeArray<float>(WaveTableSize, Allocator.Persistent);
        _sawWaveTable = new NativeArray<float>(WaveTableSize, Allocator.Persistent);
        _noiseWaveTable = new NativeArray<float>(WaveTableSize, Allocator.Persistent);

        audioNotes = new NativeArray<Note>(MaxNotes, Allocator.Persistent);
        audioNotesActive = new NativeArray<bool>(MaxNotes, Allocator.Persistent);
        activeNoteIndices = new NativeArray<int>(MaxNotes, Allocator.Persistent);
        updateBuffer = new NativeArray<NoteUpdate>(UpdateBufferSize, Allocator.Persistent);

        const float twoPi = 6.28318530718f;
        const float invWaveTableSize = 1f / WaveTableSize;
        const float invTwoPi = 0.15915494309189535f;

        for (int i = 0; i < WaveTableSize; i++) {
            float phase = i * invWaveTableSize * twoPi;
            float normalizedPhase = phase * invTwoPi;

            _sineWaveTable[i] = Mathf.Sin(phase);

            float triangleValue = (normalizedPhase < 0.25f) ? 4 * normalizedPhase : (normalizedPhase < 0.75f) ? 2 - 4 * normalizedPhase : 4 * normalizedPhase - 4;
            _triangleWaveTable[i] = triangleValue * 0.95f;

            _squareWaveTable[i] = ((phase < Mathf.PI) ? 0.5f : -0.5f);
            _sawWaveTable[i] = ((phase / Mathf.PI) - 1f) * 0.71f;
            _noiseWaveTable[i] = UnityEngine.Random.Range(-0.6f, 0.6f);
        }
    }

    void Start() {
        keyboardController = ObjectRegistry.registry.GetObjectList("Keyboard")[0].GetComponent<KeyboardController>();
        effectsController = ObjectRegistry.registry.GetObjectList("Effects")[0].GetComponent<EffectsController>();
        eqOverlay = ObjectRegistry.registry.GetObjectList("EQ Overlay")[0].GetComponent<EQOverlay>();

        sampleRate = AudioSettings.outputSampleRate;

        vibratoLFO = new LFO(SineWaveLUT, 5f, sampleRate);
        tremoloLFO = new LFO(SineWaveLUT, 5f, sampleRate);
        chorusLFO = new LFO(SineWaveLUT, 0.3f, sampleRate);
        phaserLFO = new LFO(SineWaveLUT, 0.4f, sampleRate);
        delayLFO = new LFO(SineWaveLUT, 0.1f, sampleRate);
        reverbLFO = new LFO(SineWaveLUT, 0.05f, sampleRate);
        driveLFO = new LFO(SineWaveLUT, 0.2f, sampleRate);

        InitializeEffectBuffers();

        parametricEQ = new ParametricEQ();
        warmthFilter = new WarmthFilter(800f, sampleRate);
        parametricEQ.Initialize(sampleRate);
        parametricEQ.Reset();

        for (int i = 0; i < MaxOscillators; i++) {
            oscillatorEnabled[i] = false;
            oscillatorWaveType[i] = 0;
            oscillatorLevel[i] = 1f;
        }

        RecalculateTotalOscillatorLevel();

        waveformBuffer = new float[waveformBufferSize];
        spectrumBuffer = new float[spectrumBufferSize];
        frequencyBands = new float[spectrumBands];

        Transform displayPanel = ObjectRegistry.registry.GetObjectList("Display Panel")[0].transform;
        waveformDisplay = displayPanel.GetChild(0).gameObject;
        spectrumAnalyzerDisplay = displayPanel.GetChild(1).gameObject;

        SetDisplayMode(DataManager.dataManager.data.displayToggleState ? DisplayMode.Waveform : DisplayMode.SpectrumAnalyzer);
    }

    public void SaveToCurrentState() {
        DataManager.dataManager.data.currentState.oscillatorEnabled = (bool[])oscillatorEnabled.Clone();
        DataManager.dataManager.data.currentState.oscillatorWaveType = (int[])oscillatorWaveType.Clone();
        DataManager.dataManager.data.currentState.oscillatorLevel = (float[])oscillatorLevel.Clone();
    }

    public void LoadFromCurrentState() {
        oscillatorEnabled = (bool[])DataManager.dataManager.data.currentState.oscillatorEnabled.Clone();
        oscillatorWaveType = (int[])DataManager.dataManager.data.currentState.oscillatorWaveType.Clone();
        oscillatorLevel = (float[])DataManager.dataManager.data.currentState.oscillatorLevel.Clone();
        RecalculateTotalOscillatorLevel();
    }

    void Update() {
        if (needsDisplayUpdate) {
            waveformDisplay.SetActive(pendingDisplayMode == DisplayMode.Waveform);
            spectrumAnalyzerDisplay.SetActive(pendingDisplayMode == DisplayMode.SpectrumAnalyzer);
            needsDisplayUpdate = false;
        }

        if (isEQOverrideActive) {
            eqOverrideTimer -= Time.deltaTime;
            if (eqOverrideTimer <= 0f) EndEQOverride();
        }
    }

    void OnDestroy() {
        if (_sineWaveTable.IsCreated) _sineWaveTable.Dispose();
        if (_triangleWaveTable.IsCreated) _triangleWaveTable.Dispose();
        if (_squareWaveTable.IsCreated) _squareWaveTable.Dispose();
        if (_sawWaveTable.IsCreated) _sawWaveTable.Dispose();
        if (_noiseWaveTable.IsCreated) _noiseWaveTable.Dispose();
        if (audioNotes.IsCreated) audioNotes.Dispose();
        if (audioNotesActive.IsCreated) audioNotesActive.Dispose();
        if (activeNoteIndices.IsCreated) activeNoteIndices.Dispose();
        if (updateBuffer.IsCreated) updateBuffer.Dispose();
    }

    public void SetOscillator(int oscillatorIndex, bool enabled, int waveType) {
        if (oscillatorIndex >= 0 && oscillatorIndex < MaxOscillators) {
            oscillatorEnabled[oscillatorIndex] = enabled;
            if (enabled && waveType >= 0 && waveType <= 5) {
                oscillatorWaveType[oscillatorIndex] = waveType;
            }
            RecalculateTotalOscillatorLevel();
        }
    }

    public void AddNote(Note note) {
        int _writeIndex = Interlocked.Increment(ref updateWriteIndex) - 1;
        int _bufferIndex = _writeIndex & (UpdateBufferSize - 1);
        updateBuffer[_bufferIndex] = new NoteUpdate { operation = 0, note = note };
    }

    public void ReleaseKey(Key key) {
        int _writeIndex = Interlocked.Increment(ref updateWriteIndex) - 1;
        int _bufferIndex = _writeIndex & (UpdateBufferSize - 1);
        updateBuffer[_bufferIndex] = new NoteUpdate { operation = 1, key = key };
    }

    void OnAudioFilterRead(float[] data, int channels) {
        int _samplesPerChannel = data.Length / channels;
        ProcessUpdates();

        unsafe {
            fixed (float* dataPtr = data) {
                ProcessAudio(dataPtr, _samplesPerChannel, channels);
            }
        }
    }

    void ProcessUpdates() {
        while (updateReadIndex != updateWriteIndex) {
            int _bufferIndex = updateReadIndex & (UpdateBufferSize - 1);
            NoteUpdate update = updateBuffer[_bufferIndex];

            if (update.operation == 0) AddNoteToAudioBuffer(update.note);
            else ReleaseKeyInAudioBuffer(update.key);

            updateReadIndex++;
        }

        CleanupFinishedNotes();
    }

    void AddNoteToAudioBuffer(Note note) {
        for (int i = 0; i < MaxNotes; i++) {
            if (!audioNotesActive[i]) {
                audioNotes[i] = note;
                audioNotesActive[i] = true;
                UpdateActiveNotesList();
                return;
            }
        }
    }

    void ReleaseKeyInAudioBuffer(Key key) {
        for (int i = 0; i < MaxNotes; i++) {
            if (audioNotesActive[i] && audioNotes[i].key == key && audioNotes[i].currentStage < 3) {
                Note note = audioNotes[i];
                note.release();
                audioNotes[i] = note;
            }
        }
    }

    void CleanupFinishedNotes() {
        for (int i = 0; i < MaxNotes; i++) {
            if (audioNotesActive[i] && audioNotes[i].isOver()) {
                audioNotesActive[i] = false;
            }
        }
        UpdateActiveNotesList();
    }

    unsafe void ProcessAudio(float* data, int samplesPerChannel, int channels) {
        const float invTwoPi = 0.15915494309189535f;
        const float twoPi = 6.28318530718f;
        const int waveTableMask = WaveTableSize - 1;
        float invOversamplingFactor = 1f / oversamplingFactor;

        float* sineTablePtr = (float*)_sineWaveTable.GetUnsafeReadOnlyPtr();
        float* triangleTablePtr = (float*)_triangleWaveTable.GetUnsafeReadOnlyPtr();
        float* squareTablePtr = (float*)_squareWaveTable.GetUnsafeReadOnlyPtr();
        float* sawTablePtr = (float*)_sawWaveTable.GetUnsafeReadOnlyPtr();
        float* noiseTablePtr = (float*)_noiseWaveTable.GetUnsafeReadOnlyPtr();

        Note* notesPtr = (Note*)audioNotes.GetUnsafePtr();

        isVibratoActive = effectsController.IsEffectActive("Vibrato");
        isTremoloActive = effectsController.IsEffectActive("Tremolo");
        isChorusActive = effectsController.IsEffectActive("Chorus");
        isPhaserActive = effectsController.IsEffectActive("Phaser");
        isDelayActive = effectsController.IsEffectActive("Delay");
        isReverbActive = effectsController.IsEffectActive("Reverb");
        isDriveActive = effectsController.IsEffectActive("Drive");
        isSaturationActive = effectsController.IsEffectActive("Saturation");
        isCompressionActive = effectsController.IsEffectActive("Compression");

        CacheEffectValues();

        int sampleCounter = 0;

        for (int s = 0; s < samplesPerChannel; s++) {
            // Update LFOs every sample
            if (isVibratoActive) cachedVibratoLFO = vibratoLFO.calculate();
            if (isTremoloActive) cachedTremoloLFO = tremoloLFO.calculate();
            if (isChorusActive) cachedChorusLFO = chorusLFO.calculate();
            if (isPhaserActive) cachedPhaserLFO = phaserLFO.calculate();
            if (isDelayActive) cachedDelayLFO = delayLFO.calculate();
            if (isReverbActive) cachedReverbLFO = reverbLFO.calculate();
            if (isDriveActive) cachedDriveLFO = driveLFO.calculate();

            if (sampleCounter % MODULATION_BLOCK_SIZE == 0) {
                smoothedVolumeModulator = math.lerp(smoothedVolumeModulator, cachedVolume, 1f - MODULATOR_SMOOTHING);
                smoothedPitchModulator = math.lerp(smoothedPitchModulator, cachedPitch, 1f - MODULATOR_SMOOTHING);
                smoothedDetuneModulator = math.lerp(smoothedDetuneModulator, cachedDetune, 1f - MODULATOR_SMOOTHING);

                float targetTremolo = cachedVolume;
                if (isTremoloActive) {
                    targetTremolo *= 1f + (cachedTremoloLFO * cachedTremolo * 0.5f);
                }
                smoothedTremoloModulator = math.lerp(smoothedTremoloModulator, targetTremolo, 1f - MODULATOR_SMOOTHING);

                float targetVibrato = 1f;
                if (isVibratoActive) {
                    targetVibrato = 1f + (cachedVibratoLFO * cachedVibrato * 0.02f);
                }
                smoothedVibratoModulator = math.lerp(smoothedVibratoModulator, targetVibrato, 1f - MODULATOR_SMOOTHING);
            }

            float _accumulatedSample = 0f;

            for (int os = 0; os < oversamplingFactor; os++) {
                float _combinedSample = 0f;

                for (int noteIdx = 0; noteIdx < activeNoteCount; noteIdx++) {
                    int noteIndex = activeNoteIndices[noteIdx];
                    Note* notePtr = &notesPtr[noteIndex];

                    if (notePtr->currentStage >= 4) continue;

                    switch (notePtr->currentStage) {
                        case 0: // Attack
                            notePtr->attackTime += invOversamplingFactor / sampleRate;
                            float attackProgress = notePtr->attackTime / notePtr->attackDuration;
                            notePtr->multiplier = 1f - Mathf.Exp(-attackProgress * 4f);
                            if (attackProgress >= 1f) {
                                notePtr->currentStage = 1;
                                notePtr->multiplier = 1f;
                            }
                            break;

                        case 1: // Decay
                            notePtr->decayTime += invOversamplingFactor / sampleRate;
                            float decayProgress = notePtr->decayTime / notePtr->decayDuration;
                            notePtr->multiplier = notePtr->sustainLevel + (1f - notePtr->sustainLevel) * Mathf.Exp(-decayProgress * 5f);
                            if (notePtr->multiplier <= notePtr->sustainLevel) {
                                notePtr->currentStage = 2;
                                notePtr->multiplier = notePtr->sustainLevel;
                            }
                            break;

                        case 3: // Release
                            notePtr->releaseTime += invOversamplingFactor / sampleRate;
                            float releaseProgress = notePtr->releaseTime / notePtr->releaseDuration;
                            notePtr->multiplier = notePtr->releaseStartLevel * Mathf.Exp(-releaseProgress * 5f);
                            if (notePtr->multiplier <= 0.001f) {
                                notePtr->multiplier = 0f;
                                notePtr->currentStage = 4;
                            }
                            break;
                    }

                    float normalizedPhase = notePtr->phase * invTwoPi;
                    int tableIndex = ((int)(normalizedPhase * WaveTableSize)) & waveTableMask;

                    float _mixedWave = 0f;

                    for (int osc = 0; osc < MaxOscillators; osc++) {
                        if (!oscillatorEnabled[osc]) continue;

                        float _oscWaveValue = oscillatorWaveType[osc] switch {
                            1 => sineTablePtr[tableIndex],
                            2 => triangleTablePtr[tableIndex],
                            3 => squareTablePtr[tableIndex],
                            4 => sawTablePtr[tableIndex],
                            5 => noiseTablePtr[tableIndex],
                            _ => 0f
                        };

                        _mixedWave += _oscWaveValue * oscillatorLevel[osc];
                    }

                    if (totalOscillatorLevel > 0f) _mixedWave /= totalOscillatorLevel;

                    _combinedSample += _mixedWave * notePtr->multiplier;

                    float modulatedIncrement = notePtr->increment * smoothedPitchModulator * smoothedDetuneModulator * smoothedVibratoModulator;
                    notePtr->phase += modulatedIncrement * invOversamplingFactor;
                    if (notePtr->phase >= twoPi) notePtr->phase -= twoPi;
                }

                _accumulatedSample += _combinedSample;
            }
            _accumulatedSample *= invOversamplingFactor;

            float _cleanSample = _accumulatedSample * _gain * smoothedVolumeModulator * smoothedTremoloModulator;
            float _processedSample = _cleanSample;

            if (isDriveActive) _processedSample = ProcessDrive(_processedSample);
            if (isSaturationActive) _processedSample = ProcessSaturation(_processedSample);

            _processedSample = ProcessEQ(_processedSample);

            if (isChorusActive) _processedSample = ProcessChorus(_processedSample);
            if (isPhaserActive) _processedSample = ProcessPhaser(_processedSample);
            if (isCompressionActive) _processedSample = ProcessCompression(_processedSample);
            if (isDelayActive) _processedSample = ProcessDelay(_processedSample);
            if (isReverbActive) _processedSample = ProcessReverb(_processedSample);

            float _finalSample = math.clamp(_cleanSample * (1f - cachedMix) + _processedSample * cachedMix, -1f, 1f);

            currentPeakLevel = Mathf.Max(Mathf.Abs(_finalSample), currentPeakLevel * PEAK_DECAY);

            if (currentDisplayMode == DisplayMode.Waveform) {
                if (++waveformUpdateCounter >= WAVEFORM_UPDATE_INTERVAL) {
                    if (++waveformScaleUpdateCounter >= WAVEFORM_SCALE_UPDATE_INTERVAL) {
                        currentWaveformScale = CalculateWaveformDisplayScale();
                        waveformScaleUpdateCounter = 0;
                    }

                    waveformBuffer[waveformWriteIndex] = _finalSample * currentWaveformScale;
                    waveformWriteIndex = (waveformWriteIndex + 1) % waveformBufferSize;
                    waveformUpdateCounter = 0;
                }
            } else if (++spectrumUpdateCounter >= 16) {
                spectrumBuffer[spectrumWriteIndex] = _finalSample;
                spectrumWriteIndex = (spectrumWriteIndex + 1) % spectrumBufferSize;
                if (spectrumWriteIndex % 64 == 0) UpdateFrequencyBands();
                spectrumUpdateCounter = 0;
            }

            int _sampleBaseIndex = s * channels;
            for (int channel = 0; channel < channels; channel++) {
                data[_sampleBaseIndex + channel] = _finalSample;
            }

            sampleCounter++;
        }
    }
}