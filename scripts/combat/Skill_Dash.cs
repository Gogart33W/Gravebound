using Godot;
using Gravebound.Actors;

namespace Gravebound.Combat;

[GlobalClass]
public partial class Skill_Dash : SkillBase
{
    [Export] public float DashSpeed { get; set; } = 22.0f;
    [Export] public float DashDuration { get; set; } = 0.22f;
    [Export] public float InvulnerableDuration { get; set; } = 0.28f;

    public Skill_Dash()
    {
        SkillName = "Dash";
        Cooldown = 2.0f;
        ResourceCost = 15.0f;
        Range = 6.0f;
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
            dashDir = -caster.GlobalTransform.Basis.Z;
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

        // Trigger roll / dash animation
        var animator = caster.GetNodeOrNull<HeroAnimator>("HeroAnimator");
        if (animator != null)
        {
            animator.PlayOneShot("Roll", 0.05);
        }

        // Delegate physical dash and invulnerability to HeroController
        if (caster is HeroController hero)
        {
            hero.StartDash(dashDir, DashSpeed, DashDuration, InvulnerableDuration);
        }

        SpawnDashGhostTrail(caster, casterPos, dashDir);
        GD.Print($"[Skill_Dash] Dashed towards {dashDir}");
        return true;
    }

    private void SpawnDashGhostTrail(Node3D caster, Vector3 startPos, Vector3 dashDir)
    {
        var vfx = new Node3D();
        vfx.Position = startPos + (Vector3.Up * 0.1f);

        var trail = new MeshInstance3D();
        var box = new BoxMesh { Size = new Vector3(0.6f, 0.1f, 2.5f) };
        trail.Mesh = box;

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.2f, 0.8f, 1.0f, 0.7f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        trail.MaterialOverride = mat;
        trail.Rotation = new Vector3(0, Mathf.Atan2(dashDir.X, dashDir.Z), 0);
        vfx.AddChild(trail);

        caster.GetTree().Root.AddChild(vfx);

        var tween = vfx.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(mat, "albedo_color:a", 0.0f, 0.25f);
        tween.TweenProperty(trail, "scale", new Vector3(0.5f, 1.0f, 1.5f), 0.25f);
        tween.Chain().TweenCallback(Callable.From(() => vfx.QueueFree()));
    }
}
