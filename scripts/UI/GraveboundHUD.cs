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

    // Character Progression & Stats UI
    private PanelContainer? _characterModal;
    private Button? _characterButton;
    private Button? _characterCloseButton;
    private Label? _levelBadgeLabel;
    private Label? _xpLabel;
    private ProgressBar? _xpBar;
    private Label? _pointsLabel;
    private Label? _strengthLabel;
    private Button? _strengthPlusBtn;
    private Label? _agilityLabel;
    private Button? _agilityPlusBtn;
    private Label? _vitalityLabel;
    private Button? _vitalityPlusBtn;
    private Label? _willpowerLabel;
    private Button? _willpowerPlusBtn;
    private Label? _combatDamageLabel;
    private Label? _combatArmorLabel;
    private Label? _combatHealthLabel;

    // Top Target Health Bar & Loot Notification
    private PanelContainer? _targetBarBox;
    private ProgressBar? _targetHealthBar;
    private Label? _targetNameLabel;
    private Label? _targetHpLabel;
    private float _targetBarHideTimer;
    private Label? _lootToastLabel;
    private float _lootToastTimer;

    // Shop UI
    private PanelContainer? _shopModal;
    private Label? _shopTitleLabel;
    private Label? _shopGoldLabel;
    private Label? _greetingLabel;
    private VBoxContainer? _playerItemsList;
    private VBoxContainer? _waresList;
    private Button? _shopCloseButton;
    private System.Collections.Generic.List<ItemData> _currentShopWares = new();

    // Waypoint UI
    private PanelContainer? _waypointModal;
    private Button? _destHubBtn;
    private Button? _destCryptBtn;
    private Button? _destCatacombsBtn;
    private Button? _waypointCloseButton;

    [Signal] public delegate void ContinuePressedEventHandler();

    public bool IsModalOpen =>
        (_loreModal != null && _loreModal.Visible) ||
        (_victoryModal != null && _victoryModal.Visible) ||
        (_inventoryModal != null && _inventoryModal.Visible) ||
        (_characterModal != null && _characterModal.Visible) ||
        (_shopModal != null && _shopModal.Visible) ||
        (_waypointModal != null && _waypointModal.Visible);

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
        _inventoryButton = GetNodeOrNull<Button>("Control/BottomBar/ButtonsHBox/InventoryButton");
        _characterButton = GetNodeOrNull<Button>("Control/BottomBar/ButtonsHBox/CharacterButton");
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

        // Character Progression nodes
        _characterModal = GetNodeOrNull<PanelContainer>("Control/Center/CharacterModal");
        _characterCloseButton = GetNodeOrNull<Button>("Control/Center/CharacterModal/VBox/CloseButton");
        _levelBadgeLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/HeaderHBox/LevelBadge");
        _xpLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/XpBox/XpLabel");
        _xpBar = GetNodeOrNull<ProgressBar>("Control/Center/CharacterModal/VBox/XpBox/XpBar");
        _pointsLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/PointsLabel");
        _strengthLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/AttributesVBox/StrengthHBox/StrengthLabel");
        _strengthPlusBtn = GetNodeOrNull<Button>("Control/Center/CharacterModal/VBox/AttributesVBox/StrengthHBox/StrengthPlusBtn");
        _agilityLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/AttributesVBox/AgilityHBox/AgilityLabel");
        _agilityPlusBtn = GetNodeOrNull<Button>("Control/Center/CharacterModal/VBox/AttributesVBox/AgilityHBox/AgilityPlusBtn");
        _vitalityLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/AttributesVBox/VitalityHBox/VitalityLabel");
        _vitalityPlusBtn = GetNodeOrNull<Button>("Control/Center/CharacterModal/VBox/AttributesVBox/VitalityHBox/VitalityPlusBtn");
        _willpowerLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/AttributesVBox/WillpowerHBox/WillpowerLabel");
        _willpowerPlusBtn = GetNodeOrNull<Button>("Control/Center/CharacterModal/VBox/AttributesVBox/WillpowerHBox/WillpowerPlusBtn");
        _combatDamageLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/CombatSummaryVBox/CombatDamage");
        _combatArmorLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/CombatSummaryVBox/CombatArmor");
        _combatHealthLabel = GetNodeOrNull<Label>("Control/Center/CharacterModal/VBox/CombatSummaryVBox/CombatHealth");

        // Shop nodes
        _shopModal = GetNodeOrNull<PanelContainer>("Control/Center/ShopModal");
        _shopTitleLabel = GetNodeOrNull<Label>("Control/Center/ShopModal/VBox/HeaderHBox/ShopTitle");
        _shopGoldLabel = GetNodeOrNull<Label>("Control/Center/ShopModal/VBox/HeaderHBox/ShopGoldLabel");
        _greetingLabel = GetNodeOrNull<Label>("Control/Center/ShopModal/VBox/GreetingLabel");
        _playerItemsList = GetNodeOrNull<VBoxContainer>("Control/Center/ShopModal/VBox/ContentHBox/PlayerBagVBox/PlayerScroll/PlayerItemsList");
        _waresList = GetNodeOrNull<VBoxContainer>("Control/Center/ShopModal/VBox/ContentHBox/MerchantWaresVBox/WaresScroll/WaresList");
        _shopCloseButton = GetNodeOrNull<Button>("Control/Center/ShopModal/VBox/ShopCloseButton");

        // Waypoint nodes
        _waypointModal = GetNodeOrNull<PanelContainer>("Control/Center/WaypointModal");
        _destHubBtn = GetNodeOrNull<Button>("Control/Center/WaypointModal/VBox/DestHubBtn");
        _destCryptBtn = GetNodeOrNull<Button>("Control/Center/WaypointModal/VBox/DestCryptBtn");
        _destCatacombsBtn = GetNodeOrNull<Button>("Control/Center/WaypointModal/VBox/DestCatacombsBtn");
        _waypointCloseButton = GetNodeOrNull<Button>("Control/Center/WaypointModal/VBox/WaypointCloseButton");

        if (_loreModal != null) _loreModal.Visible = false;
        if (_deathScreen != null) _deathScreen.Visible = false;
        if (_targetBarBox != null) _targetBarBox.Visible = false;
        if (_victoryModal != null) _victoryModal.Visible = false;
        if (_inventoryModal != null) _inventoryModal.Visible = false;
        if (_characterModal != null) _characterModal.Visible = false;
        if (_shopModal != null) _shopModal.Visible = false;
        if (_waypointModal != null) _waypointModal.Visible = false;
        if (_lootToastLabel != null) _lootToastLabel.Visible = false;

        if (_shopCloseButton != null)
        {
            _shopCloseButton.Pressed += () =>
            {
                if (_shopModal != null) _shopModal.Visible = false;
            };
        }

        if (_waypointCloseButton != null)
        {
            _waypointCloseButton.Pressed += () =>
            {
                if (_waypointModal != null) _waypointModal.Visible = false;
            };
        }

        if (_destHubBtn != null)
        {
            _destHubBtn.Pressed += () =>
            {
                if (_waypointModal != null) _waypointModal.Visible = false;
                GetTree().ChangeSceneToFile("res://scenes/HubTown.tscn");
            };
        }

        if (_destCryptBtn != null)
        {
            _destCryptBtn.Pressed += () =>
            {
                if (_waypointModal != null) _waypointModal.Visible = false;
                GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
            };
        }

        if (_destCatacombsBtn != null)
        {
            _destCatacombsBtn.Pressed += () =>
            {
                if (_waypointModal != null) _waypointModal.Visible = false;
                GetTree().ChangeSceneToFile("res://scenes/Catacombs.tscn");
            };
        }

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

        if (_characterButton != null)
        {
            _characterButton.Pressed += ToggleCharacterStats;
        }

        if (_characterCloseButton != null)
        {
            _characterCloseButton.Pressed += () =>
            {
                if (_characterModal != null) _characterModal.Visible = false;
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

        // Stat plus buttons
        if (_strengthPlusBtn != null) _strengthPlusBtn.Pressed += () => PlayerProgression.Instance?.AllocatePoint("strength");
        if (_agilityPlusBtn != null) _agilityPlusBtn.Pressed += () => PlayerProgression.Instance?.AllocatePoint("agility");
        if (_vitalityPlusBtn != null) _vitalityPlusBtn.Pressed += () => PlayerProgression.Instance?.AllocatePoint("vitality");
        if (_willpowerPlusBtn != null) _willpowerPlusBtn.Pressed += () => PlayerProgression.Instance?.AllocatePoint("willpower");

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

        // Hook Singletons deferred
        Callable.From(HookSingletons).CallDeferred();
    }

    private void HookSingletons()
    {
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            inv.InventoryChanged += RefreshInventoryUI;
            inv.EquipmentChanged += RefreshInventoryUI;
            inv.EquipmentChanged += RefreshCharacterStatsUI;
            inv.GoldChanged += (gold) => UpdateGold(gold);
            inv.ItemPickedUp += OnItemPickedUp;
            UpdateGold(inv.Gold);
        }

        var prog = PlayerProgression.Instance;
        if (prog != null)
        {
            prog.XPChanged += (cur, req, lvl) => RefreshXP(cur, req, lvl);
            prog.LevelUp += (lvl) => OnLevelUp(lvl);
            prog.StatsChanged += RefreshCharacterStatsUI;
            prog.StatsChanged += RefreshInventoryUI;
            RefreshXP(prog.CurrentXP, prog.XPRequiredForNextLevel, prog.Level);
        }
    }

    private void InitBagGrid()
    {
        if (_bagGrid == null) return;

        foreach (Node child in _bagGrid.GetChildren())
        {
            child.QueueFree();
        }

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
            if (_characterModal != null) _characterModal.Visible = false;
            RefreshInventoryUI();
        }
    }

    public void ToggleCharacterStats()
    {
        if (_characterModal == null) return;
        _characterModal.Visible = !_characterModal.Visible;
        if (_characterModal.Visible)
        {
            if (_inventoryModal != null) _inventoryModal.Visible = false;
            RefreshCharacterStatsUI();
        }
    }

    public void RefreshXP(int current, int required, int level)
    {
        if (_xpBar != null)
        {
            _xpBar.MaxValue = required;
            _xpBar.Value = current;
        }
        if (_xpLabel != null)
        {
            _xpLabel.Text = $"Досвід (XP): {current} / {required}";
        }
        if (_levelBadgeLabel != null)
        {
            _levelBadgeLabel.Text = $"Рівень: {level}";
        }
    }

    public void OnLevelUp(int newLevel)
    {
        ShowToast($"✦ РІВЕНЬ ПІДВИЩЕНО! Рівень {newLevel} ✦ (+5 очок характеристик)", new Color(1.0f, 0.85f, 0.2f));
        RefreshCharacterStatsUI();
    }

    public void RefreshCharacterStatsUI()
    {
        var prog = PlayerProgression.Instance;
        var inv = InventoryManager.Instance;
        var player = PlayerController.Instance;
        if (prog == null) return;

        if (_levelBadgeLabel != null) _levelBadgeLabel.Text = $"Рівень: {prog.Level}";
        if (_pointsLabel != null) _pointsLabel.Text = $"✦ Доступно очок для розподілу: {prog.UnspentPoints}";

        // Show/hide [+] buttons based on unspent points
        bool hasPoints = prog.UnspentPoints > 0;
        if (_strengthPlusBtn != null) _strengthPlusBtn.Visible = hasPoints;
        if (_agilityPlusBtn != null) _agilityPlusBtn.Visible = hasPoints;
        if (_vitalityPlusBtn != null) _vitalityPlusBtn.Visible = hasPoints;
        if (_willpowerPlusBtn != null) _willpowerPlusBtn.Visible = hasPoints;

        if (_strengthLabel != null)
            _strengthLabel.Text = $"⚔️ Сила: {prog.Strength} (+{(prog.Strength - 10) * 1.5f:0.#} до фіз. атаки)";
        if (_agilityLabel != null)
            _agilityLabel.Text = $"🏹 Спритність: {prog.Agility} (+{(prog.Agility - 10) * 0.5f:0.#} до атаки/криту)";
        if (_vitalityLabel != null)
            _vitalityLabel.Text = $"❤️ Живучість: {prog.Vitality} (+{(prog.Vitality - 10) * 8} Max HP)";
        if (_willpowerLabel != null)
            _willpowerLabel.Text = $"🔮 Воля: {prog.Willpower} (+{(prog.Willpower - 10) * 2}% магічного супротиву)";

        if (player != null)
        {
            int totalDmg = player.GetTotalAttackDamage();
            int totalArmor = player.GetTotalArmor();
            int totalMaxHp = player.GetTotalMaxHealth();

            if (_combatDamageLabel != null)
                _combatDamageLabel.Text = $"Загальна Атака: {totalDmg} (База {player.AttackDamage} + Стати {prog.GetBonusDamage()} + Лут {inv?.GetBonusDamage() ?? 0})";
            if (_combatArmorLabel != null)
                _combatArmorLabel.Text = $"Загальна Броня: {totalArmor} (Зниження шкоди на {Mathf.Min(75, totalArmor * 2)}%)";
            if (_combatHealthLabel != null)
                _combatHealthLabel.Text = $"Загальне Здоров'я: {player.CurrentHealth} / {totalMaxHp} (+{prog.GetBonusHealth()} від Живучості)";

            OnHealthChanged(player.CurrentHealth, totalMaxHp);
        }
    }

    public void RefreshInventoryUI()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        UpdateEquipSlotButton(_headSlotBtn, ItemSlotType.Head, "🪖 Шолом");
        UpdateEquipSlotButton(_chestSlotBtn, ItemSlotType.Chest, "🥋 Лати");
        UpdateEquipSlotButton(_weaponSlotBtn, ItemSlotType.Weapon, "⚔️ Зброя");
        UpdateEquipSlotButton(_shieldSlotBtn, ItemSlotType.Shield, "🛡️ Щит");
        UpdateEquipSlotButton(_ringSlotBtn, ItemSlotType.Ring, "💍 Перстень");

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

        var player = PlayerController.Instance;
        var prog = PlayerProgression.Instance;
        if (player != null)
        {
            int bonusDmg = (inv.GetBonusDamage()) + (prog?.GetBonusDamage() ?? 0);
            int bonusArmor = inv.GetBonusArmor();
            int bonusHp = (inv.GetBonusHealth()) + (prog?.GetBonusHealth() ?? 0);

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
        if (_shopGoldLabel != null)
        {
            _shopGoldLabel.Text = $"💰 {amount} Золота";
        }
    }

    public void OnItemPickedUp(string name, string colorHex)
    {
        ShowToast($"✦ Знайдено: {name}", Color.FromHtml(colorHex));
    }

    public void ShowToast(string text, Color color)
    {
        if (_lootToastLabel != null)
        {
            _lootToastLabel.Text = text;
            _lootToastLabel.Modulate = color;
            _lootToastLabel.Visible = true;
            _lootToastTimer = 3.5f;
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

        if (@event.IsActionPressed("toggle_character_stats"))
        {
            ToggleCharacterStats();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            if (_shopModal != null && _shopModal.Visible)
            {
                _shopModal.Visible = false;
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_waypointModal != null && _waypointModal.Visible)
            {
                _waypointModal.Visible = false;
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_characterModal != null && _characterModal.Visible)
            {
                _characterModal.Visible = false;
                GetViewport().SetInputAsHandled();
                return;
            }

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

    public void OpenShop(string npcName, string role, string greeting, System.Collections.Generic.List<ItemData> wares)
    {
        if (_inventoryModal != null) _inventoryModal.Visible = false;
        if (_characterModal != null) _characterModal.Visible = false;
        if (_loreModal != null) _loreModal.Visible = false;
        if (_waypointModal != null) _waypointModal.Visible = false;

        _currentShopWares = wares;
        if (_shopTitleLabel != null) _shopTitleLabel.Text = $"🏪 {npcName.ToUpper()} — {role}";
        if (_greetingLabel != null) _greetingLabel.Text = greeting;

        RefreshShopUI();

        if (_shopModal != null) _shopModal.Visible = true;
    }

    public void RefreshShopUI()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        if (_shopGoldLabel != null)
        {
            _shopGoldLabel.Text = $"💰 {inv.Gold} Золота";
        }

        // Render player items for sale
        if (_playerItemsList != null)
        {
            foreach (Node child in _playerItemsList.GetChildren())
            {
                child.QueueFree();
            }

            bool hasAny = false;
            for (int i = 0; i < InventoryManager.MaxSlots; i++)
            {
                int slotIdx = i;
                var item = inv.Slots[slotIdx];
                if (item == null) continue;
                hasAny = true;

                int sellPrice = Mathf.Max(1, item.ValueGold / 2);
                var btn = new Button();
                btn.CustomMinimumSize = new Vector2(0, 32);
                btn.AddThemeFontSizeOverride("font_size", 12);
                btn.AddThemeColorOverride("font_color", item.GetRarityColor());
                btn.Text = $"{item.ItemName} (+{sellPrice} 💰)";
                btn.TooltipText = $"{item.Description}\n{item.GetFormattedStats()}\nЦіна продажу: {sellPrice} золота";
                btn.Pressed += () =>
                {
                    inv.RemoveItem(slotIdx);
                    inv.AddGold(sellPrice);
                    Audio.SoundManager.Instance?.PlayAltarChime();
                    ShowToast($"Продано: {item.ItemName} (+{sellPrice} 💰)", new Color(1f, 0.85f, 0.3f));
                    RefreshShopUI();
                };
                _playerItemsList.AddChild(btn);
            }

            if (!hasAny)
            {
                var emptyLbl = new Label();
                emptyLbl.Text = "(Сумка порожня)";
                emptyLbl.Modulate = new Color(0.6f, 0.6f, 0.6f);
                _playerItemsList.AddChild(emptyLbl);
            }
        }

        // Render merchant wares
        if (_waresList != null)
        {
            foreach (Node child in _waresList.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var ware in _currentShopWares)
            {
                var item = ware;
                var btn = new Button();
                btn.CustomMinimumSize = new Vector2(0, 32);
                btn.AddThemeFontSizeOverride("font_size", 12);
                btn.AddThemeColorOverride("font_color", item.GetRarityColor());
                btn.Text = $"{item.ItemName} ({item.ValueGold} 💰)";
                btn.TooltipText = $"{item.Description}\n{item.GetFormattedStats()}\nЦіна купівлі: {item.ValueGold} золота";
                btn.Pressed += () =>
                {
                    if (inv.Gold < item.ValueGold)
                    {
                        ShowToast("Недостатньо золота для покупки!", new Color(1f, 0.3f, 0.3f));
                        return;
                    }
                    if (!inv.HasFreeSlot())
                    {
                        ShowToast("Ваша сумка переповнена (20/20)!", new Color(1f, 0.3f, 0.3f));
                        return;
                    }

                    inv.AddGold(-item.ValueGold);
                    inv.AddItem(item.Clone());
                    Audio.SoundManager.Instance?.PlayAltarChime();
                    ShowToast($"Придбано: {item.ItemName} (-{item.ValueGold} 💰)", new Color(0.35f, 1f, 0.6f));
                    RefreshShopUI();
                };
                _waresList.AddChild(btn);
            }
        }
    }

    public void OpenWaypointModal()
    {
        if (_inventoryModal != null) _inventoryModal.Visible = false;
        if (_characterModal != null) _characterModal.Visible = false;
        if (_loreModal != null) _loreModal.Visible = false;
        if (_shopModal != null) _shopModal.Visible = false;

        if (_waypointModal != null) _waypointModal.Visible = true;
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
