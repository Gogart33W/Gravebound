using Godot;

namespace Gravebound.Combat;

public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    [Export] public float MaxHealth { get; set; } = 100.0f;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0.0f;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0.0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0.0f, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0.0f)
        {
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0.0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
