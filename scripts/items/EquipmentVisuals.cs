using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class EquipmentVisuals : Node
{
    [Export] public NodePath MainHandSlotPath { get; set; } = new NodePath("../Slot_MainHand");
    [Export] public NodePath OffHandSlotPath { get; set; } = new NodePath("../Slot_OffHand");
    [Export] public NodePath HeadSlotPath { get; set; } = new NodePath("../Slot_Head");

    [Export] public Vector3 MainHandPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 MainHandRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public Vector3 OffHandPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 OffHandRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public Vector3 HeadPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 HeadRotationDegrees { get; set; } = Vector3.Zero;

    private BoneAttachment3D? _slotMainHand;
    private BoneAttachment3D? _slotOffHand;
    private BoneAttachment3D? _slotHead;

    private EquipmentComponent? _equipment;
    private readonly System.Collections.Generic.Dictionary<ItemType, Node3D> _spawnedVisuals = new();

    public override void _Ready()
    {
        _slotMainHand = GetNodeOrNull<BoneAttachment3D>(MainHandSlotPath);
        _slotOffHand = GetNodeOrNull<BoneAttachment3D>(OffHandSlotPath);
        _slotHead = GetNodeOrNull<BoneAttachment3D>(HeadSlotPath);

        _equipment = GetParent()?.GetNodeOrNull<EquipmentComponent>("EquipmentComponent");
        if (_equipment != null)
        {
            _equipment.ItemEquipped += OnItemEquipped;
            _equipment.ItemUnequipped += OnItemUnequipped;
            RefreshAllVisuals();
        }
    }

    public Node3D? GetSpawnedVisual(ItemType slot)
    {
        if (_spawnedVisuals.TryGetValue(slot, out var visual) && GodotObject.IsInstanceValid(visual))
        {
            return visual;
        }
        return null;
    }

    public void RefreshAllVisuals()
    {
        ClearAllVisuals();

        if (_equipment == null)
        {
            return;
        }

        foreach (var pair in _equipment.GetAllEquipped())
        {
            var item = pair.Value;
            if (item != null)
            {
                OnItemEquipped(pair.Key, item);
            }
        }
    }

    public void ClearAllVisuals()
    {
        foreach (var visual in _spawnedVisuals.Values)
        {
            if (GodotObject.IsInstanceValid(visual))
            {
                visual.QueueFree();
            }
        }
        _spawnedVisuals.Clear();
    }

    private void OnItemEquipped(int slotType, ItemData item)
    {
        var type = (ItemType)slotType;
        RemoveVisual(type);

        if (item.WorldVisual == null)
        {
            return;
        }

        BoneAttachment3D? targetSlot = type switch
        {
            ItemType.MainHand => _slotMainHand,
            ItemType.OffHand => _slotOffHand,
            ItemType.Helmet => _slotHead,
            _ => null
        };

        if (targetSlot == null)
        {
            return;
        }

        var instance = item.WorldVisual.Instantiate<Node3D>();
        if (instance == null)
        {
            return;
        }

        ApplySlotTransform(instance, type);
        targetSlot.AddChild(instance);
        _spawnedVisuals[type] = instance;

        GD.Print($"[EquipmentVisuals] Attached visual '{item.ItemName}' to {targetSlot.Name}");
    }

    private void OnItemUnequipped(int slotType)
    {
        var type = (ItemType)slotType;
        RemoveVisual(type);
    }

    private void RemoveVisual(ItemType type)
    {
        if (_spawnedVisuals.TryGetValue(type, out var visual))
        {
            if (GodotObject.IsInstanceValid(visual))
            {
                visual.QueueFree();
            }
            _spawnedVisuals.Remove(type);
            GD.Print($"[EquipmentVisuals] Removed visual for slot {type}");
        }
    }

    private void ApplySlotTransform(Node3D instance, ItemType type)
    {
        Vector3 posOffset = Vector3.Zero;
        Vector3 rotOffset = Vector3.Zero;

        switch (type)
        {
            case ItemType.MainHand:
                posOffset = MainHandPositionOffset;
                rotOffset = MainHandRotationDegrees;
                break;
            case ItemType.OffHand:
                posOffset = OffHandPositionOffset;
                rotOffset = OffHandRotationDegrees;
                break;
            case ItemType.Helmet:
                posOffset = HeadPositionOffset;
                rotOffset = HeadRotationDegrees;
                break;
        }

        instance.Position = posOffset;
        instance.RotationDegrees = rotOffset;
        instance.Scale = Vector3.One;
    }
}
