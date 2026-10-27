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
        PlayOneShot(AttackAnim, 0.08);
    }

    public void PlayOneShot(StringName animName, double blendTime = 0.08)
    {
        if (_animPlayer == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        if (_animPlayer.HasAnimation(animName))
        {
            _isPlayingOneShot = true;
            _currentAnim = animName;
            if (_animPlayer.IsPlaying() && _animPlayer.CurrentAnimation == animName)
            {
                _animPlayer.Seek(0, true);
            }
            _animPlayer.Play(animName, customBlend: blendTime);
        }
        else
        {
            GD.PrintErr($"[HeroAnimator] OneShot animation NOT found: {animName}");
        }
    }

    public void StopOneShot()
    {
        if (_isPlayingOneShot)
        {
            _isPlayingOneShot = false;
            _currentAnim = "";
        }
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
            // Moving cancels one-shot animation smoothly
            _isPlayingOneShot = false;
            PlayAnimation(RunAnim, 0.12);
        }
        else if (!_isPlayingOneShot)
        {
            PlayAnimation(IdleAnim, 0.15);
        }
    }

    private void PlayAnimation(StringName animName, double blendTime = 0.15)
    {
        if (_animPlayer == null || string.IsNullOrEmpty(animName) || _currentAnim == animName || _isPlayingOneShot)
        {
            return;
        }

        if (_animPlayer.HasAnimation(animName))
        {
            _currentAnim = animName;
            _animPlayer.Play(animName, customBlend: blendTime);
        }
    }

    private void CopyAnimationsFromSource(PackedScene sourceScene)
    {
        if (_animPlayer == null) return;

        bool isFbx = sourceScene.ResourcePath != null &&
                     sourceScene.ResourcePath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

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
                        bool isRigTrack = trackPath.StartsWith("Rig/");
                        if (isRigTrack)
                        {
                            trackPath = "Armature/" + trackPath.Substring(4);
                            anim.TrackSetPath(t, new NodePath(trackPath));
                        }

                        // Fix FBX 1/100 centimeter scale bug ONLY for FBX sources!
                        // (GLTF/GLB files are already in meters; scaling them causes 100x teleportation glitches)
                        if ((isFbx || isRigTrack) && anim.TrackGetType(t) == Animation.TrackType.Position3D)
                        {
                            for (int k = 0; k < anim.TrackGetKeyCount(t); k++)
                            {
                                Vector3 pos = (Vector3)anim.TrackGetKeyValue(t, k);
                                anim.TrackSetKeyValue(t, k, pos * 100.0f);
                            }
                        }

                        // Ground root bone: prevent root motion dislocation during attacks/locomotion.
                        // In an ARPG, character displacement is driven by CharacterBody3D navigation/velocity.
                        // Root position displacement causes the character mesh to violently slide/teleport
                        // away from collision capsule and camera, creating duplicate/flying visual glitches.
                        if (trackPath.EndsWith(":root") && anim.TrackGetType(t) == Animation.TrackType.Position3D)
                        {
                            for (int k = 0; k < anim.TrackGetKeyCount(t); k++)
                            {
                                anim.TrackSetKeyValue(t, k, Vector3.Zero);
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
