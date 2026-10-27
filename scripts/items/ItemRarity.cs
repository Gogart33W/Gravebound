using Godot;

namespace Gravebound.Items;

public enum ItemRarity
{
    Common,
    Magic,
    Rare,
    Unique
}

public static class ItemRarityExtensions
{
    public static Color GetColor(this ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.85f, 0.85f, 0.85f),
            ItemRarity.Magic => new Color(0.35f, 0.65f, 1.0f),
            ItemRarity.Rare => new Color(0.98f, 0.78f, 0.18f),
            ItemRarity.Unique => new Color(0.98f, 0.45f, 0.12f),
            _ => Colors.White
        };
    }
}
