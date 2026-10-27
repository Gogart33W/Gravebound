using Godot;
using Gravebound.Actors;

namespace Gravebound.Combat;

[GlobalClass]
public partial class Skill_Dash : SkillBase
{
    [Export] public float DashSpeed { get; set; } = 15.0f;
    [Export] public float DashDuration { get; set; } = 0.38f;
    [Export] public float InvulnerableDuration { get; set; } = 0.40f;

    public Skill_Dash()
    {
        SkillName = "Dash";
        Cooldown = 2.0f;
        ResourceCost = 15.0f;
        Range = 5.7f;
    }

    public override bool Cast(Node3D caster, Vector3 targetPosition, Node3D? targetNode = null)
    {
        if (caster == null || !caster.IsInsideTree())
        {
            return false;
        }

        Vector3 casterPos = caster.GlobalPosition;
        Vector3 dashDir = targetPosition - casterPos;
        dashDir.Y = 0.0f;

        if (dashDir.LengthSquared() < 0.01f)
        {
            dashDir = caster.GlobalTransform.Basis.Z;
            dashDir.Y = 0.0f;
        }

        if (dashDir.LengthSquared() < 0.001f)
        {
            dashDir = Vector3.Forward;
        }
        else
        {
            dashDir = dashDir.Normalized();
        }

        // Rotate caster to face dash direction
        float targetAngle = Mathf.Atan2(dashDir.X, dashDir.Z);
        caster.Rotation = new Vector3(caster.Rotation.X, targetAngle, caster.Rotation.Z);

        // Trigger combat dive roll animation at 2.8x speed (~0.5s roll)
        var animator = caster.GetNodeOrNull<HeroAnimator>("HeroAnimator");
        if (animator != null)
        {
            animator.PlayOneShot("Roll", 0.05, 2.8f);
        }

        // Delegate physical dash and invulnerability to HeroController
        if (caster is HeroController hero)
        {
            hero.StartDash(dashDir, DashSpeed, DashDuration, InvulnerableDuration);
        }

        SpawnDashDustPuff(caster, casterPos, dashDir);
        GD.Print($"[Skill_Dash] Dashed towards {dashDir}");
        return true;
    }

    private void SpawnDashDustPuff(Node3D caster, Vector3 startPos, Vector3 dashDir)
    {
        var vfx = new Node3D();
        vfx.Position = startPos + (Vector3.Up * 0.05f);

        // Ground dust ring expanding at launch
        var dustMesh = new MeshInstance3D();
        var torus = new TorusMesh
        {
            InnerRadius = 0.3f,
            OuterRadius = 0.6f,
            Rings = 16,
            RingSegments = 8
        };
        dustMesh.Mesh = torus;

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.9f, 0.88f, 0.82f, 0.5f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        dustMesh.MaterialOverride = mat;
        vfx.AddChild(dustMesh);

        caster.GetTree().Root.AddChild(vfx);

        var tween = vfx.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(dustMesh, "scale", new Vector3(2.2f, 0.5f, 2.2f), 0.3f);
        tween.TweenProperty(mat, "albedo_color:a", 0.0f, 0.3f);
        tween.Chain().TweenCallback(Callable.From(() => vfx.QueueFree()));
    }
}
