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
            ItemRarity.Common => new Color(0.9f, 0.9f, 0.9f),
            ItemRarity.Magic => new Color(0.3f, 0.6f, 1.0f),
            ItemRarity.Rare => new Color(1.0f, 0.85f, 0.2f),
            ItemRarity.Unique => new Color(1.0f, 0.5f, 0.1f),
            _ => Colors.White
        };
    }
}
