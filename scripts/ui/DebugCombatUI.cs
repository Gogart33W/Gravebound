using Godot;
using Gravebound.Actors;
using Gravebound.Combat;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class DebugCombatUI : CanvasLayer
{
    private ProgressBar _healthBar = null!;
    private ProgressBar _resourceBar = null!;
    private Label _healthLabel = null!;
    private Label _resourceLabel = null!;
    private Label _statsLabel = null!;
    private Label _skillQLabel = null!;
    private Label _skillWLabel = null!;

    // Target Frame
    private PanelContainer _targetPanel = null!;
    private Label _targetNameLabel = null!;
    private ProgressBar _targetHealthBar = null!;
    private Label _targetHealthLabel = null!;

    private HeroController? _hero;
    private Node3D? _currentTarget;
    private double _targetVisibleTimer;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("Control/Panel/VBox/HealthBar");
        _resourceBar = GetNode<ProgressBar>("Control/Panel/VBox/ResourceBar");
        _healthLabel = GetNode<Label>("Control/Panel/VBox/HealthBar/HealthLabel");
        _resourceLabel = GetNode<Label>("Control/Panel/VBox/ResourceBar/ResourceLabel");
        _statsLabel = GetNode<Label>("Control/Panel/VBox/StatsLabel");
        _skillQLabel = GetNode<Label>("Control/Panel/VBox/SkillsHBox/SkillQ/Label");
        _skillWLabel = GetNode<Label>("Control/Panel/VBox/SkillsHBox/SkillW/Label");

        _targetPanel = GetNode<PanelContainer>("Control/TargetPanel");
        _targetNameLabel = GetNode<Label>("Control/TargetPanel/VBox/TargetNameLabel");
        _targetHealthBar = GetNode<ProgressBar>("Control/TargetPanel/VBox/TargetHealthBar");
        _targetHealthLabel = GetNode<Label>("Control/TargetPanel/VBox/TargetHealthBar/TargetHealthLabel");

        _targetPanel.Visible = false;

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

        if (_hero.Equipment != null)
        {
            _hero.Equipment.EquipmentUpdated += UpdateHeroStatsDisplay;
        }

        UpdateHeroStatsDisplay();
    }

    private void UpdateHeroStatsDisplay()
    {
        if (_hero == null || _statsLabel == null)
        {
            return;
        }

        float bonusAtk = _hero.BonusAttackPower;
        float totalAtk = _hero.AttackDamage;
        float speed = _hero.MoveSpeed;

        string atkStr = bonusAtk > 0.001f
            ? $"⚔ Атака: {totalAtk:0} (+{bonusAtk:0})"
            : $"⚔ Атака: {totalAtk:0}";

        _statsLabel.Text = $"{atkStr} | 🏃 Швидкість: {speed:0.0}";
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

        UpdateHeroStatsDisplay();
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

        // Process Target Frame
        ProcessTargetFrame(delta);
    }

    private void ProcessTargetFrame(double delta)
    {
        if (_hero == null)
        {
            return;
        }

        if (_hero.CurrentAttackTarget != null && GodotObject.IsInstanceValid(_hero.CurrentAttackTarget))
        {
            _currentTarget = _hero.CurrentAttackTarget;
            _targetVisibleTimer = 4.0;
        }

        if (_currentTarget != null && GodotObject.IsInstanceValid(_currentTarget))
        {
            var health = _currentTarget.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null && !health.IsDead)
            {
                _targetPanel.Visible = true;
                _targetHealthBar.MaxValue = health.MaxHealth;
                _targetHealthBar.Value = health.CurrentHealth;

                float heroDmg = Mathf.Max(1.0f, _hero.AttackDamage);
                int hitsLeft = Mathf.CeilToInt(health.CurrentHealth / heroDmg);
                string hitWord = GetHitWord(hitsLeft);

                string name = _currentTarget is DummyEnemy ? "🎯 Тренувальний манекен" : $"🎯 {_currentTarget.Name}";
                _targetNameLabel.Text = name;
                _targetHealthLabel.Text = $"{Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)} HP ({hitsLeft} {hitWord})";
                return;
            }
        }

        if (_targetVisibleTimer > 0.0)
        {
            _targetVisibleTimer -= delta;
            if (_targetVisibleTimer <= 0.0)
            {
                _targetPanel.Visible = false;
                _currentTarget = null;
            }
        }
        else
        {
            _targetPanel.Visible = false;
        }
    }

    private static string GetHitWord(int count)
    {
        int rem100 = count % 100;
        int rem10 = count % 10;

        if (rem100 is >= 11 and <= 19)
        {
            return "ударів";
        }

        return rem10 switch
        {
            1 => "удар",
            2 or 3 or 4 => "удари",
            _ => "ударів"
        };
    }
}
