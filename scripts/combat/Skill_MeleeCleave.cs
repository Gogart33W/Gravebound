using Godot;
using Gravebound.Actors;

namespace Gravebound.Combat;

[GlobalClass]
public partial class Skill_MeleeCleave : SkillBase
{
    [Export] public float Damage { get; set; } = 45.0f;
    [Export] public float ArcDegrees { get; set; } = 150.0f;
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

        // Trigger dynamic spinning whirlwind cleave animation at 2.2x speed (~0.7s total)
        var animator = caster.GetNodeOrNull<HeroAnimator>("HeroAnimator");
        if (animator != null)
        {
            animator.PlayOneShot("Sword_Regular_C", 0.05, 2.2f);
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

        SpawnCrescentSlashEffect(caster, casterPos, forward);
        GD.Print($"[Skill_MeleeCleave] Cast Spin Cleave! Enemies hit: {hitCount}");
        return true;
    }

    private void SpawnCrescentSlashEffect(Node3D caster, Vector3 position, Vector3 forward)
    {
        var vfx = new Node3D();
        // Position at chest/waist height where the sword swings
        vfx.Position = position + (Vector3.Up * 0.9f);
        float yaw = Mathf.Atan2(forward.X, forward.Z);
        vfx.Rotation = new Vector3(Mathf.DegToRad(-5.0f), yaw, 0.0f);

        var slashMesh = new MeshInstance3D();
        slashMesh.Mesh = CreateSlashArcMesh(radius: Range * 0.75f, width: 0.75f, arcDeg: ArcDegrees, segments: 24);

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoColor = new Color(1.0f, 0.6f, 0.15f, 0.95f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        slashMesh.MaterialOverride = mat;
        vfx.AddChild(slashMesh);

        caster.GetTree().Root.AddChild(vfx);

        var tween = vfx.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(slashMesh, "scale", new Vector3(1.3f, 1.0f, 1.3f), 0.3f);
        tween.TweenProperty(mat, "albedo_color:a", 0.0f, 0.3f);
        tween.Chain().TweenCallback(Callable.From(() => vfx.QueueFree()));
    }

    private static ArrayMesh CreateSlashArcMesh(float radius, float width, float arcDeg, int segments)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        float halfRad = Mathf.DegToRad(arcDeg * 0.5f);
        float step = (halfRad * 2.0f) / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = -halfRad + (i * step);
            float a1 = -halfRad + ((i + 1) * step);

            // Forward in local space is (sin(a), 0, cos(a))
            var dir0 = new Vector3(Mathf.Sin(a0), 0.0f, Mathf.Cos(a0));
            var dir1 = new Vector3(Mathf.Sin(a1), 0.0f, Mathf.Cos(a1));

            var out0 = dir0 * (radius + width * 0.5f);
            var in0 = dir0 * (radius - width * 0.5f);
            var out1 = dir1 * (radius + width * 0.5f);
            var in1 = dir1 * (radius - width * 0.5f);

            // Triangle 1
            st.SetNormal(Vector3.Up);
            st.AddVertex(out0);
            st.AddVertex(in0);
            st.AddVertex(out1);

            // Triangle 2
            st.SetNormal(Vector3.Up);
            st.AddVertex(out1);
            st.AddVertex(in0);
            st.AddVertex(in1);
        }

        return st.Commit();
    }
}
