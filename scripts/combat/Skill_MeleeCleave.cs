using Godot;
using Gravebound.Actors;

namespace Gravebound.Combat;

[GlobalClass]
public partial class Skill_MeleeCleave : SkillBase
{
    [Export] public float Damage { get; set; } = 45.0f;
    [Export] public float ArcDegrees { get; set; } = 140.0f;
    [Export] public uint TargetLayerMask { get; set; } = 4u; // Layer 3 (Enemies = 4)

    public Skill_MeleeCleave()
    {
        SkillName = "Cleave";
        Cooldown = 2.5f;
        ResourceCost = 25.0f;
        Range = 3.5f;
    }

    public override bool Cast(Node3D caster, Vector3 targetPosition, Node3D? targetNode = null)
    {
        if (caster == null || !caster.IsInsideTree())
        {
            return false;
        }

        // Aim towards targetNode if present, otherwise targetPosition
        Vector3 casterPos = caster.GlobalPosition;
        Vector3 aimTarget = targetNode != null ? targetNode.GlobalPosition : targetPosition;
        Vector3 aimDir = aimTarget - casterPos;
        aimDir.Y = 0.0f;

        Vector3 forward;
        if (aimDir.LengthSquared() > 0.001f)
        {
            forward = aimDir.Normalized();
            float targetAngle = Mathf.Atan2(forward.X, forward.Z);
            caster.Rotation = new Vector3(caster.Rotation.X, targetAngle, caster.Rotation.Z);
        }
        else
        {
            forward = caster.GlobalTransform.Basis.Z;
            forward.Y = 0.0f;
            forward = forward.LengthSquared() > 0.001f ? forward.Normalized() : Vector3.Forward;
        }

        // Trigger cleave attack animation
        var animator = caster.GetNodeOrNull<HeroAnimator>("HeroAnimator");
        if (animator != null)
        {
            animator.PlayOneShot("Sword_Regular_B", 0.08);
        }

        int hitCount = 0;
        var hitTargets = new System.Collections.Generic.HashSet<IDamageable>();

        // 1. Check all enemies in "Enemies" group within arc
        var enemyNodes = caster.GetTree().GetNodesInGroup("Enemies");
        foreach (var node in enemyNodes)
        {
            if (node is Node3D enemyNode && node != caster)
            {
                Vector3 toEnemy = enemyNode.GlobalPosition - casterPos;
                toEnemy.Y = 0.0f;
                float dist = toEnemy.Length();

                if (dist <= Range && dist > 0.001f)
                {
                    float angleDeg = Mathf.RadToDeg(forward.AngleTo(toEnemy.Normalized()));
                    if (angleDeg <= ArcDegrees * 0.5f)
                    {
                        IDamageable? damageable = enemyNode as IDamageable
                                               ?? enemyNode.GetNodeOrNull<HealthComponent>("HealthComponent") as IDamageable;

                        if (damageable != null && hitTargets.Add(damageable))
                        {
                            damageable.TakeDamage(Damage);
                            hitCount++;
                        }
                    }
                }
            }
        }

        // 2. Also query physics shape for any other IDamageables (e.g. breakable props)
        var spaceState = caster.GetWorld3D().DirectSpaceState;
        var shapeQuery = new PhysicsShapeQueryParameters3D();
        var sphere = new SphereShape3D { Radius = Range };
        shapeQuery.Shape = sphere;
        shapeQuery.Transform = new Transform3D(Basis.Identity, casterPos);
        shapeQuery.CollisionMask = TargetLayerMask;

        var hits = spaceState.IntersectShape(shapeQuery, 16);
        foreach (var hit in hits)
        {
            if (hit.TryGetValue("collider", out Variant colliderVar))
            {
                var colliderObj = colliderVar.AsGodotObject();
                if (colliderObj != caster && colliderObj is Node3D objNode)
                {
                    IDamageable? damageable = colliderObj as IDamageable
                                           ?? (colliderObj is Node n ? n.GetParent() as IDamageable : null);

                    if (damageable != null && hitTargets.Add(damageable))
                    {
                        Vector3 toObj = objNode.GlobalPosition - casterPos;
                        toObj.Y = 0.0f;
                        if (toObj.LengthSquared() > 0.001f)
                        {
                            float angleDeg = Mathf.RadToDeg(forward.AngleTo(toObj.Normalized()));
                            if (angleDeg <= ArcDegrees * 0.5f)
                            {
                                damageable.TakeDamage(Damage);
                                hitCount++;
                            }
                        }
                    }
                }
            }
        }

        SpawnCleaveEffect(caster, casterPos, forward);
        GD.Print($"[Skill_MeleeCleave] Cast Cleave! Enemies hit: {hitCount}");
        return true;
    }

    private void SpawnCleaveEffect(Node3D caster, Vector3 position, Vector3 forward)
    {
        var vfx = new Node3D();
        vfx.Position = position + (Vector3.Up * 0.8f) + (forward * (Range * 0.4f));

        var slashMesh = new MeshInstance3D();
        var cylinder = new CylinderMesh
        {
            TopRadius = Range * 0.8f,
            BottomRadius = Range * 0.8f,
            Height = 0.05f
        };
        slashMesh.Mesh = cylinder;

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1.0f, 0.4f, 0.1f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        slashMesh.MaterialOverride = mat;
        vfx.AddChild(slashMesh);

        caster.GetTree().Root.AddChild(vfx);

        var tween = vfx.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(slashMesh, "scale", new Vector3(1.3f, 1.0f, 1.3f), 0.2f);
        tween.TweenProperty(mat, "albedo_color:a", 0.0f, 0.2f);
        tween.Chain().TweenCallback(Callable.From(() => vfx.QueueFree()));
    }
}
