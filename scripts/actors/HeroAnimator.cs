using Godot;

namespace Gravebound.Actors;

public partial class HeroAnimator : Node
{
    [Export] public StringName IdleAnim { get; set; } = "Idle_Shield";
    [Export] public StringName RunAnim { get; set; } = "Walk";
    [Export] public StringName AttackAnim { get; set; } = "Sword_Regular_A";
    [Export] public float SpeedThreshold { get; set; } = 0.1f;
    [Export] public PackedScene? AnimationSource { get; set; }
    [Export] public PackedScene? LocomotionSource { get; set; }

    private CharacterBody3D? _hero;
    private AnimationPlayer? _animPlayer;
    private StringName _currentAnim = "";
    private bool _isPlayingOneShot;

    public override void _Ready()
    {
        _hero = GetParent<CharacterBody3D>();
        _animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                   ?? GetParent()?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        if (_animPlayer != null)
        {
            _animPlayer.AnimationFinished += OnAnimationFinished;

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

    private void OnAnimationFinished(StringName animName)
    {
        if (_isPlayingOneShot)
        {
            _isPlayingOneShot = false;
            _currentAnim = "";
        }
    }

    public void PlayAttack()
    {
        PlayOneShot(AttackAnim);
    }

    public void PlayOneShot(StringName animName)
    {
        if (_animPlayer == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        if (_animPlayer.HasAnimation(animName))
        {
            _isPlayingOneShot = true;
            _currentAnim = animName;
            _animPlayer.Stop();
            _animPlayer.Play(animName);
        }
        else
        {
            GD.PrintErr($"[HeroAnimator] OneShot animation NOT found: {animName}");
        }
    }

    public override void _Process(double delta)
    {
        if (_hero == null || _animPlayer == null || _isPlayingOneShot)
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
        if (_animPlayer == null || string.IsNullOrEmpty(animName) || _currentAnim == animName || _isPlayingOneShot)
        {
            return;
        }

        if (_animPlayer.HasAnimation(animName))
        {
            _currentAnim = animName;
            _animPlayer.Play(animName);
        }
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

                        // Fix FBX 1/100 centimeter scale bug on 3D position tracks
                        if (anim.TrackGetType(t) == Animation.TrackType.Position3D)
                        {
                            for (int k = 0; k < anim.TrackGetKeyCount(t); k++)
                            {
                                Vector3 pos = (Vector3)anim.TrackGetKeyValue(t, k);
                                if (pos != Vector3.Zero && pos.Length() < 0.1f)
                                {
                                    anim.TrackSetKeyValue(t, k, pos * 100.0f);
                                }
                            }
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
}
