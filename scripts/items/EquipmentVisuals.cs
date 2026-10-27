using Godot;
using System.Collections.Generic;

namespace Gravebound.Items;

[GlobalClass]
public partial class EquipmentVisuals : Node
{
    [Export] public NodePath MainHandSlotPath { get; set; } = new NodePath("../Slot_MainHand");
    [Export] public NodePath OffHandSlotPath { get; set; } = new NodePath("../Slot_OffHand");
    [Export] public NodePath HeadSlotPath { get; set; } = new NodePath("../Slot_Head");
    [Export] public NodePath BodyArmorSlotPath { get; set; } = new NodePath("../Slot_BodyArmor");
    [Export] public NodePath GlovesLeftSlotPath { get; set; } = new NodePath("../Slot_GlovesLeft");
    [Export] public NodePath GlovesRightSlotPath { get; set; } = new NodePath("../Slot_GlovesRight");
    [Export] public NodePath BootsLeftSlotPath { get; set; } = new NodePath("../Slot_BootsLeft");
    [Export] public NodePath BootsRightSlotPath { get; set; } = new NodePath("../Slot_BootsRight");

    [Export] public Vector3 MainHandPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 MainHandRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public Vector3 OffHandPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 OffHandRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public Vector3 HeadPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 HeadRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public Vector3 BodyArmorPositionOffset { get; set; } = Vector3.Zero;
    [Export] public Vector3 BodyArmorRotationDegrees { get; set; } = Vector3.Zero;

    private BoneAttachment3D? _slotMainHand;
    private BoneAttachment3D? _slotOffHand;
    private BoneAttachment3D? _slotHead;
    private BoneAttachment3D? _slotBodyArmor;
    private BoneAttachment3D? _slotGlovesLeft;
    private BoneAttachment3D? _slotGlovesRight;
    private BoneAttachment3D? _slotBootsLeft;
    private BoneAttachment3D? _slotBootsRight;

    private EquipmentComponent? _equipment;
    private readonly Dictionary<ItemType, List<Node3D>> _spawnedVisuals = new();

    public override void _Ready()
    {
        _slotMainHand = GetNodeOrNull<BoneAttachment3D>(MainHandSlotPath);
        _slotOffHand = GetNodeOrNull<BoneAttachment3D>(OffHandSlotPath);
        _slotHead = GetNodeOrNull<BoneAttachment3D>(HeadSlotPath);
        _slotBodyArmor = GetNodeOrNull<BoneAttachment3D>(BodyArmorSlotPath);
        _slotGlovesLeft = GetNodeOrNull<BoneAttachment3D>(GlovesLeftSlotPath);
        _slotGlovesRight = GetNodeOrNull<BoneAttachment3D>(GlovesRightSlotPath);
        _slotBootsLeft = GetNodeOrNull<BoneAttachment3D>(BootsLeftSlotPath);
        _slotBootsRight = GetNodeOrNull<BoneAttachment3D>(BootsRightSlotPath);

        EnsureBoneSlot(_slotMainHand);
        EnsureBoneSlot(_slotOffHand);
        EnsureBoneSlot(_slotHead);
        EnsureBoneSlot(_slotBodyArmor);
        EnsureBoneSlot(_slotGlovesLeft);
        EnsureBoneSlot(_slotGlovesRight);
        EnsureBoneSlot(_slotBootsLeft);
        EnsureBoneSlot(_slotBootsRight);

        _equipment = GetParent()?.GetNodeOrNull<EquipmentComponent>("EquipmentComponent");
        if (_equipment != null)
        {
            _equipment.ItemEquipped += OnItemEquipped;
            _equipment.ItemUnequipped += OnItemUnequipped;
            RefreshAllVisuals();
        }
    }

    private static void EnsureBoneSlot(BoneAttachment3D? slot)
    {
        if (slot == null || !slot.UseExternalSkeleton || string.IsNullOrEmpty(slot.BoneName))
        {
            return;
        }

        if (slot.BoneIdx == -1)
        {
            var skeleton = slot.GetNodeOrNull<Skeleton3D>(slot.ExternalSkeleton);
            if (skeleton != null)
            {
                slot.BoneIdx = skeleton.FindBone(slot.BoneName);
            }
        }
    }

