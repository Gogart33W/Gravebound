using Godot;
using Gravebound.Combat;

namespace Gravebound.Actors;

public partial class HeroController : CharacterBody3D, IDamageable
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float RotationSpeed { get; set; } = 12.0f;
    [Export] public float AttackRange { get; set; } = 2.0f;
    [Export] public float AttackCooldown { get; set; } = 0.8f;
    [Export] public float AttackDamage { get; set; } = 25.0f;
    [Export] public uint CombatRaycastMask { get; set; } = 5u; // Layer 1 (Ground=1) | Layer 3 (Enemies=4)

    private NavigationAgent3D _navAgent = null!;
    private HeroAnimator? _animator;
    private HealthComponent? _healthComponent;
    private bool _isNavigating;
    private Vector3 _currentTarget;
    private Node3D? _attackTarget;
    private float _attackCooldownTimer;

    public override void _Ready()
    {
        AddToGroup("Hero");
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _animator = GetNodeOrNull<HeroAnimator>("HeroAnimator");
        _healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");

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
        if (_attackCooldownTimer > 0.0f)
        {
            _attackCooldownTimer -= (float)delta;
        }

        // Continuous movement/targeting while holding LMB (only if not targeting specific enemy)
        if (Input.IsMouseButtonPressed(MouseButton.Left) && _attackTarget == null)
        {
            SetTargetFromMouse();
        }

        // If target was destroyed or freed, clear reference
        if (_attackTarget != null && !IsInstanceValid(_attackTarget))
        {
            _attackTarget = null;
            _isNavigating = false;
        }

        if (!_isNavigating && _attackTarget == null)
        {
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        // Target following logic
        if (_attackTarget != null)
        {
            _currentTarget = _attackTarget.GlobalPosition;
            _navAgent.TargetPosition = _currentTarget;
        }

        float horizontalDistance = new Vector2(GlobalPosition.X - _currentTarget.X, GlobalPosition.Z - _currentTarget.Z).Length();

        // Check if close enough to attack target enemy
        if (_attackTarget != null && horizontalDistance <= AttackRange)
        {
            _isNavigating = false;
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);

            // Rotate smoothly towards attack target
            Vector3 toTarget = _attackTarget.GlobalPosition - GlobalPosition;
            toTarget.Y = 0.0f;
            if (toTarget.LengthSquared() > 0.001f)
            {
                float targetAngle = Mathf.Atan2(toTarget.X, toTarget.Z);
                float newAngle = Mathf.LerpAngle(Rotation.Y, targetAngle, (float)(RotationSpeed * delta));
                Rotation = new Vector3(Rotation.X, newAngle, Rotation.Z);
            }

            // Perform primary attack if off cooldown
            if (_attackCooldownTimer <= 0.0f)
            {
                _attackCooldownTimer = AttackCooldown;
                _animator?.PlayAttack();

                if (_attackTarget is IDamageable damageable)
                {
                    damageable.TakeDamage(AttackDamage);
                }
            }

            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        // Normal movement arrival check
        float stopDistance = _attackTarget != null ? AttackRange : _navAgent.TargetDesiredDistance;
        if (horizontalDistance <= stopDistance)
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

        if (direction.LengthSquared() < 0.04f)
        {
            direction = _currentTarget - GlobalPosition;
            direction.Y = 0.0f;
        }

        if (direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();

            float currentSpeed = Mathf.Min(MoveSpeed, horizontalDistance * 4.0f);
            Velocity = new Vector3(direction.X * currentSpeed, Velocity.Y, direction.Z * currentSpeed);

            if (horizontalDistance > stopDistance + 0.1f)
            {
                float targetAngle = Mathf.Atan2(direction.X, direction.Z);
                float newAngle = Mathf.LerpAngle(Rotation.Y, targetAngle, (float)(RotationSpeed * delta));
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
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, CombatRaycastMask);

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0 && result.TryGetValue("position", out Variant hitPosVariant))
        {
            var colliderObj = result["collider"].AsGodotObject();

            // Check if clicked an enemy / IDamageable
            IDamageable? damageable = colliderObj as IDamageable
                                   ?? (colliderObj is Node node ? node.GetParent() as IDamageable : null);

            if (damageable != null && colliderObj is Node3D targetNode3D)
            {
                AttackTarget(targetNode3D);
                GD.Print($"[HeroController] Targeted enemy: {targetNode3D.Name} at {_currentTarget}");
            }
            else
            {
                // Clicked on ground or obstacle
                MoveTo(hitPosVariant.AsVector3());
            }
        }
    }

    public void AttackTarget(Node3D target)
    {
        _attackTarget = target;
        _currentTarget = target.GlobalPosition;
        _navAgent.TargetPosition = _currentTarget;
        _isNavigating = true;
    }

    public void MoveTo(Vector3 destination)
    {
        _attackTarget = null;
        _currentTarget = destination;
        _navAgent.TargetPosition = destination;
        _isNavigating = true;
    }

    public void TakeDamage(float amount)
    {
        _healthComponent?.TakeDamage(amount);
    }

    private void ApplyGravity(double delta)
    {
        if (!IsOnFloor())
        {
            Velocity += Vector3.Down * 9.8f * (float)delta;
        }
        else if (Velocity.Y < 0.0f)
        {
            Velocity = new Vector3(Velocity.X, -0.1f, Velocity.Z);
        }
    }
}
