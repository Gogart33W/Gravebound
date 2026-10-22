using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Environment;

public partial class WorldGate : Area3D
{
    [Export(PropertyHint.File, "*.tscn")] public string TargetScenePath { get; set; } = "res://scenes/HubTown.tscn";
    [Export] public string GateName { get; set; } = "Перехід між землями";
    [Export] public bool AutoEnterOnTouch { get; set; } = false;

    private Label3D? _label;
    private OmniLight3D? _glow;
    private bool _playerInRange;
    private bool _isTransitioning;

    public override void _Ready()
    {
        CollisionLayer = 8; // Interactive
        CollisionMask = 2;  // Player

        _label = GetNodeOrNull<Label3D>("Label3D");
        _glow = GetNodeOrNull<OmniLight3D>("Glow");

        if (_label != null)
        {
            _label.Text = $"✦ {GateName} ✦\n[E: Перейти]";
            _label.Modulate = new Color(0.95f, 0.85f, 0.45f);
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void Interact()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        SoundManager.Instance?.PlayAltarChime();
        GraveboundHUD.Instance?.ShowToast($"Перехід: {GateName}...", new Color(0.9f, 0.85f, 0.5f));
        GetTree().ChangeSceneToFile(TargetScenePath);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = true;
            if (_label != null) _label.Modulate = new Color(0.4f, 1.0f, 0.7f);

            if (AutoEnterOnTouch)
            {
                Interact();
            }
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = false;
            if (_label != null) _label.Modulate = new Color(0.95f, 0.85f, 0.45f);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange || _isTransitioning) return;
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        if (@event.IsActionPressed("interact"))
        {
            Interact();
            GetViewport().SetInputAsHandled();
        }
    }
}
