using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class LootEntry : Resource
{
    [Export] public ItemData? Item { get; set; }
    [Export] public float Weight { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.0,1.0")] public float DropChance { get; set; } = 1.0f;
}
