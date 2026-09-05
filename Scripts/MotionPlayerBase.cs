using UnityEngine;

[RequireComponent(typeof(Animator))]
public abstract partial class MotionPlayerBase : MonoBehaviour
{
    public bool playOnAwake = true;
    public bool deactivateOnEnd = false;

    public bool IsPlaying()
    {
        return _graph.IsValid() && _graph.IsPlaying();
    }

    public void Play()
    {
        Init();
        if (_graph.IsValid())
            _graph.Play();
    }

    public void Stop()
    {
        if (!_graph.IsValid())
            return;

        _graph.Stop();
        OnStopped();
    }

    public void Pause()
    {
        if (_graph.IsValid())
            _graph.Stop();
    }

    public void Resume()
    {
        Play();
    }

    public void Evaluate()
    {
        if (_graph.IsValid())
            _graph.Evaluate();
    }

    protected virtual void OnStopped() { }
}
