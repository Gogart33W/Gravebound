using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class EquipmentComponent : Node
{
    [Signal]
    public delegate void ItemEquippedEventHandler(int slotType, ItemData item);

    [Signal]
    public delegate void ItemUnequippedEventHandler(int slotType);

    [Signal]
    public delegate void EquipmentUpdatedEventHandler();

    private readonly Godot.Collections.Dictionary<int, ItemData> _equipped = new();

    public ItemData? GetEquipped(ItemType slot)
    {
        if (_equipped.TryGetValue((int)slot, out var item))
        {
            return item;
        }
        return null;
    }

    public bool Equip(ItemData item, out ItemData? previousItem)
    {
        previousItem = null;
        if (item.Type == ItemType.Consumable)
        {
            GD.Print($"[EquipmentComponent] Cannot equip consumable: {item.ItemName}");
            return false;
        }

        int slotKey = (int)item.Type;
        if (_equipped.TryGetValue(slotKey, out var oldItem))
        {
            previousItem = oldItem;
            EmitSignal(SignalName.ItemUnequipped, slotKey);
        }

        _equipped[slotKey] = item;
        EmitSignal(SignalName.ItemEquipped, slotKey, item);
        EmitSignal(SignalName.EquipmentUpdated);
        GD.Print($"[EquipmentComponent] Equipped {item.ItemName} in slot {item.Type}");
        return true;
    }

    public bool Equip(ItemData item)
    {
        return Equip(item, out _);
    }

    public ItemData? Unequip(ItemType slot)
    {
        int slotKey = (int)slot;
        if (_equipped.TryGetValue(slotKey, out var item))
        {
            _equipped.Remove(slotKey);
            EmitSignal(SignalName.ItemUnequipped, slotKey);
            EmitSignal(SignalName.EquipmentUpdated);
            GD.Print($"[EquipmentComponent] Unequipped {item.ItemName} from slot {slot}");
            return item;
        }
        return null;
    }

    public float GetTotalStatModifier(string statKey)
    {
        float total = 0.0f;
        foreach (var item in _equipped.Values)
        {
            if (item?.StatModifiers != null && item.StatModifiers.TryGetValue(statKey, out float value))
            {
                total += value;
            }
        }
        return total;
    }

    public Godot.Collections.Dictionary<int, ItemData> GetAllEquipped() => _equipped;
}
