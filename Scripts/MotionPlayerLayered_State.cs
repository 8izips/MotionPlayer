using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

public partial class MotionPlayerLayered : MotionPlayerBase
{
    [Serializable]
    public class AnimationState
    {
        public string name;
        [SerializeField, FormerlySerializedAs("clip")] AnimationClip _clip;
        public AnimationClip clip { get { return _clip; } }

        [NonSerialized] public float time = 0.0f;
        [NonSerialized] public float duration = 0.0f;
        public float normalizedTime { get { return duration > 0.0f ? time / duration : 0.0f; } }

        public AnimationClipPlayable clipPlayable { get; private set; }
        public bool IsPlayable { get { return clipPlayable.IsValid(); } }

        [NonSerialized] MotionPlayerLayered owner;
        [NonSerialized] int layerIndex = -1;
        [NonSerialized] int inputIndex = -1;

        internal void Init(MotionPlayerLayered owner, PlayableGraph graph, int layerIndex, int inputIndex, bool enable, float weight)
        {
            this.owner = owner;
            this.layerIndex = layerIndex;
            this.inputIndex = inputIndex;
            this.enable = enable && _clip != null;
            this.weight = Mathf.Clamp01(weight);
            fading = false;
            fadeSpeed = 0.0f;
            targetWeight = this.weight;
            duration = _clip != null ? _clip.length : 0.0f;
            isLooping = _clip != null && _clip.isLooping;
            time = GetPlaybackStartTime();
            endInvoked = false;

            clipPlayable = default;
            if (_clip == null || !graph.IsValid())
                return;

            clipPlayable = AnimationClipPlayable.Create(graph, _clip);
            if (!isLooping)
                clipPlayable.SetDuration(_clip.length);

            clipPlayable.SetApplyFootIK(_applyFootIK);
            clipPlayable.SetApplyPlayableIK(_applyPlayableIK);
            clipPlayable.SetSpeed(_speed);
            clipPlayable.SetTime(time);

            if (this.enable && (owner == null || owner.IsLayerActive(layerIndex)))
                clipPlayable.Play();
            else
                clipPlayable.Pause();
        }

        internal void SetLayerActive(bool active)
        {
            if (!IsPlayable)
                return;

            if (active && enable)
                clipPlayable.Play();
            else
                clipPlayable.Pause();
        }

        internal float GetPlaybackStartTime()
        {
            return _speed < 0.0f ? duration : 0.0f;
        }

        public void SetStateTime(float stateTime)
        {
            time = stateTime;
            endInvoked = HasReachedEnd();
            if (clipPlayable.IsValid())
                clipPlayable.SetTime(stateTime);
        }

        public bool enable { get; private set; } = false;

        public void SetEnable(bool enable)
        {
            this.enable = enable && IsPlayable;
            if (this.enable && !HasReachedEnd())
                endInvoked = false;

            if (!IsPlayable)
                return;

            if (this.enable && (owner == null || owner.IsLayerActive(layerIndex)))
                clipPlayable.Play();
            else
                clipPlayable.Pause();
        }

        public float weight { get; private set; } = 0.0f;

        public void SetWeight(float weight)
        {
            this.weight = Mathf.Clamp01(weight);
            owner?.SetStateInputWeight(layerIndex, inputIndex, this.weight);
        }

        public bool fading { get; private set; } = false;
        public float fadeSpeed { get; private set; } = 0.0f;
        public float targetWeight { get; private set; } = 0.0f;

        public void ResetFade()
        {
            fading = false;
            fadeSpeed = 0.0f;
            targetWeight = weight;
        }

        public void SetFade(float targetWeight, float fadeTime)
        {
            targetWeight = Mathf.Clamp01(targetWeight);
            this.targetWeight = targetWeight;
            float diff = Mathf.Abs(weight - targetWeight);

            if (fadeTime <= 0.0f || diff <= Mathf.Epsilon) {
                fading = false;
                fadeSpeed = 0.0f;
                SetWeight(targetWeight);
                return;
            }

            fading = true;
            fadeSpeed = diff / fadeTime;
        }

        [SerializeField] float _speed = 1.0f;
        public float speed {
            get { return _speed; }
            set {
                if (Mathf.Approximately(_speed, value))
                    return;

                _speed = value;
                endInvoked = HasReachedEnd();
                if (clipPlayable.IsValid())
                    clipPlayable.SetSpeed(_speed);
            }
        }

        [SerializeField, FormerlySerializedAs("applyFootIK")] bool _applyFootIK = false;
        public bool applyFootIK {
            get { return _applyFootIK; }
            set {
                if (_applyFootIK == value)
                    return;
                _applyFootIK = value;
                if (clipPlayable.IsValid())
                    clipPlayable.SetApplyFootIK(value);
            }
        }

        [SerializeField, FormerlySerializedAs("applyPlayableIK")] bool _applyPlayableIK = false;
        public bool applyPlayableIK {
            get { return _applyPlayableIK; }
            set {
                if (_applyPlayableIK == value)
                    return;
                _applyPlayableIK = value;
                if (clipPlayable.IsValid())
                    clipPlayable.SetApplyPlayableIK(value);
            }
        }

        public bool isLooping { get; private set; } = false;

        [NonSerialized] bool endInvoked = false;
        public Action endCallback { get; private set; }

