using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

public partial class MotionPlayer : MotionPlayerBase
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

        [NonSerialized] MotionPlayer owner;
        [NonSerialized] int inputIndex = -1;

        internal void Init(MotionPlayer owner, PlayableGraph graph, int inputIndex, bool enable, float weight)
        {
            this.owner = owner;
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

            if (this.enable)
                clipPlayable.Play();
            else
                clipPlayable.Pause();
        }

        internal void BindRuntime(MotionPlayer owner, int inputIndex)
        {
            this.owner = owner;
            this.inputIndex = inputIndex;
        }

        internal void SetClipReference(AnimationClip value)
        {
            _clip = value;
        }

        internal void DestroyPlayable(PlayableGraph graph)
        {
            if (clipPlayable.IsValid() && graph.IsValid())
                graph.DestroyPlayable(clipPlayable);
            clipPlayable = default;
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

            if (this.enable)
                clipPlayable.Play();
            else
                clipPlayable.Pause();
        }

        public float weight { get; private set; } = 0.0f;

        public void SetWeight(float weight)
        {
            this.weight = Mathf.Clamp01(weight);
            owner?.SetStateInputWeight(inputIndex, this.weight);
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

    [SerializeField] AnimationState[] _states = new AnimationState[1];
    public IReadOnlyList<AnimationState> States { get { return _states; } }
    public int StateCount { get { return _states != null ? _states.Length : 0; } }
    public int CurStateIndex { get; private set; } = -1;
    public AnimationState CurState { get { return GetState(CurStateIndex); } }

    bool IsStateIndexInRange(int index)
    {
        return _states != null && index >= 0 && index < _states.Length;
    }

    bool IsStateIndexValid(int index)
    {
        return IsStateIndexInRange(index) && _states[index] != null;
    }

    int _GetStateIndex(string stateName)
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

    void _InitStates()
    {
        if (_states == null)
            _states = Array.Empty<AnimationState>();

        _playable.SetInputCount(_states.Length);
        CurStateIndex = -1;

        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
            if (state == null)
                continue;

            bool isDefault = CurStateIndex < 0 && state.clip != null;
            state.Init(this, _graph, i, isDefault, isDefault ? 1.0f : 0.0f);
            if (!state.IsPlayable)
                continue;

            _playable.ConnectInput(i, state.clipPlayable);
            _playable.SetInputWeight(i, state.weight);
            if (isDefault)
                CurStateIndex = i;
        }
    }

    AnimationState _GetState(int index)
    {
        return IsStateIndexValid(index) ? _states[index] : null;
    }

    int _AddState(string stateName, AnimationClip animClip, int index = -1)
    {
        AnimationState state = new AnimationState { name = stateName };
        state.SetClipReference(animClip);
        return _AddState(state, index);
    }

    int _AddState(AnimationState newState, int index = -1)
    {
        if (newState == null)
            return -1;

        Init();
        if (!_graph.IsValid())
            return -1;

        int oldLength = _states != null ? _states.Length : 0;
        if (index < 0)
            index = oldLength;
        if (index > oldLength)
            return -1;

        newState.Init(this, _graph, index, false, 0.0f);

        AnimationState[] newStates = new AnimationState[oldLength + 1];
        if (oldLength > 0) {
            Array.Copy(_states, 0, newStates, 0, index);
            Array.Copy(_states, index, newStates, index + 1, oldLength - index);
        }
        newStates[index] = newState;

        if (CurStateIndex >= index)
            CurStateIndex++;

        _states = newStates;
        ReconnectStates();
        return index;
    }

    void _RemoveState(int index)
    {
        Init();
        if (!IsStateIndexInRange(index))
            return;

        AnimationState removedState = _states[index];
        bool removedCurrent = index == CurStateIndex;

        _playable.DisconnectInputs();
        removedState?.DestroyPlayable(_graph);

        AnimationState[] newStates = new AnimationState[_states.Length - 1];
        if (index > 0)
            Array.Copy(_states, 0, newStates, 0, index);
        if (index < _states.Length - 1)
            Array.Copy(_states, index + 1, newStates, index, _states.Length - index - 1);

        _states = newStates;

        if (removedCurrent)
            CurStateIndex = FindEnabledStateIndex();
        else if (CurStateIndex > index)
            CurStateIndex--;

        ReconnectStates();
    }

    int FindEnabledStateIndex()
    {
        if (_states == null)
            return -1;

        for (int i = 0; i < _states.Length; i++) {
            if (_states[i] != null && _states[i].enable && _states[i].IsPlayable)
                return i;
        }

        return -1;
    }

    void ReconnectStates()
    {
        if (!_graph.IsValid() || _playable == null)
            return;

        _playable.DisconnectInputs();
        _playable.SetInputCount(_states != null ? _states.Length : 0);
        if (_states == null)
            return;

        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
            if (state == null)
                continue;

            state.BindRuntime(this, i);
            if (!state.IsPlayable)
                continue;

            _playable.ConnectInput(i, state.clipPlayable);
            _playable.SetInputWeight(i, state.weight);
        }
    }

    internal void SetStateInputWeight(int index, float weight)
    {
        if (_playable != null)
            _playable.SetInputWeight(index, Mathf.Clamp01(weight));
    }
}
