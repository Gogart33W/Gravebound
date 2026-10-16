using Godot;
using System;
using Gravebound.Player;

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

    // Top Target Health Bar
    private PanelContainer? _targetBarBox;
    private ProgressBar? _targetHealthBar;
    private Label? _targetNameLabel;
    private Label? _targetHpLabel;
    private float _targetBarHideTimer;

    public bool IsModalOpen =>
        (_loreModal != null && _loreModal.Visible) ||
        (_victoryModal != null && _victoryModal.Visible);

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _healthBar = GetNodeOrNull<ProgressBar>("Control/BottomBar/HealthBar");
        _healthLabel = GetNodeOrNull<Label>("Control/BottomBar/HealthBar/HealthLabel");
        _questTitleLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestTitle");
        _questObjectiveLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestObjective");
        _loreModal = GetNodeOrNull<PanelContainer>("Control/Center/LoreModal");
        _loreTitleLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Title");
        _loreBodyLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Body");
        _loreCloseButton = GetNodeOrNull<Button>("Control/Center/LoreModal/VBox/CloseButton");
        _deathScreen = GetNodeOrNull<Control>("Control/DeathScreen");
        _victoryModal = GetNodeOrNull<PanelContainer>("Control/Center/VictoryModal");
        _victoryContinueButton = GetNodeOrNull<Button>("Control/Center/VictoryModal/VBox/ContinueButton");

        _targetBarBox = GetNodeOrNull<PanelContainer>("Control/TopCenter/TargetBarBox");
        _targetHealthBar = GetNodeOrNull<ProgressBar>("Control/TopCenter/TargetBarBox/VBox/TargetHealthBar");
        _targetNameLabel = GetNodeOrNull<Label>("Control/TopCenter/TargetBarBox/VBox/TargetName");
        _targetHpLabel = GetNodeOrNull<Label>("Control/TopCenter/TargetBarBox/VBox/TargetHealthBar/TargetHpLabel");

        if (_loreModal != null) _loreModal.Visible = false;
        if (_deathScreen != null) _deathScreen.Visible = false;
        if (_targetBarBox != null) _targetBarBox.Visible = false;
        if (_victoryModal != null) _victoryModal.Visible = false;

        if (_loreCloseButton != null)
        {
            _loreCloseButton.Pressed += () =>
            {
                if (_loreModal != null) _loreModal.Visible = false;
            };
        }

        if (_victoryContinueButton != null)
        {
            _victoryContinueButton.Pressed += () =>
            {
                if (_victoryModal != null) _victoryModal.Visible = false;
            };
        }

        var player = PlayerController.Instance ?? GetTree().Root.FindChild("Player", true, false) as PlayerController;
        if (player != null)
        {
            player.HealthChanged += OnHealthChanged;
            player.PlayerDied += OnPlayerDied;
            player.PlayerRespawned += OnPlayerRespawned;
            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
        }
    }

    public override void _Process(double delta)
    {
        if (_targetBarHideTimer > 0f)
        {
            _targetBarHideTimer -= (float)delta;
            if (_targetBarHideTimer <= 0f)
            {
                if (_targetBarBox != null) _targetBarBox.Visible = false;
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
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.L || key.Keycode == Key.Escape)
            {
                if (_loreModal != null && _loreModal.Visible)
                {
                    _loreModal.Visible = false;
                    GetViewport().SetInputAsHandled();
                    return;
                }
                else if (key.Keycode == Key.L)
                {
                    ToggleLoreModal();
                    GetViewport().SetInputAsHandled();
                }
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

    public void ShowVictoryModal()
    {
        if (_victoryModal != null)
        {
            _victoryModal.Visible = true;
        }
    }
}
