using Godot;
using System;
using Gravebound.Player;

namespace Gravebound.UI;

public partial class GraveboundHUD : CanvasLayer
{
    private ProgressBar? _healthBar;
    private Label? _healthLabel;
    private Label? _questTitleLabel;
    private Label? _questObjectiveLabel;
    private PanelContainer? _loreModal;
    private Label? _loreTitleLabel;
    private Label? _loreBodyLabel;
    private Control? _deathScreen;

    public override void _Ready()
    {
        _healthBar = GetNodeOrNull<ProgressBar>("Control/BottomBar/HealthBar");
        _healthLabel = GetNodeOrNull<Label>("Control/BottomBar/HealthBar/HealthLabel");
        _questTitleLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestTitle");
        _questObjectiveLabel = GetNodeOrNull<Label>("Control/TopRight/QuestBox/VBox/QuestObjective");
        _loreModal = GetNodeOrNull<PanelContainer>("Control/Center/LoreModal");
        _loreTitleLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Title");
        _loreBodyLabel = GetNodeOrNull<Label>("Control/Center/LoreModal/VBox/Body");
        _deathScreen = GetNodeOrNull<Control>("Control/DeathScreen");

        if (_loreModal != null)
        {
            _loreModal.Visible = false;
        }

        if (_deathScreen != null)
        {
            _deathScreen.Visible = false;
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.L)
            {
                ToggleLoreModal();
            }
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
}
