using Godot;
using Gravebound.Actors;
using Gravebound.Combat;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class EnemyHealthBar3D : Node3D
{
    [Export] public string EnemyName { get; set; } = "Манекен";
    [Export] public NodePath? HealthComponentPath { get; set; }

    private SubViewport _subViewport = null!;
    private Sprite3D _sprite3D = null!;
    private ProgressBar _progressBar = null!;
    private Label _nameLabel = null!;
    private Label _hpLabel = null!;

    private HealthComponent? _health;
    private HeroController? _hero;
    private EquipmentComponent? _equipment;

    public override void _Ready()
    {
        _subViewport = GetNode<SubViewport>("SubViewport");
        _sprite3D = GetNode<Sprite3D>("Sprite3D");
        _progressBar = GetNode<ProgressBar>("SubViewport/Control/VBox/ProgressBar");
        _nameLabel = GetNode<Label>("SubViewport/Control/VBox/NameLabel");
        _hpLabel = GetNode<Label>("SubViewport/Control/VBox/ProgressBar/HPLabel");

        _sprite3D.Texture = _subViewport.GetTexture();

        _nameLabel.Text = EnemyName;

        FindHealthComponent();
        ConnectHero();
        UpdateDisplay();
    }

    private void FindHealthComponent()
    {
        if (HealthComponentPath != null && !HealthComponentPath.IsEmpty)
        {
            _health = GetNodeOrNull<HealthComponent>(HealthComponentPath);
        }

        if (_health == null)
        {
            _health = GetParent()?.GetNodeOrNull<HealthComponent>("HealthComponent");
        }

        if (_health != null)
        {
            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;
        }
    }

    private void ConnectHero()
    {
        _hero = GetTree().GetFirstNodeInGroup("Hero") as HeroController;
        if (_hero != null)
        {
            _equipment = _hero.GetNodeOrNull<EquipmentComponent>("EquipmentComponent");
            if (_equipment != null)
            {
                _equipment.EquipmentUpdated += OnHeroStatsChanged;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_hero == null)
        {
            _hero = GetTree().GetFirstNodeInGroup("Hero") as HeroController;
            if (_hero != null)
            {
                ConnectHero();
                UpdateDisplay();
            }
        }
    }

    private void OnHeroStatsChanged()
    {
        UpdateDisplay();
    }

    private void OnHealthChanged(float current, float max)
    {
        UpdateDisplay();
    }

    private void OnDied()
    {
        Visible = false;
    }

    public void UpdateDisplay()
    {
        if (_health == null || _progressBar == null || _hpLabel == null)
        {
            return;
        }

        Visible = !_health.IsDead;
        if (!Visible)
        {
            return;
        }

        float cur = _health.CurrentHealth;
        float max = _health.MaxHealth;

        _progressBar.MaxValue = max;
        _progressBar.Value = cur;

        float heroDamage = 25.0f;
        if (_hero != null)
        {
            heroDamage = Mathf.Max(1.0f, _hero.AttackDamage);
        }

        int hitsLeft = Mathf.CeilToInt(cur / heroDamage);
        string hitWord = GetHitWord(hitsLeft);

        _hpLabel.Text = $"{Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(max)} HP ({hitsLeft} {hitWord})";
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
