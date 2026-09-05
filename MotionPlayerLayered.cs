using UnityEngine;

public partial class MotionPlayerLayered : MotionPlayerBase
{
    public AnimationLayer GetLayer(string layerName)
    {
        return _GetLayer(_GetLayerIndex(layerName));
    }

    public AnimationLayer GetLayer(int layerIndex)
    {
        return _GetLayer(layerIndex);
    }

    public int GetLayerIndex(string layerName)
    {
        return _GetLayerIndex(layerName);
    }

    public bool TryGetLayer(string layerName, out AnimationLayer layer)
    {
        layer = GetLayer(layerName);
        return layer != null;
    }

    public AnimationState GetState(int layerIndex, string stateName)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        return layer?.GetState(stateName);
    }

    public AnimationState GetState(int layerIndex, int stateIndex)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        return layer?.GetState(stateIndex);
    }

    public AnimationState GetState(string layerName, string stateName)
    {
        return GetState(_GetLayerIndex(layerName), stateName);
    }

    public bool SetLayerWeight(string layerName, float weight)
    {
        return SetLayerWeight(_GetLayerIndex(layerName), weight);
    }

    public bool SetLayerWeight(int layerIndex, float weight)
    {
        Init();
        AnimationLayer layer = _GetLayer(layerIndex);
        if (layer == null)
            return false;

        layer.SetWeight(layerIndex == 0 ? 1.0f : weight);
        return true;
    }

    public void Play(string stateName)
    {
        Play(0, stateName, true);
    }

    public void Play(int stateIndex)
    {
        Play(0, stateIndex, true);
    }

    public void Play(string layerName, string stateName)
    {
        Play(_GetLayerIndex(layerName), stateName, true);
    }

    public void Play(int layerIndex, string stateName)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        Play(layerIndex, layer != null ? layer.GetStateIndex(stateName) : -1, true);
    }

    public void Play(int layerIndex, int stateIndex)
    {
        Play(layerIndex, stateIndex, true);
    }

    public void Play(int layerIndex, string stateName, bool restart)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        Play(layerIndex, layer != null ? layer.GetStateIndex(stateName) : -1, restart);
    }

    public void Play(int layerIndex, int stateIndex, bool restart)
    {
        Init();
        AnimationLayer layer = _GetLayer(layerIndex);
        AnimationState targetState = layer?.GetState(stateIndex);
        if (targetState == null || !targetState.IsPlayable)
            return;

        if (restart)
            targetState.SetStateTime(targetState.GetPlaybackStartTime());

        layer.SetCurrentStateIndex(stateIndex);
        for (int i = 0; i < layer.StateCount; i++) {
            AnimationState state = layer.GetState(i);
            if (state == null || !state.IsPlayable)
                continue;

            bool isTarget = i == stateIndex;
            state.SetEnable(isTarget);
            state.SetWeight(isTarget ? 1.0f : 0.0f);
            state.ResetFade();
        }

        if (!IsPlaying())
            base.Play();
    }

    public void Play(int layerIndex, int stateIndex, float normalizedTime)
    {
        Init();
        AnimationState state = GetState(layerIndex, stateIndex);
        if (state == null || !state.IsPlayable)
            return;

        state.SetStateTime(state.duration * Mathf.Clamp01(normalizedTime));
        Play(layerIndex, stateIndex, false);
    }

    public void Resume(string stateName)
    {
        Play(0, stateName, false);
    }

    public void Resume(int layerIndex, string stateName)
    {
        Play(layerIndex, stateName, false);
    }

    public void Resume(int layerIndex, int stateIndex)
    {
        Play(layerIndex, stateIndex, false);
    }

    public void CrossFade(string stateName, float fadeTime)
    {
        CrossFade(0, stateName, fadeTime);
    }

    public void CrossFade(string layerName, string stateName, float fadeTime)
    {
        CrossFade(_GetLayerIndex(layerName), stateName, fadeTime);
    }

    public void CrossFade(int layerIndex, string stateName, float fadeTime)
    {
        AnimationLayer layer = _GetLayer(layerIndex);
        CrossFade(layerIndex, layer != null ? layer.GetStateIndex(stateName) : -1, fadeTime);
    }

    public void CrossFade(int layerIndex, int stateIndex, float fadeTime)
    {
        Init();
        AnimationLayer layer = _GetLayer(layerIndex);
        AnimationState targetState = layer?.GetState(stateIndex);
        if (targetState == null || !targetState.IsPlayable)
            return;

        if (fadeTime <= 0.0f) {
            Play(layerIndex, stateIndex);
            return;
        }

        if (!targetState.enable)
            targetState.SetStateTime(targetState.GetPlaybackStartTime());

        layer.SetCurrentStateIndex(stateIndex);
        targetState.SetEnable(true);

        for (int i = 0; i < layer.StateCount; i++) {
            AnimationState state = layer.GetState(i);
            if (state == null || !state.IsPlayable || !state.enable)
                continue;

            float targetWeight = i == stateIndex ? 1.0f : 0.0f;
            state.SetFade(targetWeight, fadeTime);
            if (!state.fading && Mathf.Approximately(targetWeight, 0.0f)) {
                state.SetEnable(false);
                state.SetStateTime(0.0f);
            }
        }

        if (!IsPlaying())
            base.Play();
    }

    public void CrossFade(int layerIndex, int stateIndex, float fadeTime, float normalizedTime)
    {
        Init();
        AnimationState state = GetState(layerIndex, stateIndex);
        if (state == null || !state.IsPlayable)
            return;

        CrossFade(layerIndex, stateIndex, fadeTime);
        state.SetStateTime(state.duration * Mathf.Clamp01(normalizedTime));
    }

    public void Blend(int layerIndex, int stateIndex, float targetWeight, float fadeTime)
    {
        Init();
        AnimationState state = GetState(layerIndex, stateIndex);
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

    public void Pause(int layerIndex, int stateIndex)
    {
        AnimationState state = GetState(layerIndex, stateIndex);
        if (state == null || !state.IsPlayable)
            return;

        state.SetEnable(false);
        state.SetWeight(0.0f);
        state.ResetFade();
    }

    public void Stop(int layerIndex, int stateIndex)
    {
        AnimationState state = GetState(layerIndex, stateIndex);
        if (state == null || !state.IsPlayable)
            return;

        state.SetEnable(false);
        state.SetWeight(0.0f);
        state.ResetFade();
        state.SetStateTime(0.0f);
    }

    public void StopLayer(int layerIndex)
    {
        AnimationLayer layer = GetLayer(layerIndex);
        if (layer == null)
            return;

        for (int i = 0; i < layer.StateCount; i++) {
            AnimationState state = layer.GetState(i);
            if (state == null || !state.IsPlayable)
                continue;
            state.SetEnable(false);
            state.SetWeight(0.0f);
            state.ResetFade();
            state.SetStateTime(0.0f);
        }

        layer.SetCurrentStateIndex(-1);
    }

    public void Rewind(int layerIndex, int stateIndex)
    {
        AnimationState state = GetState(layerIndex, stateIndex);
        if (state != null)
            state.SetStateTime(0.0f);
    }
}
