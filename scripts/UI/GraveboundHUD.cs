using Godot;
using System;
using Gravebound.Player;
using Gravebound.Inventory;

namespace Gravebound.UI;

public partial class GraveboundHUD : CanvasLayer
{
    public static GraveboundHUD? Instance { get; private set; }

    private ProgressBar? _healthBar;
    private Label? _healthLabel;
    private Label? _questTitleLabel;
    private Label? _questObjectiveLabel;
    private PanelContainer? _loreModal;
    private Label? _loreTitleLabel;
    private Label? _loreBodyLabel;
    private Button? _loreCloseButton;
    private Control? _deathScreen;
    private PanelContainer? _victoryModal;
    private Button? _victoryContinueButton;
    private Label? _victoryTitleLabel;
    private Label? _victoryBodyLabel;

    // Inventory & Equipment UI
    private PanelContainer? _inventoryModal;
    private Button? _inventoryButton;
    private Button? _inventoryCloseButton;
    private Label? _goldLabel;
    private Label? _statsDamageLabel;
    private Label? _statsArmorLabel;
    private Label? _statsHealthLabel;
    private Label? _detailLabel;
    private GridContainer? _bagGrid;
    private Button? _headSlotBtn;
    private Button? _chestSlotBtn;
    private Button? _weaponSlotBtn;
    private Button? _shieldSlotBtn;
    private Button? _ringSlotBtn;

    // Top Target Health Bar & Loot Notification
    private PanelContainer? _targetBarBox;
    private ProgressBar? _targetHealthBar;
    private Label? _targetNameLabel;
    private Label? _targetHpLabel;
    private float _targetBarHideTimer;
    private Label? _lootToastLabel;
    private float _lootToastTimer;

    [Signal] public delegate void ContinuePressedEventHandler();

    public bool IsModalOpen =>
        (_loreModal != null && _loreModal.Visible) ||
        (_victoryModal != null && _victoryModal.Visible) ||
        (_inventoryModal != null && _inventoryModal.Visible);

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
        _healthBar = GetNodeOrNull<ProgressBar>("Control/BottomBar/HealthBar");
        _healthLabel = GetNodeOrNull<Label>("Control/BottomBar/HealthBar/HealthLabel");
        _inventoryButton = GetNodeOrNull<Button>("Control/BottomBar/InventoryButton");
        _questTitleLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestTitle");
        _questObjectiveLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestObjective");
        _loreModal = GetNodeOrNull<PanelContainer>("Control/Center/LoreModal");
        _loreTitleLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Title");
        _loreBodyLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Body");
        _loreCloseButton = GetNodeOrNull<Button>("Control/Center/LoreModal/VBox/CloseButton");
        _deathScreen = GetNodeOrNull<Control>("Control/DeathScreen");
        _victoryModal = GetNodeOrNull<PanelContainer>("Control/Center/VictoryModal");
        _victoryContinueButton = GetNodeOrNull<Button>("Control/Center/VictoryModal/VBox/ContinueButton");
        _victoryTitleLabel = GetNodeOrNull<Label>("Control/Center/VictoryModal/VBox/Title");
        _victoryBodyLabel = GetNodeOrNull<Label>("Control/Center/VictoryModal/VBox/Body");

        _targetBarBox = GetNodeOrNull<PanelContainer>("Control/TopCenter/TargetBarBox");
        _targetHealthBar = GetNodeOrNull<ProgressBar>("Control/TopCenter/TargetBarBox/VBox/TargetHealthBar");
        _targetNameLabel = GetNodeOrNull<Label>("Control/TopCenter/TargetBarBox/VBox/TargetName");
        _targetHpLabel = GetNodeOrNull<Label>("Control/TopCenter/TargetBarBox/VBox/TargetHealthBar/TargetHpLabel");
        _lootToastLabel = GetNodeOrNull<Label>("Control/LootToastLabel");

