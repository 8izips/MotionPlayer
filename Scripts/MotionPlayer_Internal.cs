using UnityEngine;
using UnityEngine.Playables;

public partial class MotionPlayer : MotionPlayerBase
{
    bool _statesInitialized = false;

    public override void Init()
    {
        if (_statesInitialized && _initialized && _graph.IsValid() && _playable != null)
            return;

        base.Init();
        CreateAnimationPlayableOutput();
        _InitStates();
        _statesInitialized = true;
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (_states == null || _states.Length == 0)
            _states = new AnimationState[1];

        for (int i = 0; i < _states.Length; i++) {
            if (_states[i] == null)
                _states[i] = new AnimationState();
        }

        if (string.IsNullOrEmpty(_states[0].name))
            _states[0].name = "Default";
    }

    protected override void Tick(float elapsedTime)
    {
        if (!_statesInitialized || !_graph.IsValid() || _states == null)
            return;

        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
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
            if (state.TryInvokeEndCallback() && deactivateOnEnd && i == CurStateIndex) {
                gameObject.SetActive(false);
                return;
            }
        }
    }

    protected override void OnStopped()
    {
        if (_states == null)
            return;

        for (int i = 0; i < _states.Length; i++) {
            AnimationState state = _states[i];
            if (state == null || !state.IsPlayable)
                continue;
            state.ResetFade();
            state.SetStateTime(state.GetPlaybackStartTime());
        }
    }
}
