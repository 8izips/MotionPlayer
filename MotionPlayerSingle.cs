using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class MotionPlayerSingle : MotionPlayerBase
{
    [SerializeField] AnimationClip animClip;

    AnimationClipPlayable clipPlayable;
    bool clipInitialized = false;

    public override void Init()
    {
        if (clipInitialized && _initialized && _graph.IsValid() && clipPlayable.IsValid())
            return;

        base.Init();
        clipInitialized = false;
        if (animClip == null || !_graph.IsValid())
            return;

        clipPlayable = AnimationClipPlayable.Create(_graph, animClip);
        if (!animClip.isLooping)
            clipPlayable.SetDuration(animClip.length);

        CreateAnimationOutput(clipPlayable, "MotionPlayerSingle");
        clipInitialized = true;
    }

    protected override void Tick(float deltaTime)
    {
        if (!clipInitialized || !_graph.IsValid() || !clipPlayable.IsValid() || animClip == null)
            return;

        if (deactivateOnEnd && !animClip.isLooping && clipPlayable.GetTime() >= animClip.length)
            gameObject.SetActive(false);
    }

    protected override void OnStopped()
    {
        if (clipPlayable.IsValid())
            clipPlayable.SetTime(0.0);
    }
}