        // Inventory nodes
        _inventoryModal = GetNodeOrNull<PanelContainer>("Control/Center/InventoryModal");
        _inventoryCloseButton = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/CloseButton");
        _goldLabel = GetNodeOrNull<Label>("Control/Center/InventoryModal/VBox/HeaderHBox/GoldLabel");
        _headSlotBtn = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/HeadSlot");
        _chestSlotBtn = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/ChestSlot");
        _weaponSlotBtn = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/WeaponSlot");
        _shieldSlotBtn = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/ShieldSlot");
        _ringSlotBtn = GetNodeOrNull<Button>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/RingSlot");
        _statsDamageLabel = GetNodeOrNull<Label>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/StatsPanel/StatsVBox/StatsDamageLabel");
        _statsArmorLabel = GetNodeOrNull<Label>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/StatsPanel/StatsVBox/StatsArmorLabel");
        _statsHealthLabel = GetNodeOrNull<Label>("Control/Center/InventoryModal/VBox/ContentHBox/EquipVBox/StatsPanel/StatsVBox/StatsHealthLabel");
        _bagGrid = GetNodeOrNull<GridContainer>("Control/Center/InventoryModal/VBox/ContentHBox/BagVBox/Grid");
        _detailLabel = GetNodeOrNull<Label>("Control/Center/InventoryModal/VBox/ContentHBox/BagVBox/ItemDetailPanel/DetailLabel");

        if (_loreModal != null) _loreModal.Visible = false;
        if (_deathScreen != null) _deathScreen.Visible = false;
        if (_targetBarBox != null) _targetBarBox.Visible = false;
        if (_victoryModal != null) _victoryModal.Visible = false;
        if (_inventoryModal != null) _inventoryModal.Visible = false;
        if (_lootToastLabel != null) _lootToastLabel.Visible = false;

        if (_loreCloseButton != null)
        {
            _loreCloseButton.Pressed += () =>
            {
                if (_loreModal != null) _loreModal.Visible = false;
            };
        }

        if (_inventoryButton != null)
        {
            _inventoryButton.Pressed += ToggleInventory;
        }

        if (_inventoryCloseButton != null)
        {
            _inventoryCloseButton.Pressed += () =>
            {
                if (_inventoryModal != null) _inventoryModal.Visible = false;
            };
        }

        if (_victoryContinueButton != null)
        {
            _victoryContinueButton.Pressed += () =>
            {
                if (_victoryModal != null) _victoryModal.Visible = false;
                EmitSignal(SignalName.ContinuePressed);
            };
        }

        // Equipment slot unequip clicks
        if (_headSlotBtn != null) _headSlotBtn.Pressed += () => InventoryManager.Instance?.UnequipItem(ItemSlotType.Head);
        if (_chestSlotBtn != null) _chestSlotBtn.Pressed += () => InventoryManager.Instance?.UnequipItem(ItemSlotType.Chest);
        if (_weaponSlotBtn != null) _weaponSlotBtn.Pressed += () => InventoryManager.Instance?.UnequipItem(ItemSlotType.Weapon);
        if (_shieldSlotBtn != null) _shieldSlotBtn.Pressed += () => InventoryManager.Instance?.UnequipItem(ItemSlotType.Shield);
        if (_ringSlotBtn != null) _ringSlotBtn.Pressed += () => InventoryManager.Instance?.UnequipItem(ItemSlotType.Ring);

        // Populate Bag grid buttons
        InitBagGrid();

        var player = PlayerController.Instance ?? GetTree().Root.FindChild("Player", true, false) as PlayerController;
        if (player != null)
        {
            player.HealthChanged += OnHealthChanged;
            player.PlayerDied += OnPlayerDied;
            player.PlayerRespawned += OnPlayerRespawned;
            OnHealthChanged(player.CurrentHealth, player.GetTotalMaxHealth());
        }

