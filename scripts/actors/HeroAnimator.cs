using Godot;

namespace Gravebound.Actors;

public partial class HeroAnimator : Node
{
    [Export] public StringName IdleAnim { get; set; } = "Idle_Shield_Loop";
    [Export] public StringName RunAnim { get; set; } = "Walk_Carry_Loop";
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

        // Copy animations from the source library glb into the player
        if (AnimationSource != null && _animPlayer != null)
        {
            var animInst = AnimationSource.Instantiate();
            var sourcePlayer = animInst.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if (sourcePlayer != null)
            {
                foreach (StringName libName in sourcePlayer.GetAnimationLibraryList())
                {
                    var lib = sourcePlayer.GetAnimationLibrary(libName);
                    if (!_animPlayer.HasAnimationLibrary(libName))
                    {
                        _animPlayer.AddAnimationLibrary(libName, (AnimationLibrary)lib.Duplicate());
                    }
                }
            }
            animInst.QueueFree();
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
        }
    }
}
