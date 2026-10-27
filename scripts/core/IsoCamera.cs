using Godot;

namespace Gravebound.Core;

public partial class IsoCamera : Node3D
{
    [Export] public Node3D? Target { get; set; }
    [Export] public float FollowSpeed { get; set; } = 8.0f;
    [Export] public float PitchDegrees { get; set; } = 50.0f;
    [Export] public float YawDegrees { get; set; } = 45.0f;
    [Export] public float Distance { get; set; } = 14.0f;
    [Export] public float MinZoom { get; set; } = 8.0f;
    [Export] public float MaxZoom { get; set; } = 22.0f;
    [Export] public float ZoomSpeed { get; set; } = 2.0f;

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
            GlobalPosition = Target.GlobalPosition;
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
            GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, (float)(FollowSpeed * delta));
        }
    }

    private void ApplyOrientation()
    {
        RotationDegrees = new Vector3(-PitchDegrees, YawDegrees, 0.0f);

        if (_camera != null)
        {
            _camera.Position = new Vector3(0.0f, 0.0f, Distance);
        }
    }
}