    public Node3D? GetSpawnedVisual(int slotType)
    {
        return GetSpawnedVisual((ItemType)slotType);
    }

    public Node3D? GetSpawnedVisual(ItemType slot)
    {
        if (_spawnedVisuals.TryGetValue(slot, out var list) && list.Count > 0 && GodotObject.IsInstanceValid(list[0]))
        {
            return list[0];
        }
        return null;
    }

    public Godot.Collections.Array<Node3D> GetSpawnedVisuals(int slotType)
    {
        var arr = new Godot.Collections.Array<Node3D>();
        if (_spawnedVisuals.TryGetValue((ItemType)slotType, out var list))
        {
            foreach (var v in list)
            {
                if (GodotObject.IsInstanceValid(v))
                {
                    arr.Add(v);
                }
            }
        }
        return arr;
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
        foreach (var list in _spawnedVisuals.Values)
        {
            foreach (var visual in list)
            {
                if (GodotObject.IsInstanceValid(visual))
                {
                    visual.QueueFree();
                }
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

        var spawnedList = new List<Node3D>();

        switch (type)
        {
            case ItemType.MainHand:
                AttachSingleVisual(_slotMainHand, item.WorldVisual, type, spawnedList);
                break;

            case ItemType.OffHand:
                AttachSingleVisual(_slotOffHand, item.WorldVisual, type, spawnedList);
                break;

            case ItemType.Helmet:
                AttachSingleVisual(_slotHead, item.WorldVisual, type, spawnedList);
                break;

            case ItemType.BodyArmor:
                AttachSingleVisual(_slotBodyArmor, item.WorldVisual, type, spawnedList);
                break;

            case ItemType.Gloves:
                AttachLimbPair(_slotGlovesLeft, _slotGlovesRight, item.WorldVisual, spawnedList);
                break;

            case ItemType.Boots:
                AttachLimbPair(_slotBootsLeft, _slotBootsRight, item.WorldVisual, spawnedList);
                break;
        }

        if (spawnedList.Count > 0)
        {
            _spawnedVisuals[type] = spawnedList;
            GD.Print($"[EquipmentVisuals] Attached visual '{item.ItemName}' ({spawnedList.Count} instances) to slot {type}");
        }
    }

    private void AttachSingleVisual(BoneAttachment3D? slot, PackedScene visualScene, ItemType type, List<Node3D> spawnedList)
    {
        if (slot == null) return;

        var instance = visualScene.Instantiate<Node3D>();
        if (instance == null) return;

        ApplySlotTransform(instance, type);
        slot.AddChild(instance);
        spawnedList.Add(instance);
    }

    private void AttachLimbPair(BoneAttachment3D? leftSlot, BoneAttachment3D? rightSlot, PackedScene visualScene, List<Node3D> spawnedList)
    {
        if (leftSlot != null)
        {
            var leftInst = visualScene.Instantiate<Node3D>();
            if (leftInst != null)
            {
                leftSlot.AddChild(leftInst);
                spawnedList.Add(leftInst);
            }
        }

        if (rightSlot != null)
        {
            var rightInst = visualScene.Instantiate<Node3D>();
            if (rightInst != null)
            {
                // Mirror horizontally for the opposite limb
                rightInst.Scale = new Vector3(-1, 1, 1);
                rightSlot.AddChild(rightInst);
                spawnedList.Add(rightInst);
            }
        }
    }

    private void OnItemUnequipped(int slotType)
    {
        var type = (ItemType)slotType;
        RemoveVisual(type);
    }

    private void RemoveVisual(ItemType type)
    {
        if (_spawnedVisuals.TryGetValue(type, out var list))
        {
            foreach (var visual in list)
            {
                if (GodotObject.IsInstanceValid(visual))
                {
                    visual.QueueFree();
                }
            }
            _spawnedVisuals.Remove(type);
            GD.Print($"[EquipmentVisuals] Removed visuals for slot {type}");
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
            case ItemType.BodyArmor:
                posOffset = BodyArmorPositionOffset;
                rotOffset = BodyArmorRotationDegrees;
                break;
        }

        if (posOffset != Vector3.Zero)
        {
            instance.Position += posOffset;
        }
        if (rotOffset != Vector3.Zero)
        {
            instance.RotationDegrees += rotOffset;
        }
    }
}
