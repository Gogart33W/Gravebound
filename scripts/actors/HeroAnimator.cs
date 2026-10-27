using Godot;

namespace Gravebound.Actors;

public partial class HeroAnimator : Node
{
    [Export] public StringName IdleAnim { get; set; } = "Idle_Shield";
    [Export] public StringName RunAnim { get; set; } = "Walk_Carry";
    [Export] public float SpeedThreshold { get; set; } = 0.1f;
    [Export] public PackedScene? AnimationSource { get; set; }

    private CharacterBody3D? _hero;
    private AnimationPlayer? _animPlayer;
    private StringName _currentAnim = "";

    public override void _Ready()
    {
        _hero = GetParent<CharacterBody3D>();
        _animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                   ?? GetParent()?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        GD.Print($"[HeroAnimator] _Ready started. Hero={_hero?.Name}, AnimPlayer={_animPlayer?.Name}, AnimSource={AnimationSource?.ResourcePath}");

        // Copy animations from the source library glb into the player
        if (AnimationSource != null && _animPlayer != null)
        {
            var animInst = AnimationSource.Instantiate();
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

                    int count = 0;
                    foreach (StringName animName in sourceLib.GetAnimationList())
                    {
                        var anim = (Animation)sourceLib.GetAnimation(animName).Duplicate();
                        string nameLower = animName.ToString().ToLowerInvariant();
                        if (nameLower.Contains("idle") || nameLower.Contains("walk") || nameLower.Contains("run"))
                        {
                            anim.LoopMode = Animation.LoopModeEnum.Linear;
                        }

                        if (targetLib.HasAnimation(animName))
                        {
                            targetLib.RemoveAnimation(animName);
                        }
                        targetLib.AddAnimation(animName, anim);
                        count++;
                    }
                    GD.Print($"[HeroAnimator] Copied {count} animations from library '{libName}'. Total in player: {_animPlayer.GetAnimationList().Length}");
                }
            }
            else
            {
                GD.PrintErr("[HeroAnimator] Source AnimationPlayer not found in AnimationSource!");
            }
            animInst.QueueFree();
        }
        else
        {
            GD.PrintErr($"[HeroAnimator] Missing reference: AnimSource={AnimationSource != null}, AnimPlayer={_animPlayer != null}");
        }

        PlayAnimation(IdleAnim);
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
