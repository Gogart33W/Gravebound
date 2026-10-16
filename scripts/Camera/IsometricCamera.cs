using Godot;
using System;

namespace Gravebound.Camera;

public partial class IsometricCamera : Camera3D
{
    [Export] public NodePath? TargetPath { get; set; }
    [Export] public Vector3 CameraOffset { get; set; } = new Vector3(0.0f, 13.5f, 11.5f);
    [Export] public float FollowSpeed { get; set; } = 8.0f;
    [Export] public float LookAtHeight { get; set; } = 1.0f;
    [Export] public float MinZoom { get; set; } = 0.6f;
    [Export] public float MaxZoom { get; set; } = 1.6f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;

    private Node3D? _target;
    private float _zoomLevel = 1.0f;

    public override void _Ready()
    {
        if (TargetPath != null && !TargetPath.IsEmpty)
        {
            _target = GetNodeOrNull<Node3D>(TargetPath);
        }
        else
        {
            _target = GetTree().Root.FindChild("Player", true, false) as Node3D;
        }

        if (_target != null)
        {
            GlobalPosition = _target.GlobalPosition + CameraOffset * _zoomLevel;
            LookAt(_target.GlobalPosition + Vector3.Up * LookAtHeight, Vector3.Up);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                _zoomLevel = Mathf.Clamp(_zoomLevel - ZoomSpeed, MinZoom, MaxZoom);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                _zoomLevel = Mathf.Clamp(_zoomLevel + ZoomSpeed, MinZoom, MaxZoom);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null)
        {
            _target = GetTree().Root.FindChild("Player", true, false) as Node3D;
            if (_target == null) return;
        }

        Vector3 targetPos = _target.GlobalPosition;
        Vector3 desiredPos = targetPos + CameraOffset * _zoomLevel;

        GlobalPosition = GlobalPosition.Lerp(desiredPos, FollowSpeed * (float)delta);
        LookAt(targetPos + Vector3.Up * LookAtHeight, Vector3.Up);
    }
}
