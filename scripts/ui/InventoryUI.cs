using Godot;
using Gravebound.Actors;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class InventoryUI : CanvasLayer
{
    [Export] public PackedScene? SlotScene { get; set; }

    private Control _windowPanel = null!;
    private GridContainer _inventoryGrid = null!;
    private ItemSlotUI _slotHelmet = null!;
    private ItemSlotUI _slotMainHand = null!;
    private ItemSlotUI _slotOffHand = null!;
    private ItemTooltipUI _tooltip = null!;
    private Button _closeButton = null!;
    private Label _capacityLabel = null!;

    private readonly Godot.Collections.Array<ItemSlotUI> _inventorySlots = new();

    private InventoryComponent? _inventory;
    private EquipmentComponent? _equipment;

    public bool IsOpen => _windowPanel?.Visible ?? false;

    public override void _Ready()
    {
        _windowPanel = GetNode<Control>("%WindowPanel");
        _inventoryGrid = GetNode<GridContainer>("%InventoryGrid");
        _slotHelmet = GetNode<ItemSlotUI>("%SlotHelmet");
        _slotMainHand = GetNode<ItemSlotUI>("%SlotMainHand");
        _slotOffHand = GetNode<ItemSlotUI>("%SlotOffHand");
        _tooltip = GetNode<ItemTooltipUI>("%ItemTooltipUI");
        _closeButton = GetNode<Button>("%CloseButton");
        _capacityLabel = GetNode<Label>("%CapacityLabel");

        _closeButton.Pressed += () => SetOpen(false);

        // Configure equipment slots
        _slotHelmet.IsEquipmentSlot = true;
        _slotHelmet.EquipmentSlotType = ItemType.Helmet;
        _slotHelmet.SlotHovered += OnSlotHovered;
        _slotHelmet.SlotUnhovered += OnSlotUnhovered;
        _slotHelmet.EquipSlotRightClicked += OnEquipSlotRightClicked;

        _slotMainHand.IsEquipmentSlot = true;
        _slotMainHand.EquipmentSlotType = ItemType.MainHand;
        _slotMainHand.SlotHovered += OnSlotHovered;
        _slotMainHand.SlotUnhovered += OnSlotUnhovered;
        _slotMainHand.EquipSlotRightClicked += OnEquipSlotRightClicked;

        _slotOffHand.IsEquipmentSlot = true;
        _slotOffHand.EquipmentSlotType = ItemType.OffHand;
        _slotOffHand.SlotHovered += OnSlotHovered;
        _slotOffHand.SlotUnhovered += OnSlotUnhovered;
        _slotOffHand.EquipSlotRightClicked += OnEquipSlotRightClicked;

        BuildInventoryGrid();

        // Initially closed
        SetOpen(false);

        // Connect to Hero when ready
        CallDeferred(nameof(ConnectToHero));
    }

    private void ConnectToHero()
    {
        var heroNode = GetTree().GetFirstNodeInGroup("Hero") as HeroController;
        if (heroNode != null)
        {
            SetHero(heroNode);
        }
    }

    public void SetHero(HeroController hero)
    {
        if (_inventory != null)
        {
            _inventory.InventoryUpdated -= RefreshUI;
        }
        if (_equipment != null)
        {
            _equipment.EquipmentUpdated -= RefreshUI;
        }

        _inventory = hero.Inventory;
        _equipment = hero.Equipment;

        if (_inventory != null)
        {
            _inventory.InventoryUpdated += RefreshUI;
        }
        if (_equipment != null)
        {
            _equipment.EquipmentUpdated += RefreshUI;
        }

        RefreshUI();
    }

    private void BuildInventoryGrid()
    {
        foreach (var child in _inventoryGrid.GetChildren())
        {
            child.QueueFree();
        }
        _inventorySlots.Clear();

        SlotScene ??= GD.Load<PackedScene>("res://scenes/ui/ItemSlotUI.tscn");

        for (int i = 0; i < 20; i++)
        {
            var slotUI = SlotScene.Instantiate<ItemSlotUI>();
            slotUI.IsEquipmentSlot = false;
            slotUI.SlotIndex = i;
            slotUI.SlotHovered += OnSlotHovered;
            slotUI.SlotUnhovered += OnSlotUnhovered;
            slotUI.SlotRightClicked += OnInventorySlotRightClicked;

            _inventoryGrid.AddChild(slotUI);
            _inventorySlots.Add(slotUI);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_inventory") ||
            (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo &&
             (keyEvent.Keycode == Key.I || keyEvent.PhysicalKeycode == Key.I)))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && (@event.IsActionPressed("ui_cancel") ||
                 (@event is InputEventKey escKey && escKey.Pressed && !escKey.Echo &&
                  (escKey.Keycode == Key.Escape || escKey.PhysicalKeycode == Key.Escape))))
        {
            SetOpen(false);
            GetViewport().SetInputAsHandled();
        }
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        _windowPanel.Visible = open;
        if (!open)
        {
            _tooltip.HideTooltip();
        }
        else
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (_inventory != null)
        {
            var slots = _inventory.GetSlots();
            int filledCount = 0;
            for (int i = 0; i < _inventorySlots.Count; i++)
            {
                var item = i < slots.Count ? slots[i] : null;
                _inventorySlots[i].SetItem(item);
                if (item != null)
                {
                    filledCount++;
                }
            }

            if (_capacityLabel != null)
            {
                _capacityLabel.Text = $"Bag: {filledCount} / {_inventory.Capacity}";
            }
        }

        if (_equipment != null)
        {
            _slotHelmet.SetItem(_equipment.GetEquipped(ItemType.Helmet));
            _slotMainHand.SetItem(_equipment.GetEquipped(ItemType.MainHand));
            _slotOffHand.SetItem(_equipment.GetEquipped(ItemType.OffHand));
        }
    }

    private void OnInventorySlotRightClicked(int slotIndex)
    {
        if (_inventory == null || _equipment == null)
        {
            return;
        }

        var item = _inventory.GetItem(slotIndex);
        if (item == null)
        {
            return;
        }

        if (item.Type == ItemType.Consumable)
        {
            GD.Print($"[InventoryUI] Consumed {item.ItemName}");
            _inventory.RemoveItem(slotIndex);
            return;
        }

        // Equip item: swap with previously equipped item if any
        if (_equipment.Equip(item, out var oldEquipped))
        {
            _inventory.SetItem(slotIndex, oldEquipped);
        }
    }

    private void OnEquipSlotRightClicked(int slotType)
    {
        if (_inventory == null || _equipment == null)
        {
            return;
        }

        var type = (ItemType)slotType;
        var equippedItem = _equipment.GetEquipped(type);
        if (equippedItem == null)
        {
            return;
        }

        // Try adding unequipped item to bag
        if (_inventory.TryAddItem(equippedItem))
        {
            _equipment.Unequip(type);
        }
        else
        {
            GD.Print("[InventoryUI] Cannot unequip: Inventory is full!");
        }
    }

    private void OnSlotHovered(ItemData item, Vector2 globalPos, bool isEquipSlot)
    {
        _tooltip.ShowItem(item, globalPos, isEquipSlot);
    }

    private void OnSlotUnhovered()
    {
        _tooltip.HideTooltip();
    }
}
