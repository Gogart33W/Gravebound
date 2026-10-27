using Godot;
using Gravebound.Combat;

namespace Gravebound.Actors;

public partial class DummyEnemy : CharacterBody3D, IDamageable
{
    [Export] public bool ResetsOnDeath { get; set; } = true;
    [Export] public float ResetDelay { get; set; } = 1.0f;

    public bool IsDead => _health?.IsDead ?? false;

    private HealthComponent _health = null!;
    private MeshInstance3D? _mesh;
    private StandardMaterial3D? _originalMaterial;
    private StandardMaterial3D? _hitMaterial;
    private double _hitFlashTimer;

    public override void _Ready()
    {
        AddToGroup("Enemies");
        _health = GetNode<HealthComponent>("HealthComponent");
        _health.Died += OnDied;

        _mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (_mesh != null)
        {
            if (_mesh.GetActiveMaterial(0) is StandardMaterial3D standardMat)
            {
                _originalMaterial = (StandardMaterial3D)standardMat.Duplicate();
            }
            else
            {
                _originalMaterial = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.8f, 0.3f, 0.2f)
                };
            }

            _hitMaterial = (StandardMaterial3D)_originalMaterial.Duplicate();
            _hitMaterial.AlbedoColor = new Color(1.0f, 0.9f, 0.9f);
            _hitMaterial.EmissionEnabled = true;
            _hitMaterial.Emission = new Color(1.0f, 0.2f, 0.2f);
            _hitMaterial.EmissionEnergyMultiplier = 2.0f;

            _mesh.MaterialOverride = _originalMaterial;
        }
    }

    public override void _Process(double delta)
    {
        if (_hitFlashTimer > 0.0)
        {
            _hitFlashTimer -= delta;
            if (_hitFlashTimer <= 0.0 && _mesh != null)
            {
                _mesh.MaterialOverride = _originalMaterial;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        _health.TakeDamage(amount);
        GD.Print($"[DummyEnemy] Hit for {amount} damage! Current HP: {_health.CurrentHealth:0.0}/{_health.MaxHealth:0.0}");

        if (_mesh != null && _hitMaterial != null)
        {
            _mesh.MaterialOverride = _hitMaterial;
            _hitFlashTimer = 0.12;
        }
    }

    private async void OnDied()
    {
        GD.Print("[DummyEnemy] Target dummy destroyed!");

        if (ResetsOnDeath)
        {
            // Brief visual fade/disable and restore for continuous training
            Visible = false;
            SetPhysicsProcess(false);
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;

            await ToSignal(GetTree().CreateTimer(ResetDelay), SceneTreeTimer.SignalName.Timeout);

            _health.ResetHealth();
            Visible = true;
            SetPhysicsProcess(true);
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = false;
            GD.Print("[DummyEnemy] Target dummy respawned and ready for combat.");
        }
        else
        {
            QueueFree();
        }
    }
}
