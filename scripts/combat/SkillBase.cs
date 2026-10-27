using Godot;

namespace Gravebound.Combat;

[GlobalClass]
public abstract partial class SkillBase : Resource
{
    [Export] public string SkillName { get; set; } = "Skill";
    [Export] public float Cooldown { get; set; } = 3.0f;
    [Export] public float ResourceCost { get; set; } = 20.0f;
    [Export] public float Range { get; set; } = 3.0f;

    /// <summary>
    /// Executes the skill logic. Returns true if the cast was successful.
    /// </summary>
    public abstract bool Cast(Node3D caster, Vector3 targetPosition, Node3D? targetNode = null);
}
