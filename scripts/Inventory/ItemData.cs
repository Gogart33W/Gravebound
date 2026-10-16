using Godot;
using System;

namespace Gravebound.Inventory;

public enum ItemRarity
{
    Common,    // White
    Magic,     // Sky Blue
    Rare,      // Golden Yellow
    Unique     // Dark Orange / Amethyst
}

public enum ItemSlotType
{
    Head,
    Chest,
    Hands,
    Legs,
    Weapon,
    Shield,
    Ring,
    Amulet,
    Potion
}

public partial class ItemData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string ItemName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;
    [Export] public ItemSlotType SlotType { get; set; } = ItemSlotType.Weapon;

    [Export] public int BonusHealth { get; set; } = 0;
    [Export] public int BonusDamage { get; set; } = 0;
    [Export] public int BonusArmor { get; set; } = 0;
    [Export] public int ValueGold { get; set; } = 10;

    public Color GetRarityColor()
    {
        return Rarity switch
        {
            ItemRarity.Common => new Color(0.92f, 0.92f, 0.92f),
            ItemRarity.Magic => new Color(0.35f, 0.65f, 1.0f),
            ItemRarity.Rare => new Color(1.0f, 0.82f, 0.2f),
            ItemRarity.Unique => new Color(1.0f, 0.5f, 0.15f),
            _ => Colors.White
        };
    }

    public string GetFormattedStats()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (BonusDamage > 0) parts.Add($"+{BonusDamage} Шкоди");
        if (BonusArmor > 0) parts.Add($"+{BonusArmor} Броні");
        if (BonusHealth > 0) parts.Add($"+{BonusHealth} Здоров'я");
        if (SlotType == ItemSlotType.Potion) parts.Add($"+{BonusHealth} Відновлення HP");
        return string.Join(" | ", parts);
    }

    public ItemData Clone()
    {
        return new ItemData
        {
            Id = Id,
            ItemName = ItemName,
            Description = Description,
            Rarity = Rarity,
            SlotType = SlotType,
            BonusHealth = BonusHealth,
            BonusDamage = BonusDamage,
            BonusArmor = BonusArmor,
            ValueGold = ValueGold
        };
    }

    public static ItemData CreateRandomDrop(bool isBoss)
    {
        int roll = GD.RandRange(0, 100);

        if (isBoss)
        {
            // Boss always drops Rare or Unique
            if (roll > 40)
            {
                return new ItemData
                {
                    Id = "unique_morgul_blade",
                    ItemName = "Клинок Архімага Мор-Гула",
                    Description = "Стародавній меч, що палає фіолетовим попелом душ.",
                    Rarity = ItemRarity.Unique,
                    SlotType = ItemSlotType.Weapon,
                    BonusDamage = 24,
                    BonusHealth = 30,
                    BonusArmor = 4,
                    ValueGold = 250
                };
            }
            return new ItemData
            {
                Id = "rare_crypt_plate",
                ItemName = "Багряні Лати Прадавнього Вартового",
                Description = "Загартований у горнилі катакомб панцир верховного лицаря.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Chest,
                BonusArmor = 15,
                BonusHealth = 40,
                ValueGold = 180
            };
        }

        // Regular enemies drop pool
        if (roll < 35)
        {
            return new ItemData
            {
                Id = "potion_hp",
                ItemName = "Цілюще Зілля Попелу",
                Description = "Миттєво відновлює 35 одиниць здоров'я.",
                Rarity = ItemRarity.Common,
                SlotType = ItemSlotType.Potion,
                BonusHealth = 35,
                ValueGold = 15
            };
        }
        else if (roll < 60)
        {
            return new ItemData
            {
                Id = "magic_iron_sword",
                ItemName = "Зачарований Залізний Меч",
                Description = "Гострий клинок, натертий оливою могильного срібла.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Weapon,
                BonusDamage = 8,
                BonusHealth = 10,
                ValueGold = 45
            };
        }
        else if (roll < 80)
        {
            return new ItemData
            {
                Id = "magic_shield",
                ItemName = "Окований Щит Вартового",
                Description = "Масивний щит із викарбуваним знаком Ордену Попелу.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Shield,
                BonusArmor = 7,
                BonusHealth = 15,
                ValueGold = 50
            };
        }
        else if (roll < 93)
        {
            return new ItemData
            {
                Id = "rare_helm",
                ItemName = "Шолом Могильного Рицаря",
                Description = "Міцний закритий шолом, що зберігає холод катакомб.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Head,
                BonusArmor = 9,
                BonusHealth = 25,
                BonusDamage = 3,
                ValueGold = 95
            };
        }
        else
        {
            return new ItemData
            {
                Id = "rare_ring",
                ItemName = "Перстень Незгасного Полум'я",
                Description = "Древній обруч із темного золота, що пульсує жаром.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Ring,
                BonusDamage = 7,
                BonusHealth = 20,
                ValueGold = 120
            };
        }
    }
}
