using Godot;
using Gravebound.Actors;
using Gravebound.Combat;

namespace Gravebound.UI;

public partial class DebugCombatUI : CanvasLayer
{
    private ProgressBar _healthBar = null!;
    private ProgressBar _resourceBar = null!;
    private Label _healthLabel = null!;
    private Label _resourceLabel = null!;
    private Label _skillQLabel = null!;
    private Label _skillWLabel = null!;

    private HeroController? _hero;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("Control/Panel/VBox/HealthBar");
        _resourceBar = GetNode<ProgressBar>("Control/Panel/VBox/ResourceBar");
        _healthLabel = GetNode<Label>("Control/Panel/VBox/HealthBar/HealthLabel");
        _resourceLabel = GetNode<Label>("Control/Panel/VBox/ResourceBar/ResourceLabel");
        _skillQLabel = GetNode<Label>("Control/Panel/VBox/SkillsHBox/SkillQ/Label");
        _skillWLabel = GetNode<Label>("Control/Panel/VBox/SkillsHBox/SkillW/Label");

        CallDeferred(nameof(InitHeroConnections));
    }

    private void InitHeroConnections()
    {
        _hero = GetTree().GetFirstNodeInGroup("Hero") as HeroController;
        if (_hero == null)
        {
            return;
        }

        var health = _hero.HealthComp;
        if (health != null)
        {
            health.HealthChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        }

        var resource = _hero.ResourceComp;
        if (resource != null)
        {
            resource.ResourceChanged += OnResourceChanged;
            OnResourceChanged(resource.CurrentResource, resource.MaxResource);
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (_healthBar != null)
        {
            _healthBar.MaxValue = max;
            _healthBar.Value = current;
        }

        if (_healthLabel != null)
        {
            _healthLabel.Text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void OnResourceChanged(float current, float max)
    {
        if (_resourceBar != null)
        {
            _resourceBar.MaxValue = max;
            _resourceBar.Value = current;
        }

        if (_resourceLabel != null)
        {
            _resourceLabel.Text = $"MP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    public override void _Process(double delta)
    {
        if (_hero == null)
        {
            _hero = GetTree().GetFirstNodeInGroup("Hero") as HeroController;
            if (_hero != null)
            {
                InitHeroConnections();
            }
            return;
        }

        // Update Skill Q UI
        if (_skillQLabel != null)
        {
            if (_hero.CooldownQ > 0.0f)
            {
                _skillQLabel.Text = $"[Q] Cleave ({_hero.CooldownQ:F1}s)";
                _skillQLabel.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }
            else
            {
                _skillQLabel.Text = "[Q] Cleave (READY)";
                _skillQLabel.Modulate = new Color(1.0f, 0.6f, 0.2f, 1.0f);
            }
        }

        // Update Skill W UI
        if (_skillWLabel != null)
        {
            if (_hero.CooldownW > 0.0f)
            {
                _skillWLabel.Text = $"[W] Dash ({_hero.CooldownW:F1}s)";
                _skillWLabel.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }
            else
            {
                _skillWLabel.Text = "[W] Dash (READY)";
                _skillWLabel.Modulate = new Color(0.2f, 0.9f, 1.0f, 1.0f);
            }
        }
    }
}