        // Hook InventoryManager signals
        Callable.From(HookInventory).CallDeferred();
    }

    private void HookInventory()
    {
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            inv.InventoryChanged += RefreshInventoryUI;
            inv.EquipmentChanged += RefreshInventoryUI;
            inv.GoldChanged += (gold) => UpdateGold(gold);
            inv.ItemPickedUp += OnItemPickedUp;
            UpdateGold(inv.Gold);
        }
    }

    private void InitBagGrid()
    {
        if (_bagGrid == null) return;

        // Clear existing children
        foreach (Node child in _bagGrid.GetChildren())
        {
            child.QueueFree();
        }

        // Create 20 slot buttons
        for (int i = 0; i < InventoryManager.MaxSlots; i++)
        {
            int slotIdx = i;
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(90, 48);
            btn.AddThemeFontSizeOverride("font_size", 11);
            btn.Text = "[Порожньо]";
            btn.MouseEntered += () => ShowSlotTooltip(slotIdx);
            btn.Pressed += () =>
            {
                InventoryManager.Instance?.EquipItem(slotIdx);
                ShowSlotTooltip(slotIdx);
            };
            _bagGrid.AddChild(btn);
        }
    }

    public void ToggleInventory()
    {
        if (_inventoryModal == null) return;
        _inventoryModal.Visible = !_inventoryModal.Visible;
        if (_inventoryModal.Visible)
        {
            RefreshInventoryUI();
        }
    }

    public void RefreshInventoryUI()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        // Update Equipment buttons
        UpdateEquipSlotButton(_headSlotBtn, ItemSlotType.Head, "🪖 Шолом");
        UpdateEquipSlotButton(_chestSlotBtn, ItemSlotType.Chest, "🥋 Лати");
        UpdateEquipSlotButton(_weaponSlotBtn, ItemSlotType.Weapon, "⚔️ Зброя");
        UpdateEquipSlotButton(_shieldSlotBtn, ItemSlotType.Shield, "🛡️ Щит");
        UpdateEquipSlotButton(_ringSlotBtn, ItemSlotType.Ring, "💍 Перстень");

        // Update Bag grid
        if (_bagGrid != null)
        {
            var children = _bagGrid.GetChildren();
            for (int i = 0; i < children.Count && i < InventoryManager.MaxSlots; i++)
            {
                if (children[i] is Button slotBtn)
                {
                    var item = inv.Slots[i];
                    if (item != null)
                    {
                        slotBtn.Text = item.ItemName;
                        slotBtn.Modulate = item.GetRarityColor();
                    }
                    else
                    {
                        slotBtn.Text = "[Порожньо]";
                        slotBtn.Modulate = new Color(0.6f, 0.6f, 0.6f);
                    }
                }
            }
        }

        // Update Stats summary
        var player = PlayerController.Instance;
        if (player != null)
        {
            int bonusDmg = inv.GetBonusDamage();
            int bonusArmor = inv.GetBonusArmor();
            int bonusHp = inv.GetBonusHealth();

            if (_statsDamageLabel != null)
                _statsDamageLabel.Text = $"Атака: {player.AttackDamage} (+{bonusDmg}) -> {player.GetTotalAttackDamage()}";
            if (_statsArmorLabel != null)
                _statsArmorLabel.Text = $"Броня: 0 (+{bonusArmor}) -> {player.GetTotalArmor()}";
            if (_statsHealthLabel != null)
                _statsHealthLabel.Text = $"Здоров'я: {player.CurrentHealth} / {player.GetTotalMaxHealth()} (+{bonusHp})";

            OnHealthChanged(player.CurrentHealth, player.GetTotalMaxHealth());
        }
    }

    private void UpdateEquipSlotButton(Button? btn, ItemSlotType slotType, string prefix)
    {
        if (btn == null) return;
        var inv = InventoryManager.Instance;
        if (inv != null && inv.Equipped.TryGetValue(slotType, out var item))
        {
            btn.Text = $"{prefix}: {item.ItemName}";
            btn.Modulate = item.GetRarityColor();
        }
        else
        {
            btn.Text = $"{prefix}: [Порожньо]";
            btn.Modulate = new Color(0.65f, 0.65f, 0.65f);
        }
    }

    private void ShowSlotTooltip(int slotIdx)
    {
        if (_detailLabel == null) return;
        var inv = InventoryManager.Instance;
        if (inv != null && slotIdx >= 0 && slotIdx < InventoryManager.MaxSlots)
        {
            var item = inv.Slots[slotIdx];
            if (item != null)
            {
                _detailLabel.Text = $"✦ {item.ItemName} ({item.Rarity})\n{item.Description}\n[ {item.GetFormattedStats()} ] | Ціна: {item.ValueGold} зол.";
                _detailLabel.Modulate = item.GetRarityColor();
                return;
            }
        }
        _detailLabel.Text = "Порожній слот торби.";
        _detailLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
    }

    public void UpdateGold(int amount)
    {
        if (_goldLabel != null)
        {
            _goldLabel.Text = $"💰 {amount} Золота";
        }
    }

    public void OnItemPickedUp(string name, string colorHex)
    {
        if (_lootToastLabel != null)
        {
            _lootToastLabel.Text = $"✦ Знайдено: {name}";
            _lootToastLabel.Modulate = Color.FromHtml(colorHex);
            _lootToastLabel.Visible = true;
            _lootToastTimer = 3.0f;
        }
    }

    public override void _Process(double delta)
    {
        if (_targetBarHideTimer > 0f)
        {
            _targetBarHideTimer -= (float)delta;
            if (_targetBarHideTimer <= 0f && _targetBarBox != null)
            {
                _targetBarBox.Visible = false;
            }
        }

        if (_lootToastTimer > 0f)
        {
            _lootToastTimer -= (float)delta;
            if (_lootToastTimer <= 0f && _lootToastLabel != null)
            {
                _lootToastLabel.Visible = false;
            }
        }
    }

    public void ShowTargetEnemy(string name, int currentHp, int maxHp)
    {
        if (_targetBarBox != null) _targetBarBox.Visible = true;
        if (_targetNameLabel != null) _targetNameLabel.Text = name;
        if (_targetHealthBar != null)
        {
            _targetHealthBar.MaxValue = maxHp;
            _targetHealthBar.Value = Mathf.Max(0, currentHp);
        }
        if (_targetHpLabel != null)
        {
            _targetHpLabel.Text = $"{Mathf.Max(0, currentHp)} / {maxHp}";
        }
        _targetBarHideTimer = 3.5f;
    }

    public void ClearTargetEnemy()
    {
        if (_targetBarBox != null) _targetBarBox.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_inventory"))
        {
            ToggleInventory();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            if (_inventoryModal != null && _inventoryModal.Visible)
            {
                _inventoryModal.Visible = false;
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_loreModal != null && _loreModal.Visible)
            {
                _loreModal.Visible = false;
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    private void OnPlayerDied()
    {
        if (_deathScreen != null)
        {
            _deathScreen.Visible = true;
        }
    }

    private void OnPlayerRespawned()
    {
        if (_deathScreen != null)
        {
            _deathScreen.Visible = false;
        }
    }

    public void OnHealthChanged(int current, int max)
    {
        if (_healthBar != null)
        {
            _healthBar.MaxValue = max;
            _healthBar.Value = current;
        }
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"{current} / {max}";
        }
    }

    public void UpdateQuest(string title, string objective)
    {
        if (_questTitleLabel != null) _questTitleLabel.Text = title;
        if (_questObjectiveLabel != null) _questObjectiveLabel.Text = objective;
    }

    public void ShowLore(string title, string text)
    {
        if (_loreTitleLabel != null) _loreTitleLabel.Text = title;
        if (_loreBodyLabel != null) _loreBodyLabel.Text = text;
        if (_loreModal != null) _loreModal.Visible = true;
    }

    public void ToggleLoreModal()
    {
        if (_loreModal != null)
        {
            _loreModal.Visible = !_loreModal.Visible;
        }
    }

    public void ConfigureVictory(string title, string body, string buttonText)
    {
        if (_victoryTitleLabel != null) _victoryTitleLabel.Text = title;
        if (_victoryBodyLabel != null) _victoryBodyLabel.Text = body;
        if (_victoryContinueButton != null) _victoryContinueButton.Text = buttonText;
    }

    public void ShowVictoryModal()
    {
        if (_victoryModal != null)
        {
            _victoryModal.Visible = true;
        }
    }
}
