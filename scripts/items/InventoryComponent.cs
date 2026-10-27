using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class InventoryComponent : Node
{
    [Signal]
    public delegate void ItemAddedEventHandler(ItemData item, int slot);

    [Signal]
    public delegate void ItemRemovedEventHandler(int slot);

    [Signal]
    public delegate void InventoryUpdatedEventHandler();

    [Export] public int Capacity { get; set; } = 40;

    private readonly Godot.Collections.Array<ItemData?> _slots = new();

    public override void _Ready()
    {
        EnsureSlots();
    }

    public void EnsureSlots()
    {
        while (_slots.Count < Capacity)
        {
            _slots.Add(null);
        }
    }

    public ItemData? GetItem(int slot)
    {
        EnsureSlots();
        if (slot >= 0 && slot < _slots.Count)
        {
            return _slots[slot];
        }
        return null;
    }

    public int FindFirstEmptySlot()
    {
        EnsureSlots();
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    public bool TryAddItem(ItemData? item)
    {
        if (item == null)
        {
            return false;
        }

        int slot = FindFirstEmptySlot();
        if (slot == -1)
        {
            GD.Print($"[InventoryComponent] Inventory is full! Could not add {item.ItemName}");
            return false;
        }

        _slots[slot] = item;
        EmitSignal(SignalName.ItemAdded, item, slot);
        EmitSignal(SignalName.InventoryUpdated);
        GD.Print($"[InventoryComponent] Added {item.ItemName} to slot {slot}");
        return true;
    }

    public bool SetItem(int slot, ItemData? item)
    {
        EnsureSlots();
        if (slot < 0 || slot >= _slots.Count)
        {
            return false;
        }

        _slots[slot] = item;
        if (item != null)
        {
            EmitSignal(SignalName.ItemAdded, item, slot);
        }
        else
        {
            EmitSignal(SignalName.ItemRemoved, slot);
        }
        EmitSignal(SignalName.InventoryUpdated);
        return true;
    }

    public void RemoveItem(int slot)
    {
        EnsureSlots();
        if (slot >= 0 && slot < _slots.Count && _slots[slot] != null)
        {
            var item = _slots[slot];
            _slots[slot] = null;
            EmitSignal(SignalName.ItemRemoved, slot);
            EmitSignal(SignalName.InventoryUpdated);
            GD.Print($"[InventoryComponent] Removed {item?.ItemName} from slot {slot}");
        }
    }

    public Godot.Collections.Array<ItemData?> GetSlots()
    {
        EnsureSlots();
        return _slots;
    }
}
