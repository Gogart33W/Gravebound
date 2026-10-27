using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string ItemName { get; set; } = "Item";
    [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;
    [Export] public ItemType Type { get; set; } = ItemType.MainHand;
    [Export] public Texture2D? Icon { get; set; }
    [Export] public PackedScene? WorldVisual { get; set; }
    [Export] public Godot.Collections.Dictionary<string, float> StatModifiers { get; set; } = new();
    [Export] public string Description { get; set; } = "";
}
