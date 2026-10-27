using Godot;

namespace Gravebound.Actors;

public partial class HeroController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float RotationSpeed { get; set; } = 12.0f;
    [Export] public uint GroundCollisionMask { get; set; } = 1u;

    private NavigationAgent3D _navAgent = null!;
    private bool _isNavigating;
    private Vector3 _currentTarget;

    public override void _Ready()
    {
        AddToGroup("Hero");
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _navAgent.PathDesiredDistance = 0.6f;
        _navAgent.TargetDesiredDistance = 0.6f;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
        {
            SetTargetFromMouse();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Continuous movement while holding LMB
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            SetTargetFromMouse();
        }

        if (!_isNavigating)
        {
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        // Distance to final destination on horizontal XZ plane
        float horizontalDistance = new Vector2(GlobalPosition.X - _currentTarget.X, GlobalPosition.Z - _currentTarget.Z).Length();

        // Arrived at destination: stop cleanly
        if (horizontalDistance <= _navAgent.TargetDesiredDistance)
        {
            _isNavigating = false;
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        Vector3 nextPathPos = _navAgent.GetNextPathPosition();
        Vector3 direction = nextPathPos - GlobalPosition;
        direction.Y = 0.0f;

        // Fallback to direct target vector if waypoint is right under agent
        if (direction.LengthSquared() < 0.04f)
        {
            direction = _currentTarget - GlobalPosition;
            direction.Y = 0.0f;
        }

        if (direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();

            // Prevent overshoot near target
            float currentSpeed = Mathf.Min(MoveSpeed, horizontalDistance * 4.0f);
            Velocity = new Vector3(direction.X * currentSpeed, Velocity.Y, direction.Z * currentSpeed);

            // Smooth rotation towards travel direction (only when not at destination)
            if (horizontalDistance > _navAgent.TargetDesiredDistance + 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                float currentAngle = Rotation.Y;
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, (float)(RotationSpeed * delta));
                Rotation = new Vector3(Rotation.X, newAngle, Rotation.Z);
            }
        }
        else
        {
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
        }

        ApplyGravity(delta);
        MoveAndSlide();
    }

    private void SetTargetFromMouse()
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
            Vector3 hitPos = hitPosVariant.AsVector3();
            _currentTarget = hitPos;
            _navAgent.TargetPosition = hitPos;
            _isNavigating = true;
        }
    }

    private void ApplyGravity(double delta)
    {
        if (!IsOnFloor())
        {
            Velocity += Vector3.Down * 9.8f * (float)delta;
        }
    }
}
