using Godot;
using Gravebound.Actors;
using Gravebound.Combat;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class InventoryUI : CanvasLayer
{
    [Export] public PackedScene? SlotScene { get; set; }

    private Control _windowPanel = null!;
    private GridContainer _inventoryGrid = null!;

    // Equipment Slots
    private ItemSlotUI _slotHelmet = null!;
    private ItemSlotUI _slotBodyArmor = null!;
    private ItemSlotUI _slotGloves = null!;
    private ItemSlotUI _slotBoots = null!;
    private ItemSlotUI _slotMainHand = null!;
    private ItemSlotUI _slotOffHand = null!;
    private ItemSlotUI _slotAmulet = null!;
    private ItemSlotUI _slotRing = null!;

    // Stats Labels
    private Label _statHealth = null!;
    private Label _statMana = null!;
    private Label _statDamage = null!;
    private Label _statArmor = null!;
    private Label _statSpeed = null!;

    private ItemTooltipUI _tooltip = null!;
    private Button _closeButton = null!;
    private Label _capacityLabel = null!;

    private readonly Godot.Collections.Array<ItemSlotUI> _inventorySlots = new();

    private HeroController? _hero;
    private InventoryComponent? _inventory;
    private EquipmentComponent? _equipment;

    public bool IsOpen => _windowPanel?.Visible ?? false;

    public override void _Ready()
    {
        _windowPanel = GetNode<Control>("%WindowPanel");
        _inventoryGrid = GetNode<GridContainer>("%InventoryGrid");
        _tooltip = GetNode<ItemTooltipUI>("%ItemTooltipUI");
        _closeButton = GetNode<Button>("%CloseButton");
        _capacityLabel = GetNode<Label>("%CapacityLabel");

        // Equipment Slots
        _slotHelmet = GetNode<ItemSlotUI>("%SlotHelmet");
        _slotBodyArmor = GetNode<ItemSlotUI>("%SlotBodyArmor");
        _slotGloves = GetNode<ItemSlotUI>("%SlotGloves");
        _slotBoots = GetNode<ItemSlotUI>("%SlotBoots");
        _slotMainHand = GetNode<ItemSlotUI>("%SlotMainHand");
        _slotOffHand = GetNode<ItemSlotUI>("%SlotOffHand");
        _slotAmulet = GetNode<ItemSlotUI>("%SlotAmulet");
        _slotRing = GetNode<ItemSlotUI>("%SlotRing");

        // Stats Labels
        _statHealth = GetNode<Label>("%StatHealth");
        _statMana = GetNode<Label>("%StatMana");
        _statDamage = GetNode<Label>("%StatDamage");
        _statArmor = GetNode<Label>("%StatArmor");
        _statSpeed = GetNode<Label>("%StatSpeed");

        _closeButton.Pressed += () => SetOpen(false);

        // Configure equipment slots
        SetupEquipmentSlot(_slotHelmet, ItemType.Helmet);
        SetupEquipmentSlot(_slotBodyArmor, ItemType.BodyArmor);
        SetupEquipmentSlot(_slotGloves, ItemType.Gloves);
        SetupEquipmentSlot(_slotBoots, ItemType.Boots);
        SetupEquipmentSlot(_slotMainHand, ItemType.MainHand);
        SetupEquipmentSlot(_slotOffHand, ItemType.OffHand);
        SetupEquipmentSlot(_slotAmulet, ItemType.Amulet);
        SetupEquipmentSlot(_slotRing, ItemType.Ring);

        BuildInventoryGrid();

        // Initially closed
        SetOpen(false);

        // Connect to Hero when ready
        CallDeferred(nameof(ConnectToHero));
    }

    private void SetupEquipmentSlot(ItemSlotUI slot, ItemType type)
    {
        slot.IsEquipmentSlot = true;
        slot.EquipmentSlotType = type;
        slot.SlotHovered += OnSlotHovered;
        slot.SlotUnhovered += OnSlotUnhovered;
        slot.EquipSlotRightClicked += OnEquipSlotRightClicked;
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
        _hero = hero;

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

        BuildInventoryGrid();
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

        int capacity = _inventory?.Capacity ?? 40;
        for (int i = 0; i < capacity; i++)
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
                _capacityLabel.Text = $"Місткість: {filledCount} / {_inventory.Capacity}";
            }
        }

        if (_equipment != null)
        {
            _slotHelmet.SetItem(_equipment.GetEquipped(ItemType.Helmet));
            _slotBodyArmor.SetItem(_equipment.GetEquipped(ItemType.BodyArmor));
            _slotGloves.SetItem(_equipment.GetEquipped(ItemType.Gloves));
            _slotBoots.SetItem(_equipment.GetEquipped(ItemType.Boots));
            _slotMainHand.SetItem(_equipment.GetEquipped(ItemType.MainHand));
            _slotOffHand.SetItem(_equipment.GetEquipped(ItemType.OffHand));
            _slotAmulet.SetItem(_equipment.GetEquipped(ItemType.Amulet));
            _slotRing.SetItem(_equipment.GetEquipped(ItemType.Ring));
        }

        UpdateStatsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        if (_hero == null)
        {
            return;
        }

        if (_statHealth != null && _hero.HealthComp != null)
        {
            _statHealth.Text = $"❤️ Здоров'я: {Mathf.CeilToInt(_hero.HealthComp.CurrentHealth)} / {Mathf.CeilToInt(_hero.HealthComp.MaxHealth)}";
        }

        if (_statMana != null && _hero.ResourceComp != null)
        {
            _statMana.Text = $"💧 Мана: {Mathf.CeilToInt(_hero.ResourceComp.CurrentResource)} / {Mathf.CeilToInt(_hero.ResourceComp.MaxResource)}";
        }

        if (_statDamage != null)
        {
            float bonusAtk = _hero.BonusAttackPower;
            string atkStr = bonusAtk > 0.001f
                ? $"{_hero.AttackDamage:0} (+{bonusAtk:0})"
                : $"{_hero.AttackDamage:0}";
            _statDamage.Text = $"⚔ Шкода: {atkStr}";
        }

        if (_statArmor != null)
        {
            float armor = _equipment?.GetTotalStatModifier("armor") ?? 0.0f;
            _statArmor.Text = $"🛡 Броня: {armor:0}";
        }

        if (_statSpeed != null)
        {
            _statSpeed.Text = $"🏃 Швидкість: {_hero.MoveSpeed:0.0}";
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
            // If consumable has health restoration
            if (item.StatModifiers != null && item.StatModifiers.TryGetValue("health_restore", out float restoreAmount))
            {
                _hero?.HealthComp?.Heal(restoreAmount);
            }
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
