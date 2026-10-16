using Godot;
using System;
using Gravebound.Enemies;

namespace Gravebound.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 6.5f;
    [Export] public float Acceleration { get; set; } = 24.0f;
    [Export] public float RotationSpeed { get; set; } = 14.0f;
    [Export] public int AttackDamage { get; set; } = 25;
    [Export] public float AttackRange { get; set; } = 2.4f;
    [Export] public NodePath? NavigationAgentPath { get; set; }
    [Export] public NodePath? AnimationPlayerPath { get; set; }
    [Export] public NodePath? ClickMarkerPath { get; set; }

    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    public bool IsDead => _isDead;

    private NavigationAgent3D? _navAgent;
    private AnimationPlayer? _animPlayer;
    private Node3D? _clickMarker;
    private bool _isHoldingMouse;
    private bool _isAttacking;
    private bool _isDead;
    private float _attackCooldown;
    private float _clickMarkerPulse;
    private string _currentAnim = "";

    private Vector3 _targetDestination;
    private bool _hasTargetDestination;
    private Vector3 _initialSpawnPos;

    public static PlayerController? Instance { get; private set; }

    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void PlayerRespawnedEventHandler();
    [Signal] public delegate void QuestProgressEventHandler(string objective, string details);

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _initialSpawnPos = GlobalPosition;
        _targetDestination = GlobalPosition;
        _navAgent = NavigationAgentPath != null && !NavigationAgentPath.IsEmpty
            ? GetNodeOrNull<NavigationAgent3D>(NavigationAgentPath)
            : GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");

        _animPlayer = AnimationPlayerPath != null && !AnimationPlayerPath.IsEmpty
            ? GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath)
            : FindAnimationPlayer(this);

        _clickMarker = ClickMarkerPath != null && !ClickMarkerPath.IsEmpty
            ? GetNodeOrNull<Node3D>(ClickMarkerPath)
            : GetTree().Root.FindChild("ClickMarker", true, false) as Node3D;

        if (_clickMarker != null)
        {
            _clickMarker.Visible = false;
        }

        Callable.From(ActorSetup).CallDeferred();
        PlayAnimation("Idle");
    }

    private async void ActorSetup()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (_navAgent != null)
        {
            _navAgent.TargetPosition = GlobalPosition;
        }
    }

    private AnimationPlayer? FindAnimationPlayer(Node root)
    {
        if (root is AnimationPlayer ap) return ap;
        foreach (Node child in root.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null) return found;
        }
        return null;
    }

    public override void _Input(InputEvent @event)
    {
        if (_isDead)
        {
            // Allow respawn on space or left click when dead
            if ((@event is InputEventKey deadKey && deadKey.Pressed && deadKey.Keycode == Key.Space) ||
                (@event is InputEventMouseButton deadMb && deadMb.Pressed && deadMb.ButtonIndex == MouseButton.Left))
            {
                Respawn(_initialSpawnPos);
            }
            return;
        }

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                _isHoldingMouse = mb.Pressed;
                if (mb.Pressed)
                {
                    UpdateMoveDestination();
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                PerformAttack();
            }
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space)
            {
                PerformAttack();
            }
            else if (key.Keycode == Key.F11)
            {
                ToggleFullscreen();
            }
        }
    }

    public void ToggleFullscreen()
    {
        var currentMode = DisplayServer.WindowGetMode();
        if (currentMode == DisplayServer.WindowMode.Fullscreen || currentMode == DisplayServer.WindowMode.ExclusiveFullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
    }

    public void PerformAttack()
    {
        if (_isDead || _isAttacking || _attackCooldown > 0f) return;

        _isAttacking = true;
        _attackCooldown = 0.55f;
        Velocity = Vector3.Zero;
        _hasTargetDestination = false;

        // Face towards mouse cursor when attacking
        FaceMouseCursor();

        PlayAnimation("1H_Melee_Attack_Chop", true);

        // Execute attack damage at the apex of the swing
        GetTree().CreateTimer(0.25f).Timeout += () =>
        {
            if (!_isDead)
            {
                ExecuteMeleeStrike();
            }
        };
    }

    private void FaceMouseCursor()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayDir = camera.ProjectRayNormal(mousePos);
        var floorPlane = new Plane(Vector3.Up, GlobalPosition.Y);
        var hit = floorPlane.IntersectsRay(rayOrigin, rayDir);

        if (hit.HasValue)
        {
            Vector3 lookDir = hit.Value - GlobalPosition;
            lookDir.Y = 0;
            if (lookDir.LengthSquared() > 0.05f)
            {
                float targetAngle = Mathf.Atan2(lookDir.X, lookDir.Z);
                Rotation = new Vector3(0, targetAngle, 0);
            }
        }
    }

    private void ExecuteMeleeStrike()
    {
        // Direction player is facing (Model front is +Z)
        Vector3 forward = new Vector3(Mathf.Sin(Rotation.Y), 0, Mathf.Cos(Rotation.Y)).Normalized();
        var enemies = GetTree().GetNodesInGroup("enemies");

        foreach (Node node in enemies)
        {
            if (node is SkeletonEnemy skeleton && !skeleton.IsDead)
            {
                Vector3 toEnemy = skeleton.GlobalPosition - GlobalPosition;
                toEnemy.Y = 0;
                float distance = toEnemy.Length();

                if (distance <= AttackRange)
                {
                    Vector3 enemyDir = toEnemy.Normalized();
                    float dot = forward.Dot(enemyDir);

                    // Frontal 140 degree attack cone
                    if (dot > 0.1f)
                    {
                        skeleton.TakeDamage(AttackDamage);
                    }
                }
            }
        }
    }

    public void MoveTo(Vector3 worldTarget)
    {
        if (_isDead || _isAttacking) return;

        _targetDestination = worldTarget;
        _hasTargetDestination = true;

        if (_navAgent != null)
        {
            _navAgent.TargetPosition = worldTarget;
        }

        if (_clickMarker != null)
        {
            _clickMarker.GlobalPosition = new Vector3(worldTarget.X, worldTarget.Y + 0.08f, worldTarget.Z);
            _clickMarker.Visible = true;
            _clickMarkerPulse = 1.0f;
            _clickMarker.Scale = Vector3.One;
        }
    }

    private void UpdateMoveDestination()
    {
        if (_isDead) return;

        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayDir = camera.ProjectRayNormal(mousePos);

        // Raycast against environment layer (excluding player collision)
        var space = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDir * 300f);
        query.CollisionMask = 1;
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        var result = space.IntersectRay(query);

        Vector3 targetPoint;
        if (result.Count > 0 && result.ContainsKey("position"))
        {
            targetPoint = (Vector3)result["position"];
        }
        else
        {
            var floorPlane = new Plane(Vector3.Up, 0.0f);
            var planeHit = floorPlane.IntersectsRay(rayOrigin, rayDir);
            if (!planeHit.HasValue) return;
            targetPoint = planeHit.Value;
        }

        MoveTo(targetPoint);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return;

        float dt = (float)delta;

        if (_attackCooldown > 0f)
        {
            _attackCooldown -= dt;
            if (_attackCooldown <= 0f)
            {
                _isAttacking = false;
            }
        }

        // Continuous movement tracking while holding mouse button
        if (_isHoldingMouse && !_isAttacking)
        {
            UpdateMoveDestination();
        }

        // Click marker decay animation
        if (_clickMarker != null && _clickMarker.Visible)
        {
            _clickMarkerPulse -= dt * 2.5f;
            if (_clickMarkerPulse <= 0f)
            {
                _clickMarker.Visible = false;
            }
            else
            {
                float scale = Mathf.Lerp(1.2f, 0.4f, 1.0f - _clickMarkerPulse);
                _clickMarker.Scale = new Vector3(scale, 1.0f, scale);
            }
        }

        if (_isAttacking)
        {
            Velocity = Velocity.MoveToward(Vector3.Zero, Acceleration * dt);
            MoveAndSlide();
            return;
        }

        // Movement towards target destination
        if (_hasTargetDestination)
        {
            Vector3 diff = _targetDestination - GlobalPosition;
            diff.Y = 0;

            if (diff.Length() > 0.35f)
            {
                Vector3 moveDir = Vector3.Zero;

                // 1. Navigation agent path
                if (_navAgent != null && !_navAgent.IsNavigationFinished())
                {
                    Vector3 nextPathPos = _navAgent.GetNextPathPosition();
                    Vector3 pathDiff = nextPathPos - GlobalPosition;
                    pathDiff.Y = 0;
                    if (pathDiff.Length() > 0.1f)
                    {
                        moveDir = pathDiff.Normalized();
                    }
                }

                // 2. Direct vector fallback
                if (moveDir == Vector3.Zero)
                {
                    moveDir = diff.Normalized();
                }

                Velocity = Velocity.MoveToward(moveDir * MoveSpeed, Acceleration * dt);

                // Correct character orientation (+Z model front aligned with moveDir)
                if (moveDir.LengthSquared() > 0.001f)
                {
                    float targetAngle = Mathf.Atan2(moveDir.X, moveDir.Z);
                    float curAngle = Rotation.Y;
                    float smoothAngle = Mathf.LerpAngle(curAngle, targetAngle, RotationSpeed * dt);
                    Rotation = new Vector3(0, smoothAngle, 0);
                }

                PlayAnimation("Running_A");
            }
            else
            {
                _hasTargetDestination = false;
                StopMoving(dt);
            }
        }
        else
        {
            StopMoving(dt);
        }

        MoveAndSlide();
    }

    private void StopMoving(float dt)
    {
        Velocity = Velocity.MoveToward(Vector3.Zero, Acceleration * dt);
        PlayAnimation("Idle");
    }

    public void PlayAnimation(string animName, bool force = false)
    {
        if (_animPlayer == null) return;
        if (!force && _currentAnim == animName) return;

        if (_animPlayer.HasAnimation(animName))
        {
            _currentAnim = animName;
            _animPlayer.Play(animName);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        PlayAnimation("Hit_A", true);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        _isAttacking = false;
        _hasTargetDestination = false;
        Velocity = Vector3.Zero;

        PlayAnimation("Death_A", true);

        // Disable collision so enemies don't push the fallen hero
        var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (col != null) col.SetDeferred("disabled", true);

        EmitSignal(SignalName.PlayerDied);
    }

    public void Respawn(Vector3 spawnPosition)
    {
        CurrentHealth = MaxHealth;
        _isDead = false;
        _isAttacking = false;
        _hasTargetDestination = false;
        GlobalPosition = spawnPosition;
        Velocity = Vector3.Zero;

        var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (col != null) col.SetDeferred("disabled", false);

        PlayAnimation("Idle", true);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        EmitSignal(SignalName.PlayerRespawned);
    }
}
