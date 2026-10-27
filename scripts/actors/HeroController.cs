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

    [Export] public SkillBase? SkillQ { get; set; }
    [Export] public SkillBase? SkillW { get; set; }

    public float CooldownQ => _cooldownQTimer;
    public float CooldownW => _cooldownWTimer;
    public float MaxCooldownQ => SkillQ?.Cooldown ?? 0.0f;
    public float MaxCooldownW => SkillW?.Cooldown ?? 0.0f;

    public HealthComponent? HealthComp => _healthComponent;
    public ResourceComponent? ResourceComp => _resourceComponent;

    private NavigationAgent3D _navAgent = null!;
    private HeroAnimator? _animator;
    private HealthComponent? _healthComponent;
    private ResourceComponent? _resourceComponent;

    private bool _isNavigating;
    private Vector3 _currentTarget;
    private Node3D? _attackTarget;
    private float _attackCooldownTimer;
    private float _cooldownQTimer;
    private float _cooldownWTimer;
    private bool _isHoldingLmb;
    private bool _isSingleAttack;

    // Dash state
    private bool _isDashing;
    private Vector3 _dashDirection;
    private float _dashSpeed;
    private float _dashTimeRemaining;
    private float _invulnerableTimeRemaining;
    private float _castLockTimer;

    public override void _Ready()
    {
        AddToGroup("Hero");
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _animator = GetNodeOrNull<HeroAnimator>("HeroAnimator");
        _healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        _resourceComponent = GetNodeOrNull<ResourceComponent>("ResourceComponent");

        SkillQ ??= new Skill_MeleeCleave();
        SkillW ??= new Skill_Dash();

        _navAgent.PathDesiredDistance = 0.6f;
        _navAgent.TargetDesiredDistance = 0.6f;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                _isHoldingLmb = true;
                SetTargetFromMouse(isClick: true);
            }
            else
            {
                _isHoldingLmb = false;
            }
            return;
        }

        // Skills trigger on action or physical key
        if (@event.IsActionPressed("skill_q") || IsKeyJustPressed(@event, Key.Q))
        {
            CastSkillQ();
        }
        else if (@event.IsActionPressed("skill_w") || @event.IsActionPressed("skill_space") ||
                 IsKeyJustPressed(@event, Key.W) || IsKeyJustPressed(@event, Key.Space))
        {
            CastSkillW();
        }
    }

    private static bool IsKeyJustPressed(InputEvent @event, Key key)
    {
        return @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo &&
               (keyEvent.Keycode == key || keyEvent.PhysicalKeycode == key);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_attackCooldownTimer > 0.0f)
        {
            _attackCooldownTimer -= dt;
        }

        if (_cooldownQTimer > 0.0f)
        {
            _cooldownQTimer -= dt;
        }

        if (_cooldownWTimer > 0.0f)
        {
            _cooldownWTimer -= dt;
        }

        // Handle invulnerability timer from dash
        if (_invulnerableTimeRemaining > 0.0f)
        {
            _invulnerableTimeRemaining -= dt;
            if (_invulnerableTimeRemaining <= 0.0f && _healthComponent != null)
            {
                _healthComponent.IsInvulnerable = false;
            }
        }

        // Handle active dash
        if (_isDashing)
        {
            _dashTimeRemaining -= dt;
            Velocity = new Vector3(_dashDirection.X * _dashSpeed, Velocity.Y, _dashDirection.Z * _dashSpeed);
            ApplyGravity(delta);
            MoveAndSlide();

            if (_dashTimeRemaining <= 0.0f)
            {
                _isDashing = false;
                Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            }
            return;
        }

        // Handle cast lock (e.g. executing stationary melee cleave)
        if (_castLockTimer > 0.0f)
        {
            _castLockTimer -= dt;
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            ApplyGravity(delta);
            MoveAndSlide();
            return;
        }

        // Check if attack target is valid and still alive
        if (_attackTarget != null)
        {
            bool isAlive = IsInstanceValid(_attackTarget);
            if (isAlive)
            {
                if (_attackTarget is DummyEnemy dummy)
                {
                    isAlive = !dummy.IsDead && dummy.Visible;
                }
                else
                {
                    var health = _attackTarget.GetNodeOrNull<HealthComponent>("HealthComponent");
                    isAlive = health == null || !health.IsDead;
                }
            }

            if (!isAlive)
            {
                // Target is dead or despawned: instantly DROP focus! No hitting the air!
                _attackTarget = null;
                _isSingleAttack = false;
                _isNavigating = false;
                Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            }
        }

        // If holding LMB without an enemy target, follow cursor on ground continuously
        if (_isHoldingLmb && _attackTarget == null)
        {
            SetTargetFromMouse(isClick: false);
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

            // Attack logic: continue if holding LMB or perform single requested attack
            if (_isHoldingLmb || _isSingleAttack)
            {
                if (_attackCooldownTimer <= 0.0f)
                {
                    _attackCooldownTimer = AttackCooldown;
                    _animator?.PlayAttack();

                    if (_attackTarget is IDamageable damageable)
                    {
                        damageable.TakeDamage(AttackDamage);
                    }

                    // If single click attack, complete and release target
                    if (_isSingleAttack)
                    {
                        _isSingleAttack = false;
                        _attackTarget = null;
                    }
                    else if (_attackTarget is DummyEnemy dummy && (dummy.IsDead || !dummy.Visible))
                    {
                        _attackTarget = null;
                    }
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

    public void CastSkillQ()
    {
        TryCastSkill(SkillQ, ref _cooldownQTimer, null);
    }

    public void CastSkillQAt(Vector3 targetPos)
    {
        TryCastSkill(SkillQ, ref _cooldownQTimer, targetPos);
    }

    public void CastSkillW()
    {
        TryCastSkill(SkillW, ref _cooldownWTimer, null);
    }

    public void CastSkillWAt(Vector3 targetPos)
    {
        TryCastSkill(SkillW, ref _cooldownWTimer, targetPos);
    }

    private bool TryCastSkill(SkillBase? skill, ref float cooldownTimer, Vector3? targetOverride = null)
    {
        if (skill == null || cooldownTimer > 0.0f || _isDashing || _castLockTimer > 0.0f)
        {
            return false;
        }

        if (_resourceComponent != null && _resourceComponent.CurrentResource < skill.ResourceCost)
        {
            GD.Print($"[HeroController] Not enough resource for {skill.SkillName}! Requires {skill.ResourceCost}, have {_resourceComponent.CurrentResource}");
            return false;
        }

        Vector3 targetPos = targetOverride ?? (_attackTarget != null ? _attackTarget.GlobalPosition : GetMouseWorldPosition());
        if (skill.Cast(this, targetPos, _attackTarget))
        {
            _resourceComponent?.TryConsume(skill.ResourceCost);
            cooldownTimer = skill.Cooldown;
            _isNavigating = false;
            Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
            if (skill is not Skill_Dash)
            {
                _castLockTimer = 0.45f;
            }
            return true;
        }

        return false;
    }

    public void StartDash(Vector3 direction, float speed, float duration, float invulnerableDuration)
    {
        _isDashing = true;
        _dashDirection = direction.Normalized();
        _dashSpeed = speed;
        _dashTimeRemaining = duration;
        _invulnerableTimeRemaining = invulnerableDuration;

        _attackTarget = null;
        _isSingleAttack = false;
        _isNavigating = false;

        if (_healthComponent != null)
        {
            _healthComponent.IsInvulnerable = true;
        }
    }

    public Vector3 GetMouseWorldPosition()
    {
        Camera3D? camera = GetViewport().GetCamera3D();
        if (camera != null)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
            Vector3 rayNormal = camera.ProjectRayNormal(mousePos);
            Vector3 rayEnd = rayOrigin + (rayNormal * 1000.0f);

            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, 1u); // Ground layer 1
            var result = spaceState.IntersectRay(query);
            if (result.Count > 0 && result.TryGetValue("position", out Variant hitPosVar))
            {
                return hitPosVar.AsVector3();
            }

            // Fallback plane at Y = GlobalPosition.Y
            Plane groundPlane = new Plane(Vector3.Up, GlobalPosition.Y);
            Vector3? planeHit = groundPlane.IntersectsRay(rayOrigin, rayNormal);
            if (planeHit.HasValue)
            {
                return planeHit.Value;
            }
        }

        return GlobalPosition - (GlobalTransform.Basis.Z * 4.0f);
    }

    private void SetTargetFromMouse(bool isClick)
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
                bool isAlive = true;
                if (targetNode3D is DummyEnemy dummy)
                {
                    isAlive = !dummy.IsDead && dummy.Visible;
                }

                if (isAlive)
                {
                    AttackTarget(targetNode3D, isSingleAttack: isClick);
                    GD.Print($"[HeroController] Targeted enemy: {targetNode3D.Name} at {_currentTarget}");
                    return;
                }
            }

            // Clicked on ground or obstacle
            MoveTo(hitPosVariant.AsVector3());
        }
    }

    public void AttackTarget(Node3D target, bool isSingleAttack = false)
    {
        _attackTarget = target;
        _isSingleAttack = isSingleAttack;
        _currentTarget = target.GlobalPosition;
        _navAgent.TargetPosition = _currentTarget;
        _isNavigating = true;
    }

    public void MoveTo(Vector3 destination)
    {
        if (_castLockTimer > 0.0f)
        {
            return;
        }

        _attackTarget = null;
        _isSingleAttack = false;
        _currentTarget = destination;
        _navAgent.TargetPosition = destination;
        _isNavigating = true;
        _animator?.StopOneShot();
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
