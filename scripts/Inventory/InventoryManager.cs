using Godot;
using System;
using System.Collections.Generic;
using Gravebound.Player;

namespace Gravebound.Inventory;

public partial class InventoryManager : Node
{
    public static InventoryManager? Instance { get; private set; }

    public const int MaxSlots = 20;
    private static readonly ItemData?[] _persistentSlots = new ItemData?[MaxSlots];
    private static readonly Dictionary<ItemSlotType, ItemData> _persistentEquipped = new();
    private static int _persistentGold = 60;
    private static bool _initialized = false;

    public ItemData?[] Slots => _persistentSlots;
    public Dictionary<ItemSlotType, ItemData> Equipped => _persistentEquipped;
    public int Gold
    {
        get => _persistentGold;
        private set => _persistentGold = value;
    }

    [Signal] public delegate void InventoryChangedEventHandler();
    [Signal] public delegate void EquipmentChangedEventHandler();
    [Signal] public delegate void GoldChangedEventHandler(int newGold);
    [Signal] public delegate void ItemPickedUpEventHandler(string itemName, string rarityColor);

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Ready()
    {
        if (!_initialized)
        {
            _initialized = true;
            // Give player a starting sword and potion
            _persistentSlots[0] = new ItemData
            {
                Id = "starting_sword",
                ItemName = "Клинок Пробудженого",
                Description = "Сталевий меч, загартований у склепі.",
                Rarity = ItemRarity.Common,
                SlotType = ItemSlotType.Weapon,
                BonusDamage = 6,
                ValueGold = 20
            };

            _persistentSlots[1] = new ItemData
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
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots[i] == null)
            {
                Slots[i] = item;
                EmitSignal(SignalName.InventoryChanged);
                EmitSignal(SignalName.ItemPickedUp, item.ItemName, item.GetRarityColor().ToHtml());
                return true;
            }
        }
        return false; // Bag full
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < MaxSlots)
        {
            Slots[index] = null;
            EmitSignal(SignalName.InventoryChanged);
        }
    }

    public bool EquipItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
        var item = Slots[slotIndex];
        if (item == null) return false;

        if (item.SlotType == ItemSlotType.Potion)
        {
            return UsePotion(slotIndex);
        }

        // Swap with already equipped item if present
        if (Equipped.TryGetValue(item.SlotType, out var oldItem))
        {
            Slots[slotIndex] = oldItem;
        }
        else
        {
            Slots[slotIndex] = null;
        }

        Equipped[item.SlotType] = item;
        EmitSignal(SignalName.InventoryChanged);
        EmitSignal(SignalName.EquipmentChanged);
        return true;
    }

    public bool UnequipItem(ItemSlotType slotType)
    {
        if (!Equipped.TryGetValue(slotType, out var item)) return false;

        // Find empty slot in bag
        int emptyIdx = -1;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Slots[i] == null)
            {
                emptyIdx = i;
                break;
            }
        }

        if (emptyIdx == -1) return false; // Bag full

        Equipped.Remove(slotType);
        Slots[emptyIdx] = item;
        EmitSignal(SignalName.InventoryChanged);
        EmitSignal(SignalName.EquipmentChanged);
        return true;
    }

    public bool UsePotion(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
        var item = Slots[slotIndex];
        if (item == null || item.SlotType != ItemSlotType.Potion) return false;

        var player = PlayerController.Instance;
        if (player != null && !player.IsDead)
        {
            player.CurrentHealth = Mathf.Min(player.GetTotalMaxHealth(), player.CurrentHealth + item.BonusHealth);
            player.EmitSignal(PlayerController.SignalName.HealthChanged, player.CurrentHealth, player.GetTotalMaxHealth());
            Slots[slotIndex] = null;
            EmitSignal(SignalName.InventoryChanged);
            return true;
        }
        return false;
    }

    public int GetBonusDamage()
    {
        int total = 0;
        foreach (var item in Equipped.Values)
        {
            total += item.BonusDamage;
        }
        return total;
    }

    public int GetBonusArmor()
    {
        int total = 0;
        foreach (var item in Equipped.Values)
        {
            total += item.BonusArmor;
        }
        return total;
    }

    public int GetBonusHealth()
    {
        int total = 0;
        foreach (var item in Equipped.Values)
        {
            total += item.BonusHealth;
        }
        return total;
    }

    public void AddGold(int amount)
    {
        Gold = Mathf.Max(0, Gold + amount);
        EmitSignal(SignalName.GoldChanged, Gold);
    }
}
