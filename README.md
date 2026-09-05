# MotionPlayer

A lightweight `AnimationClip` playback system for Unity 6000.3 and later, built on the Playables API.

MotionPlayer is intended for cases where an `AnimatorController` and state machine are more than you need. It plays clips directly, supports cross-fades and optional layers, and keeps the non-layered player as small as possible.

## Components

### MotionPlayerSingle

The smallest player for a single `AnimationClip`.

- One clip only
- Direct `AnimationClipPlayable -> AnimationPlayableOutput` connection
- No mixer or state collection
- Best suited to simple props, effects, and characters that only need one clip

### MotionPlayer

The standard lightweight player for multiple clips.

- Play / Pause / Resume / Stop / Rewind
- CrossFade and Blend
- Normalized-time playback
- Reverse playback with negative speed
- End callbacks
- Foot IK / Playable IK options
- `Normal`, `UnscaledTime`, and `Fixed` update modes
- Inactive states are paused instead of continuing to evaluate in the background

### MotionPlayerLayered

Use this only when animation layers are required. The normal `MotionPlayer` does not include layer-mixer overhead.

- Base layer plus optional additional layers
- Override and Additive layers
- `AvatarMask` support
- Independent layer weights
- Layer-local Play / CrossFade / Blend controls
- Layers at weight `0` pause their active states

Layer structure is intended to be configured in the Editor rather than added and removed repeatedly at runtime.

## Installation

Copy this repository into your Unity project. A typical location is:

```text
Assets/Plugins/MotionPlayer
```

MotionPlayer does not depend on that exact path.

If the target `GameObject` already has an `Animator`, leave its Controller set to `None` when MotionPlayer is responsible for animation playback.

## Basic usage

```csharp
using UnityEngine;

public class CharacterMotion : MonoBehaviour
{
    [SerializeField] MotionPlayer motionPlayer;

    void Start()
    {
        motionPlayer.Play("Idle");
    }

    public void Run()
    {
        motionPlayer.CrossFade("Run", 0.2f);
    }

    public void StopRunning()
    {
        motionPlayer.CrossFade("Idle", 0.2f);
    }
}
```

### Pause, resume, stop, and rewind

```csharp
motionPlayer.Pause("Run");       // Keep the current time.
motionPlayer.Resume("Run");      // Continue from the current time.
motionPlayer.Stop("Run");        // Stop and return to the start.
motionPlayer.Rewind("Run");      // Return to the playback start position.
```

`Play()` restarts the state by default. Use `Resume()` when you want to continue from its current position.

### Start from normalized time

```csharp
motionPlayer.Play("Run", 0.5f);
motionPlayer.CrossFade("Attack", 0.15f, 0.25f);
```

### Reverse playback

```csharp
MotionPlayer.AnimationState state = motionPlayer.GetState("Door");
state.speed = -1.0f;
motionPlayer.Play("Door");
```

A negative speed starts from the end of the clip and uses time `0` as the playback end.

## Layered usage

```csharp
using UnityEngine;

public class LayeredCharacterMotion : MonoBehaviour
{
    [SerializeField] MotionPlayerLayered motionPlayer;

    int upperBodyLayer;

    void Awake()
    {
        upperBodyLayer = motionPlayer.GetLayerIndex("UpperBody");
    }

    public void Run()
    {
        motionPlayer.CrossFade("Run", 0.2f); // Base layer
    }

    public void Reload()
    {
        motionPlayer.SetLayerWeight(upperBodyLayer, 1.0f);
        motionPlayer.CrossFade(upperBodyLayer, "Reload", 0.15f);
    }

    public void DisableUpperBodyLayer()
    {
        motionPlayer.SetLayerWeight(upperBodyLayer, 0.0f);
    }
}
```

Layer `0` is always the Base layer and has weight `1`. Additional layers can use an `AvatarMask` and can be configured as Override or Additive.

## Runtime state lookup

String lookup is convenient for occasional calls:

```csharp
motionPlayer.CrossFade("Run", 0.2f);
```

For hot paths, cache the state index once and use the integer overloads:

```csharp
int runState;

void Awake()
{
    runState = motionPlayer.GetStateIndex("Run");
}

void Run()
{
    motionPlayer.CrossFade(runState, 0.2f);
}
```

## Requirements

- Unity 6000.3 or later
- Unity Playables / Animation Playables APIs

## Design goals

- Keep direct clip playback lightweight
- Avoid Animator Controller state-machine overhead when it is not needed
- Avoid per-frame managed allocations in normal playback paths
- Keep layers optional rather than imposing their cost on every player
- Keep the public API small and explicit

MotionPlayer is not intended to replace every Animator feature. If you need complex Animator Controller state machines, Blend Trees, parameter-driven transitions, or other Animator-specific workflows, Unity's Animator may still be the better fit.

## License

MIT License. See [LICENSE](LICENSE).
