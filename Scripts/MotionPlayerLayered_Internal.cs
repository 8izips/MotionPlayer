using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class MotionPlayerLayered : MotionPlayerBase
{
    AnimationLayerMixerPlayable layerMixer;
    bool layersInitialized = false;

    public override void Init()
    {
        if (layersInitialized && _initialized && _graph.IsValid() && layerMixer.IsValid())
            return;

        base.Init();
        layersInitialized = false;
        if (!_graph.IsValid())
            return;

        if (_layers == null || _layers.Length == 0)
            _layers = new AnimationLayer[] { new AnimationLayer { name = "Base" } };

        layerMixer = AnimationLayerMixerPlayable.Create(_graph, _layers.Length, _layers.Length == 1);
        for (int i = 0; i < _layers.Length; i++) {
            AnimationLayer layer = _layers[i];
            if (layer == null) {
                layer = new AnimationLayer { name = i == 0 ? "Base" : "Layer " + i };
                _layers[i] = layer;
            }

            layer.Init(this, _graph, i);
            _graph.Connect(layer.Mixer, 0, layerMixer, i);
            layerMixer.SetInputWeight(i, i == 0 ? 1.0f : layer.weight);

            if (i > 0) {
                layerMixer.SetLayerAdditive((uint)i, layer.additive);
                if (layer.mask != null)
                    layerMixer.SetLayerMaskFromAvatarMask((uint)i, layer.mask);
            }
        }

        CreateAnimationOutput(layerMixer, "MotionPlayerLayered");
        layersInitialized = true;
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (_layers == null || _layers.Length == 0)
            _layers = new AnimationLayer[] { new AnimationLayer { name = "Base" } };

        for (int i = 0; i < _layers.Length; i++) {
            if (_layers[i] == null)
                _layers[i] = new AnimationLayer { name = i == 0 ? "Base" : "Layer " + i };

            AnimationLayer layer = _layers[i];
            if (string.IsNullOrEmpty(layer.name))
                layer.name = i == 0 ? "Base" : "Layer " + i;

            if (layer._states == null || layer._states.Length == 0)
                layer._states = new AnimationState[1];

            for (int s = 0; s < layer._states.Length; s++) {
                if (layer._states[s] == null)
                    layer._states[s] = new AnimationState();
            }

            if (string.IsNullOrEmpty(layer._states[0].name))
                layer._states[0].name = "Default";
        }
    }

    protected override void Tick(float elapsedTime)
    {
        if (!layersInitialized || !_graph.IsValid() || _layers == null)
            return;

        for (int l = 0; l < _layers.Length; l++) {
            AnimationLayer layer = _layers[l];
            if (layer == null || !layer.IsActive)
                continue;

            for (int i = 0; i < layer.StateCount; i++) {
                AnimationState state = layer.GetState(i);
                if (state == null || !state.IsPlayable)
                    continue;

                if (state.fading) {
                    float weight = Mathf.MoveTowards(state.weight, state.targetWeight, state.fadeSpeed * elapsedTime);
                    state.SetWeight(weight);

                    if (Mathf.Approximately(state.weight, state.targetWeight)) {
                        state.SetWeight(state.targetWeight);
                        state.ResetFade();

                        if (Mathf.Approximately(state.weight, 0.0f)) {
                            state.SetEnable(false);
                            state.SetStateTime(0.0f);
                        }
                    }
                }

                if (!state.enable)
                    continue;

                state.time = (float)state.clipPlayable.GetTime();
                if (state.TryInvokeEndCallback() && deactivateOnEnd && l == 0 && i == layer.CurStateIndex) {
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
    }

    protected override void OnStopped()
    {
        if (_layers == null)
            return;

        for (int l = 0; l < _layers.Length; l++) {
            AnimationLayer layer = _layers[l];
            if (layer == null)
                continue;

            for (int i = 0; i < layer.StateCount; i++) {
                AnimationState state = layer.GetState(i);
                if (state == null || !state.IsPlayable)
                    continue;
                state.ResetFade();
                state.SetStateTime(state.GetPlaybackStartTime());
            }
        }
    }
}
