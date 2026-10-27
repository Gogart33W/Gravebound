using Godot;

namespace Gravebound.Actors;

public partial class HeroAnimator : Node
{
    [Export] public StringName IdleAnim { get; set; } = "Idle_Shield";
    [Export] public StringName RunAnim { get; set; } = "Jog_Fwd";
    [Export] public float SpeedThreshold { get; set; } = 0.1f;
    [Export] public PackedScene? AnimationSource { get; set; }
    [Export] public PackedScene? LocomotionSource { get; set; }

    private CharacterBody3D? _hero;
    private AnimationPlayer? _animPlayer;
    private StringName _currentAnim = "";

    public override void _Ready()
    {
        _hero = GetParent<CharacterBody3D>();
        _animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                   ?? GetParent()?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        if (_animPlayer != null)
        {
            if (AnimationSource != null)
            {
                CopyAnimationsFromSource(AnimationSource);
            }

            if (LocomotionSource != null)
            {
                CopyAnimationsFromSource(LocomotionSource);
            }
        }

        PlayAnimation(IdleAnim);
    }

    private void CopyAnimationsFromSource(PackedScene sourceScene)
    {
        if (_animPlayer == null) return;

        var animInst = sourceScene.Instantiate();
        var sourcePlayer = animInst.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (sourcePlayer != null)
        {
            foreach (StringName libName in sourcePlayer.GetAnimationLibraryList())
            {
                var sourceLib = sourcePlayer.GetAnimationLibrary(libName);
                AnimationLibrary targetLib;
                if (_animPlayer.HasAnimationLibrary(libName))
                {
                    targetLib = _animPlayer.GetAnimationLibrary(libName);
                }
                else
                {
                    targetLib = new AnimationLibrary();
                    _animPlayer.AddAnimationLibrary(libName, targetLib);
                }

                foreach (StringName animName in sourceLib.GetAnimationList())
                {
                    var anim = (Animation)sourceLib.GetAnimation(animName).Duplicate();
                    string rawName = animName.ToString();
                    string cleanName = rawName.StartsWith("Rig|") ? rawName.Substring(4) : rawName;
                    StringName targetAnimName = new StringName(cleanName);

                    // Retarget tracks if source uses Rig/ instead of Armature/
                    for (int t = 0; t < anim.GetTrackCount(); t++)
                    {
                        string trackPath = anim.TrackGetPath(t).ToString();
                        if (trackPath.StartsWith("Rig/"))
                        {
                            anim.TrackSetPath(t, new NodePath("Armature/" + trackPath.Substring(4)));
                        }
                    }

                    string nameLower = cleanName.ToLowerInvariant();
                    if (nameLower.Contains("idle") || nameLower.Contains("walk") || nameLower.Contains("run") || nameLower.Contains("jog"))
                    {
                        anim.LoopMode = Animation.LoopModeEnum.Linear;
                    }

                    if (targetLib.HasAnimation(targetAnimName))
                    {
                        targetLib.RemoveAnimation(targetAnimName);
                    }
                    targetLib.AddAnimation(targetAnimName, anim);
                }
            }
        }
        animInst.QueueFree();
    }

    public override void _Process(double delta)
    {
        if (_hero == null || _animPlayer == null)
        {
            return;
        }

        Vector2 horizontalVelocity = new Vector2(_hero.Velocity.X, _hero.Velocity.Z);
        float speed = horizontalVelocity.Length();

        if (speed >= SpeedThreshold)
        {
            PlayAnimation(RunAnim);
        }
        else
        {
            PlayAnimation(IdleAnim);
        }
    }

    private void PlayAnimation(StringName animName)
    {
        if (_animPlayer == null || string.IsNullOrEmpty(animName) || _currentAnim == animName)
        {
            return;
        }

        if (_animPlayer.HasAnimation(animName))
        {
            _currentAnim = animName;
            _animPlayer.Play(animName);
            GD.Print($"[HeroAnimator] Playing animation: {animName}");
        }
        else
        {
            GD.PrintErr($"[HeroAnimator] Animation NOT found: {animName}. Available: {string.Join(", ", _animPlayer.GetAnimationList())}");
        }
    }
}
