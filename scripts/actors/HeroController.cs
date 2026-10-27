using Godot;

namespace Gravebound.Actors;

public partial class HeroController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float RotationSpeed { get; set; } = 12.0f;
    [Export] public uint GroundCollisionMask { get; set; } = 1u;

    private NavigationAgent3D _navAgent = null!;
    private bool _isHoldingLmb;

    public override void _Ready()
    {
        AddToGroup("Hero");
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _navAgent.PathDesiredDistance = 0.5f;
        _navAgent.TargetDesiredDistance = 0.5f;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isHoldingLmb = mouseButton.Pressed;
            if (_isHoldingLmb)
            {
                UpdateNavigationTarget();
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isHoldingLmb)
        {
            UpdateNavigationTarget();
        }

        if (_navAgent.IsNavigationFinished())
        {
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
        Vector3 direction = nextPathPosition - GlobalPosition;
        direction.Y = 0.0f;

        if (direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();
            Velocity = new Vector3(direction.X * MoveSpeed, Velocity.Y, direction.Z * MoveSpeed);

            float targetAngle = Mathf.Atan2(direction.X, direction.Z);
            float currentAngle = Rotation.Y;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, (float)(RotationSpeed * delta));
            Rotation = new Vector3(Rotation.X, newAngle, Rotation.Z);
        }
        else
        {
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
        }

        ApplyGravity(delta);
        MoveAndSlide();
    }

    private void ApplyGravity(double delta)
    {
        if (!IsOnFloor())
        {
            Velocity += Vector3.Down * 9.8f * (float)delta;
        }
    }

    private void UpdateNavigationTarget()
    {
        Camera3D? camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            return;
        }

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
        Vector3 rayNormal = camera.ProjectRayNormal(mousePos);
        Vector3 rayEnd = rayOrigin + (rayNormal * 1000.0f);

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, GroundCollisionMask);

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0 && result.TryGetValue("position", out Variant hitPosVariant))
        {
            _navAgent.TargetPosition = hitPosVariant.AsVector3();
        }
    }
}