        bool HasReachedEnd()
        {
            if (isLooping || duration <= 0.0f)
                return false;
            return _speed < 0.0f ? time <= 0.0f : time >= duration;
        }

        internal bool TryInvokeEndCallback()
        {
            if (endInvoked || !HasReachedEnd())
                return false;

            endInvoked = true;
            endCallback?.Invoke();
            return true;
        }

        public void SetEndCallback(Action callback)
        {
            endCallback = callback;
        }

        public void ClearEndCallback()
        {
            endCallback = null;
        }
    }

    [Serializable]
    public class AnimationLayer
    {
        public string name = "Layer";
        [SerializeField] AvatarMask _mask;
        [SerializeField] bool _additive = false;
        [SerializeField, Range(0.0f, 1.0f)] float _weight = 0.0f;
        [SerializeField] AnimationState[] _states = new AnimationState[1];

        public AvatarMask mask { get { return _mask; } }
        public bool additive { get { return _additive; } }
        public float weight { get; private set; } = 1.0f;
        public IReadOnlyList<AnimationState> States { get { return _states; } }
        public int StateCount { get { return _states != null ? _states.Length : 0; } }
        public int CurStateIndex { get; private set; } = -1;
        public AnimationState CurState { get { return GetState(CurStateIndex); } }

        [NonSerialized] MotionPlayerLayered owner;
        [NonSerialized] int layerIndex = -1;
        [NonSerialized] AnimationMixerPlayable mixer;

        internal bool IsPlayable { get { return mixer.IsValid(); } }
        internal bool IsActive { get { return layerIndex == 0 || weight > Mathf.Epsilon; } }
        internal AnimationMixerPlayable Mixer { get { return mixer; } }

        internal void Init(MotionPlayerLayered owner, PlayableGraph graph, int layerIndex)
        {
            this.owner = owner;
            this.layerIndex = layerIndex;
            weight = layerIndex == 0 ? 1.0f : Mathf.Clamp01(_weight);
            CurStateIndex = -1;

            if (_states == null)
                _states = Array.Empty<AnimationState>();

            mixer = AnimationMixerPlayable.Create(graph, _states.Length);
            for (int i = 0; i < _states.Length; i++) {
                AnimationState state = _states[i];
                if (state == null)
                    continue;

                bool isDefault = CurStateIndex < 0 && state.clip != null;
                state.Init(owner, graph, layerIndex, i, isDefault, isDefault ? 1.0f : 0.0f);
                if (!state.IsPlayable)
                    continue;

                graph.Connect(state.clipPlayable, 0, mixer, i);
                mixer.SetInputWeight(i, state.weight);
                if (isDefault)
                    CurStateIndex = i;
            }

            ApplyActiveState();
        }

        internal void SetWeight(float value)
        {
            bool wasActive = IsActive;
            weight = layerIndex == 0 ? 1.0f : Mathf.Clamp01(value);
            owner?.SetLayerInputWeight(layerIndex, weight);
            if (wasActive != IsActive)
                ApplyActiveState();
        }

        void ApplyActiveState()
        {
            if (_states == null)
                return;

            for (int i = 0; i < _states.Length; i++)
                _states[i]?.SetLayerActive(IsActive);
        }

        internal void SetCurrentStateIndex(int stateIndex)
        {
            CurStateIndex = stateIndex;
        }

        public int GetStateIndex(string stateName)
        {
            if (_states == null)
                return -1;

            for (int i = 0; i < _states.Length; i++) {
                AnimationState state = _states[i];
                if (state != null && state.name == stateName)
                    return i;
            }

            return -1;
        }

        public AnimationState GetState(string stateName)
        {
            return GetState(GetStateIndex(stateName));
        }

        public AnimationState GetState(int stateIndex)
        {
            return _states != null && stateIndex >= 0 && stateIndex < _states.Length ? _states[stateIndex] : null;
        }
    }

    [SerializeField] AnimationLayer[] _layers = new AnimationLayer[1];
    public IReadOnlyList<AnimationLayer> Layers { get { return _layers; } }
    public int LayerCount { get { return _layers != null ? _layers.Length : 0; } }

    int _GetLayerIndex(string layerName)
    {
        if (_layers == null)
            return -1;

        for (int i = 0; i < _layers.Length; i++) {
            AnimationLayer layer = _layers[i];
            if (layer != null && layer.name == layerName)
                return i;
        }

        return -1;
    }

    AnimationLayer _GetLayer(int layerIndex)
    {
        return _layers != null && layerIndex >= 0 && layerIndex < _layers.Length ? _layers[layerIndex] : null;
    }

    internal bool IsLayerActive(int layerIndex)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        return layer != null && layer.IsActive;
    }

    internal void SetStateInputWeight(int layerIndex, int inputIndex, float weight)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        if (layer != null && layer.IsPlayable && inputIndex >= 0 && inputIndex < layer.Mixer.GetInputCount())
            layer.Mixer.SetInputWeight(inputIndex, Mathf.Clamp01(weight));
    }

    internal void SetLayerInputWeight(int layerIndex, float weight)
    {
        if (layerMixer.IsValid() && layerIndex >= 0 && layerIndex < layerMixer.GetInputCount())
            layerMixer.SetInputWeight(layerIndex, layerIndex == 0 ? 1.0f : Mathf.Clamp01(weight));
    }
}
