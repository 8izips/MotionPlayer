using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public abstract partial class MotionPlayerBase : MonoBehaviour
{
    #region Animator Property
    public Animator Animator {
        get {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            return _animator;
        }
    }

    public Avatar Avatar {
        get { return Animator.avatar; }
        set { Animator.avatar = value; }
    }

    public bool ApplyRootMotion {
        get { return Animator.applyRootMotion; }
        set { Animator.applyRootMotion = value; }
    }

    public AnimatorUpdateMode UpdateMode {
        get { return Animator.updateMode; }
        set {
            Animator.updateMode = value;
            UpdateGraphTimeMode();
        }
    }

    public AnimatorCullingMode CullingMode {
        get { return Animator.cullingMode; }
        set { Animator.cullingMode = value; }
    }

    Animator _animator;
    #endregion

    protected PlayableGraph _graph;
    protected AnimationPlayable _playable;
    protected bool _initialized = false;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
        if (playOnAwake)
            Play();
    }

    void OnDisable()
    {
        Stop();
    }

    void Update()
    {
        if (!_graph.IsValid() || !_graph.IsPlaying() || Animator == null || Animator.updateMode == AnimatorUpdateMode.Fixed)
            return;

        Tick(GetDeltaTime());
    }

    void FixedUpdate()
    {
        if (!_graph.IsValid() || !_graph.IsPlaying() || Animator == null || Animator.updateMode != AnimatorUpdateMode.Fixed)
            return;

        _graph.Evaluate(Time.fixedDeltaTime);
        Tick(Time.fixedDeltaTime);
    }

    void OnDestroy()
    {
        if (_graph.IsValid())
            _graph.Destroy();

        _playable = null;
        _initialized = false;
    }

    public virtual void Init()
    {
        if (_initialized && _graph.IsValid())
            return;

        if (_graph.IsValid())
            _graph.Destroy();

        _animator = GetComponent<Animator>();
        _playable = null;
        _graph = PlayableGraph.Create();
        UpdateGraphTimeMode();
        _initialized = true;
    }

    protected void CreateAnimationPlayableOutput()
    {
        if (!_graph.IsValid() || _playable != null)
            return;

        AnimationPlayable template = new AnimationPlayable();
        ScriptPlayable<AnimationPlayable> playable = ScriptPlayable<AnimationPlayable>.Create(_graph, template, 1);
        _playable = playable.GetBehaviour();

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "MotionPlayer", _animator);
        output.SetSourcePlayable(_playable.Playable);
    }

    protected void CreateAnimationOutput(Playable sourcePlayable, string outputName)
    {
        if (!_graph.IsValid() || !sourcePlayable.IsValid())
            return;

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, outputName, _animator);
        output.SetSourcePlayable(sourcePlayable);
    }

    void UpdateGraphTimeMode()
    {
        if (!_graph.IsValid() || Animator == null)
            return;

        DirectorUpdateMode mode = DirectorUpdateMode.GameTime;
        if (Animator.updateMode == AnimatorUpdateMode.UnscaledTime)
            mode = DirectorUpdateMode.UnscaledGameTime;
        else if (Animator.updateMode == AnimatorUpdateMode.Fixed)
            mode = DirectorUpdateMode.Manual;

        _graph.SetTimeUpdateMode(mode);
    }

    protected float GetDeltaTime()
    {
        return Animator != null && Animator.updateMode == AnimatorUpdateMode.UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    protected virtual void Tick(float deltaTime) { }
}
