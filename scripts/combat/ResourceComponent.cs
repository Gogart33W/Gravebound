using Godot;

namespace Gravebound.Combat;

public partial class ResourceComponent : Node
{
    [Signal]
    public delegate void ResourceChangedEventHandler(float current, float max);

    [Export] public float MaxResource { get; set; } = 100.0f;
    [Export] public float ResourceRegenRate { get; set; } = 10.0f;

    public float CurrentResource { get; private set; }

    public override void _Ready()
    {
        CurrentResource = MaxResource;
        EmitSignal(SignalName.ResourceChanged, CurrentResource, MaxResource);
    }

    public override void _Process(double delta)
    {
        if (CurrentResource < MaxResource && ResourceRegenRate > 0.0f)
        {
            Restore(ResourceRegenRate * (float)delta);
        }
    }

    public bool TryConsume(float amount)
    {
        if (amount <= 0.0f)
        {
            return true;
        }

        if (CurrentResource >= amount)
        {
            CurrentResource = Mathf.Max(0.0f, CurrentResource - amount);
            EmitSignal(SignalName.ResourceChanged, CurrentResource, MaxResource);
            return true;
        }

        return false;
    }

    public void Restore(float amount)
    {
        if (amount <= 0.0f)
        {
            return;
        }

        float old = CurrentResource;
        CurrentResource = Mathf.Min(MaxResource, CurrentResource + amount);
        if (!Mathf.IsEqualApprox(old, CurrentResource))
        {
            EmitSignal(SignalName.ResourceChanged, CurrentResource, MaxResource);
        }
    }

    public void ResetResource()
    {
        CurrentResource = MaxResource;
        EmitSignal(SignalName.ResourceChanged, CurrentResource, MaxResource);
    }
}
