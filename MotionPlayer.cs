using UnityEngine;

public partial class MotionPlayer : MotionPlayerBase
{
    public AnimationState GetState(string stateName)
    {
        return _GetState(_GetStateIndex(stateName));
    }

    public AnimationState GetState(int index)
    {
        return _GetState(index);
    }

    public int GetStateIndex(string stateName)
    {
        return _GetStateIndex(stateName);
    }

    public bool TryGetState(string stateName, out AnimationState state)
    {
        state = GetState(stateName);
        return state != null;
    }

    public bool TryGetState(int index, out AnimationState state)
    {
        state = GetState(index);
        return state != null;
    }

    public int AddState(string stateName, AnimationClip animClip)
    {
        return _AddState(stateName, animClip);
    }

    public void RemoveState(string stateName)
    {
        _RemoveState(_GetStateIndex(stateName));
    }

    public void RemoveState(int index)
    {
        _RemoveState(index);
    }

    public bool SetClip(string stateName, AnimationClip animClip)
    {
        return SetClip(_GetStateIndex(stateName), animClip);
    }

    public bool SetClip(int index, AnimationClip animClip)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null)
            return false;
        if (state.clip == animClip)
            return true;

        bool wasEnabled = state.enable;
        float weight = state.weight;

        _playable.DisconnectInput(index);
        state.DestroyPlayable(_graph);
        state.SetClipReference(animClip);
        state.Init(this, _graph, index, wasEnabled, weight);

        if (state.IsPlayable) {
            _playable.ConnectInput(index, state.clipPlayable);
            _playable.SetInputWeight(index, state.weight);
        } else if (CurStateIndex == index) {
            CurStateIndex = FindEnabledStateIndex();
        }

        return true;
    }

    public void Play(string stateName)
    {
        Play(_GetStateIndex(stateName), true);
    }

    public void Play(string stateName, bool restart)
    {
        Play(_GetStateIndex(stateName), restart);
    }

    public void Play(int index)
    {
        Play(index, true);
    }

    public void Play(int index, bool restart)
    {
        Init();
        AnimationState targetState = _GetState(index);
        if (targetState == null || !targetState.IsPlayable)
            return;

        if (restart)
            targetState.SetStateTime(targetState.GetPlaybackStartTime());

        CurStateIndex = index;
        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
            if (state == null || !state.IsPlayable)
                continue;

            bool isTarget = i == index;
            state.SetEnable(isTarget);
            state.SetWeight(isTarget ? 1.0f : 0.0f);
            state.ResetFade();
        }

        if (!IsPlaying())
            base.Play();
    }

    public void Play(string stateName, float normalizedTime)
    {
        Play(_GetStateIndex(stateName), normalizedTime);
    }

    public void Play(int index, float normalizedTime)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null || !state.IsPlayable)
            return;

        state.SetStateTime(state.duration * Mathf.Clamp01(normalizedTime));
        Play(index, false);
    }

    public void Resume(string stateName)
    {
        Play(_GetStateIndex(stateName), false);
    }

    public void Resume(int index)
    {
        Play(index, false);
    }

    public void CrossFade(string stateName, float fadeTime)
    {
        CrossFade(_GetStateIndex(stateName), fadeTime);
    }

    public void CrossFade(int index, float fadeTime)
    {
        Init();
        AnimationState targetState = _GetState(index);
        if (targetState == null || !targetState.IsPlayable)
            return;

        if (fadeTime <= 0.0f) {
            Play(index);
            return;
        }

        if (!targetState.enable)
            targetState.SetStateTime(targetState.GetPlaybackStartTime());

        CurStateIndex = index;
        targetState.SetEnable(true);

        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
            if (state == null || !state.IsPlayable || !state.enable)
                continue;

            float targetWeight = i == index ? 1.0f : 0.0f;
            state.SetFade(targetWeight, fadeTime);
            if (!state.fading && Mathf.Approximately(targetWeight, 0.0f)) {
                state.SetEnable(false);
                state.SetStateTime(0.0f);
            }
        }

        if (!IsPlaying())
            base.Play();
    }

    public void CrossFade(string stateName, float fadeTime, float normalizedTime)
    {
        CrossFade(_GetStateIndex(stateName), fadeTime, normalizedTime);
    }

    public void CrossFade(int index, float fadeTime, float normalizedTime)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null || !state.IsPlayable)
            return;

        CrossFade(index, fadeTime);
        state.SetStateTime(state.duration * Mathf.Clamp01(normalizedTime));
    }

    public void Blend(string stateName, float targetWeight, float fadeTime)
    {
        Blend(_GetStateIndex(stateName), targetWeight, fadeTime);
    }

    public void Blend(int index, float targetWeight, float fadeTime)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null || !state.IsPlayable)
            return;

        targetWeight = Mathf.Clamp01(targetWeight);
        if (!state.enable) {
            state.SetStateTime(state.GetPlaybackStartTime());
            state.SetEnable(true);
        }

        state.SetFade(targetWeight, fadeTime);
        if (!state.fading && Mathf.Approximately(targetWeight, 0.0f)) {
            state.SetEnable(false);
            state.SetStateTime(0.0f);
        }

        if (!IsPlaying())
            base.Play();
    }

    public void Pause(string stateName)
    {
        Pause(_GetStateIndex(stateName));
    }

    public void Pause(int index)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null || !state.IsPlayable)
            return;

        state.SetEnable(false);
        state.SetWeight(0.0f);
        state.ResetFade();
    }

    public void Stop(string stateName)
    {
        Stop(_GetStateIndex(stateName));
    }

    public void Stop(int index)
    {
        Init();
        AnimationState state = _GetState(index);
        if (state == null || !state.IsPlayable)
            return;

        state.SetEnable(false);
        state.SetWeight(0.0f);
        state.ResetFade();
        state.SetStateTime(0.0f);
    }

    public void SetTime(float time)
    {
        Init();
        if (_states == null)
            return;

        for (int i = 0; i < _states.Length; i++)
            _states[i]?.SetStateTime(time);
    }

    public void Rewind()
    {
        SetTime(0.0f);
    }

    public void Rewind(string stateName)
    {
        Rewind(_GetStateIndex(stateName));
    }

    public void Rewind(int stateIndex)
    {
        AnimationState state = _GetState(stateIndex);
        if (state != null)
            state.SetStateTime(0.0f);
    }
}
