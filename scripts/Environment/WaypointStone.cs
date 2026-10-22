using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Environment;

public partial class WaypointStone : Area3D
{
    [Export] public string WaypointName { get; set; } = "Обеліск Переміщення";

    private Label3D? _label;
    private OmniLight3D? _glow;
    private bool _playerInRange;

    public override void _Ready()
    {
        CollisionLayer = 8; // Interactive
        CollisionMask = 2;  // Player

        _label = GetNodeOrNull<Label3D>("Label3D");
        _glow = GetNodeOrNull<OmniLight3D>("Glow");

        if (_label != null)
        {
            _label.Text = $"✦ {WaypointName} ✦\n[E: Швидке переміщення]";
            _label.Modulate = new Color(0.4f, 0.85f, 1.0f);
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void Interact()
    {
        SoundManager.Instance?.PlayAltarChime();
        Quests.QuestManager.Instance?.AdvanceQuest("quest_waypoints", 1);
        GraveboundHUD.Instance?.OpenWaypointModal();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = true;
            if (_label != null) _label.Modulate = new Color(0.4f, 1.0f, 0.7f);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = false;
            if (_label != null) _label.Modulate = new Color(0.4f, 0.85f, 1.0f);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange) return;
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        if (@event.IsActionPressed("interact"))
        {
            Interact();
            GetViewport().SetInputAsHandled();
        }
    }
}
