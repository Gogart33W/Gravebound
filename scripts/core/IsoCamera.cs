using Godot;

namespace Gravebound.Core;

public partial class IsoCamera : Node3D
{
    [Export] public Node3D? Target { get; set; }
    [Export] public float FollowSpeed { get; set; } = 10.0f;
    [Export] public float YawDegrees { get; set; } = 45.0f;
    [Export] public float MinPitchDegrees { get; set; } = 20.0f;
    [Export] public float MaxPitchDegrees { get; set; } = 52.0f;
    [Export] public float MinZoom { get; set; } = 3.0f;
    [Export] public float MaxZoom { get; set; } = 20.0f;
    [Export] public float Distance { get; set; } = 13.0f;
    [Export] public float ZoomSpeed { get; set; } = 1.5f;
    [Export] public float MinTargetHeight { get; set; } = 1.15f;
    [Export] public float MaxTargetHeight { get; set; } = 0.8f;

    private Camera3D? _camera;
    private float _targetDistance;

    public override void _Ready()
    {
        _camera = GetNodeOrNull<Camera3D>("Camera3D");
        _targetDistance = Distance;

        if (Target == null)
        {
            Target = GetTree().GetFirstNodeInGroup("Hero") as Node3D;
        }

        if (Target != null)
        {
            GlobalPosition = GetTargetPosition();
        }

        ApplyOrientation();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                _targetDistance = Mathf.Clamp(_targetDistance - ZoomSpeed, MinZoom, MaxZoom);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                _targetDistance = Mathf.Clamp(_targetDistance + ZoomSpeed, MinZoom, MaxZoom);
            }
        }
    }

    public override void _Process(double delta)
    {
        Distance = Mathf.MoveToward(Distance, _targetDistance, (float)(ZoomSpeed * 10.0f * delta));
        ApplyOrientation();

        if (Target != null && IsInstanceValid(Target))
        {
            GlobalPosition = GlobalPosition.Lerp(GetTargetPosition(), (float)(FollowSpeed * delta));
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (Target == null) return GlobalPosition;

        float zoomFactor = Mathf.Clamp((Distance - MinZoom) / (MaxZoom - MinZoom), 0.0f, 1.0f);
        float currentTargetHeight = Mathf.Lerp(MinTargetHeight, MaxTargetHeight, zoomFactor);
        return Target.GlobalPosition + new Vector3(0.0f, currentTargetHeight, 0.0f);
    }

    private void ApplyOrientation()
    {
        float zoomFactor = Mathf.Clamp((Distance - MinZoom) / (MaxZoom - MinZoom), 0.0f, 1.0f);
        float currentPitch = Mathf.Lerp(MinPitchDegrees, MaxPitchDegrees, zoomFactor);

        RotationDegrees = new Vector3(-currentPitch, YawDegrees, 0.0f);

        if (_camera != null)
        {
            _camera.Position = new Vector3(0.0f, 0.0f, Distance);
        }
    }
}
