using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Enemies;
using Gravebound.Environment;
using Gravebound.UI;

namespace Gravebound.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 6.5f;
    [Export] public float Acceleration { get; set; } = 28.0f;
    [Export] public float RotationSpeed { get; set; } = 15.0f;
    [Export] public int AttackDamage { get; set; } = 25;
    [Export] public float AttackRange { get; set; } = 2.2f;
    [Export] public NodePath? NavigationAgentPath { get; set; }
    [Export] public NodePath? AnimationPlayerPath { get; set; }
    [Export] public NodePath? ClickMarkerPath { get; set; }

    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    public bool IsDead => _isDead;

    private NavigationAgent3D? _navAgent;
    private AnimationPlayer? _animPlayer;
    private Node3D? _clickMarker;
    private bool _isHoldingMoveButton;
    private bool _isAttacking;
    private bool _isDead;
    private float _attackCooldown;
    private float _clickMarkerPulse;
    private float _repathTimer;
    private float _invulnerabilityTimer;
    private float _hitStun;
    private float _deathTimer;
    private string _currentAnim = "";

    private Vector3 _targetDestination;
    private bool _hasTargetDestination;
    private SkeletonEnemy? _targetEnemy;
    private CryptAltar? _targetAltar;
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

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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

        if (_animPlayer != null)
        {
            string[] loopAnims = { "Idle", "Running_A", "Walking_A", "Cheer" };
            foreach (var anim in loopAnims)
            {
                if (_animPlayer.HasAnimation(anim))
                {
                    _animPlayer.GetAnimation(anim).LoopMode = Animation.LoopModeEnum.Linear;
                }
            }
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
            if (_deathTimer <= 0f)
            {
                if (@event is InputEventKey deadKey && deadKey.Pressed && (deadKey.Keycode == Key.Space || deadKey.Keycode == Key.Enter))
                {
                    Respawn(_initialSpawnPos);
                }
            }
            return;
        }

        // Ignore gameplay clicks if modal is open
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen)
        {
            _isHoldingMoveButton = false;
            return;
        }

        if (@event is InputEventMouseButton mb)
        {
            // Both Right Click (Titan Quest style) and Left Click handle movement / targeting
            if (mb.ButtonIndex == MouseButton.Right || mb.ButtonIndex == MouseButton.Left)
            {
                _isHoldingMoveButton = mb.Pressed;
                if (mb.Pressed)
                {
                    HandleMouseClick(isDragging: false);
                }
            }
        }
        else if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space)
            {
                PerformManualAttack();
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

    private void HandleMouseClick(bool isDragging)
    {
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayDir = camera.ProjectRayNormal(mousePos);
        var rayEnd = rayOrigin + rayDir * 300f;

        var space = GetWorld3D().DirectSpaceState;
        var playerExclude = new Godot.Collections.Array<Rid> { GetRid() };

        // 1. Raycast for Enemies (Mask 4: Bodies only)
        var enemyQuery = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        enemyQuery.CollisionMask = 4;
        enemyQuery.CollideWithBodies = true;
        enemyQuery.CollideWithAreas = false;
        enemyQuery.Exclude = playerExclude;
        var enemyResult = space.IntersectRay(enemyQuery);

        if (enemyResult.Count > 0 && enemyResult.ContainsKey("collider"))
        {
            var collider = enemyResult["collider"].As<GodotObject>();
            if (collider is SkeletonEnemy enemy && !enemy.IsDead)
            {
                _targetEnemy = enemy;
                _targetAltar = null;
                GraveboundHUD.Instance?.ShowTargetEnemy(enemy.EnemyName, enemy.CurrentHealth, enemy.MaxHealth);

                if (GlobalPosition.DistanceTo(enemy.GlobalPosition) <= AttackRange)
                {
                    PerformAttackOnTarget(enemy);
                }
                else
                {
                    _targetDestination = enemy.GlobalPosition;
                    _hasTargetDestination = true;
                    if (_navAgent != null) _navAgent.TargetPosition = enemy.GlobalPosition;
                }
                return;
            }
        }

        // 2. Raycast for Interactables: Altar, Portal (Mask 8: Areas only)
        var interactQuery = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        interactQuery.CollisionMask = 8;
        interactQuery.CollideWithBodies = false;
        interactQuery.CollideWithAreas = true;
        interactQuery.Exclude = playerExclude;
        var interactResult = space.IntersectRay(interactQuery);

        if (interactResult.Count > 0 && interactResult.ContainsKey("collider"))
        {
            var collider = interactResult["collider"].As<GodotObject>();

            if (collider is CryptAltar altar)
            {
                _targetAltar = altar;
                _targetEnemy = null;
                MoveTo(altar.GlobalPosition, showMarker: !isDragging);
                return;
            }

            if (collider is CatacombsPortal portal)
            {
                _targetAltar = null;
                _targetEnemy = null;
                var targetPos = new Vector3(portal.GlobalPosition.X, 0f, portal.GlobalPosition.Z);
                MoveTo(targetPos, showMarker: !isDragging);
                return;
            }
        }

        // 3. Raycast for World / Ground (Mask 1: Bodies only, NEVER areas)
        var worldQuery = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        worldQuery.CollisionMask = 1;
        worldQuery.CollideWithBodies = true;
        worldQuery.CollideWithAreas = false;
        worldQuery.Exclude = playerExclude;
        var worldResult = space.IntersectRay(worldQuery);

        if (worldResult.Count > 0 && worldResult.ContainsKey("position"))
        {
            _targetEnemy = null;
            _targetAltar = null;
            MoveTo((Vector3)worldResult["position"], showMarker: !isDragging);
            return;
        }

        // 4. Mathematical floor plane fallback
        var floorPlane = new Plane(Vector3.Up, 0.0f);
        var planeHit = floorPlane.IntersectsRay(rayOrigin, rayDir);
        if (planeHit.HasValue)
        {
            _targetEnemy = null;
            _targetAltar = null;
            MoveTo(planeHit.Value, showMarker: !isDragging);
        }
    }

    public void MoveTo(Vector3 worldTarget, bool showMarker = true)
    {
        if (_isDead) return;

        _targetDestination = worldTarget;
        _hasTargetDestination = true;

        if (_navAgent != null)
        {
            _navAgent.TargetPosition = worldTarget;
        }

        if (showMarker && _clickMarker != null)
        {
            _clickMarker.GlobalPosition = new Vector3(worldTarget.X, worldTarget.Y + 0.08f, worldTarget.Z);
            _clickMarker.Visible = true;
            _clickMarkerPulse = 1.0f;
            _clickMarker.Scale = Vector3.One;
        }
    }

    public void PerformManualAttack()
    {
        if (_isDead || _isAttacking || _attackCooldown > 0f) return;

        _isAttacking = true;
        _attackCooldown = 0.55f;
        Velocity = Vector3.Zero;
        _hasTargetDestination = false;

        FaceMouseCursor();
        SoundManager.Instance?.PlaySwordSwing();
        PlayAnimation("1H_Melee_Attack_Chop", true);

        GetTree().CreateTimer(0.25f).Timeout += () =>
        {
            if (!_isDead)
            {
                ExecuteMeleeStrike(null);
            }
        };
    }

    private void PerformAttackOnTarget(SkeletonEnemy target)
    {
        if (_isDead || _isAttacking || _attackCooldown > 0f) return;

        _isAttacking = true;
        _attackCooldown = 0.55f;
        Velocity = Vector3.Zero;
        _hasTargetDestination = false;

        // Face target enemy (+Pi offsets model forward towards -Z)
        Vector3 toEnemy = target.GlobalPosition - GlobalPosition;
        toEnemy.Y = 0;
        if (toEnemy.LengthSquared() > 0.01f)
        {
            float targetAngle = Mathf.Atan2(toEnemy.X, toEnemy.Z);
            Rotation = new Vector3(0, targetAngle, 0);
        }

        SoundManager.Instance?.PlaySwordSwing();
        PlayAnimation("1H_Melee_Attack_Chop", true);

        GetTree().CreateTimer(0.25f).Timeout += () =>
        {
            if (!_isDead && target != null && !target.IsDead)
            {
                ExecuteMeleeStrike(target);
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

    private void ExecuteMeleeStrike(SkeletonEnemy? primaryTarget)
    {
        bool hitAny = false;

        if (primaryTarget != null && !primaryTarget.IsDead && GlobalPosition.DistanceTo(primaryTarget.GlobalPosition) <= AttackRange + 0.6f)
        {
            primaryTarget.TakeDamage(AttackDamage);
            hitAny = true;
        }

        // Cleave arc: model faces local +Z (glTF convention)
        Vector3 forward = Transform.Basis.Z.Normalized();
        var enemies = GetTree().GetNodesInGroup("enemies");

        foreach (Node node in enemies)
        {
            if (node is SkeletonEnemy skeleton && skeleton != primaryTarget && !skeleton.IsDead)
            {
                Vector3 toEnemy = skeleton.GlobalPosition - GlobalPosition;
                toEnemy.Y = 0;
                float distance = toEnemy.Length();

                if (distance <= AttackRange)
                {
                    Vector3 enemyDir = toEnemy.Normalized();
                    if (forward.Dot(enemyDir) > 0.1f)
                    {
                        skeleton.TakeDamage(AttackDamage);
                        hitAny = true;
                    }
                }
            }
        }

        if (hitAny)
        {
            SoundManager.Instance?.PlaySwordHit(GlobalPosition + forward * 1.5f);
        }
    }

    public override void _Process(double delta)
    {
        if (_isDead) return;
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        // Hover detection for enemy info
        var camera = GetViewport().GetCamera3D();
        if (camera != null)
        {
            var mousePos = GetViewport().GetMousePosition();
            var rayOrigin = camera.ProjectRayOrigin(mousePos);
            var rayDir = camera.ProjectRayNormal(mousePos);
            var space = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDir * 300f);
            query.CollisionMask = 4;
            query.CollideWithBodies = true;
            var res = space.IntersectRay(query);

            if (res.Count > 0 && res.ContainsKey("collider"))
            {
                if (res["collider"].As<GodotObject>() is SkeletonEnemy hovered && !hovered.IsDead)
                {
                    GraveboundHUD.Instance?.ShowTargetEnemy(hovered.EnemyName, hovered.CurrentHealth, hovered.MaxHealth);
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_invulnerabilityTimer > 0f) _invulnerabilityTimer -= dt;
        if (_hitStun > 0f) _hitStun -= dt;

        if (_isDead)
        {
            if (_deathTimer > 0f) _deathTimer -= dt;
            return;
        }

        if (_attackCooldown > 0f)
        {
            _attackCooldown -= dt;
            if (_attackCooldown <= 0f)
            {
                _isAttacking = false;
            }
        }

        // Throttled hold tracking (avoids resetting pathing every 16ms!)
        if (_isHoldingMoveButton && !_isAttacking && _targetEnemy == null)
        {
            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = 0.12f;
                HandleMouseClick(isDragging: true);
            }
        }

        // Decay click marker
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

        // 1. Target Enemy Pursuit & Attack (Titan Quest style)
        if (_targetEnemy != null)
        {
            if (_targetEnemy.IsDead)
            {
                _targetEnemy = null;
                _hasTargetDestination = false;
                StopMoving(dt);
                MoveAndSlide();
                return;
            }

            float distToEnemy = GlobalPosition.DistanceTo(_targetEnemy.GlobalPosition);

            if (distToEnemy <= AttackRange)
            {
                // In range: stop and attack
                Velocity = Velocity.MoveToward(Vector3.Zero, Acceleration * dt);
                if (_attackCooldown <= 0f)
                {
                    PerformAttackOnTarget(_targetEnemy);
                }
                else
                {
                    PlayAnimation("Idle");
                }
                MoveAndSlide();
                return;
            }
            else
            {
                // Smooth pursuit towards moving enemy
                _targetDestination = _targetEnemy.GlobalPosition;
                _hasTargetDestination = true;
                _repathTimer -= dt;
                if (_repathTimer <= 0f)
                {
                    _repathTimer = 0.15f;
                    if (_navAgent != null) _navAgent.TargetPosition = _targetEnemy.GlobalPosition;
                }
            }
        }

        // 2. Altar Proximity Check
        if (_targetAltar != null)
        {
            if (GlobalPosition.DistanceTo(_targetAltar.GlobalPosition) <= 2.8f)
            {
                _targetAltar.Inspect();
                _targetAltar = null;
                _hasTargetDestination = false;
                StopMoving(dt);
                MoveAndSlide();
                return;
            }
        }

        // 3. Movement towards destination
        if (_hasTargetDestination)
        {
            Vector3 diff = _targetDestination - GlobalPosition;
            diff.Y = 0;

            if (diff.Length() > 0.35f)
            {
                Vector3 moveDir = Vector3.Zero;

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

                if (moveDir == Vector3.Zero)
                {
                    moveDir = diff.Normalized();
                }

                Velocity = Velocity.MoveToward(moveDir * MoveSpeed, Acceleration * dt);

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
        if (_hitStun > 0f && animName == "Idle" && !force) return;
        if (!force && _currentAnim == animName) return;

        if (_animPlayer.HasAnimation(animName))
        {
            _currentAnim = animName;
            _animPlayer.Play(animName);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || _invulnerabilityTimer > 0f) return;

        _invulnerabilityTimer = 0.35f;
        _hitStun = 0.25f;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        SoundManager.Instance?.PlayPlayerHit();
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
        _deathTimer = 1.5f;
        _isAttacking = false;
        _hasTargetDestination = false;
        _targetEnemy = null;
        _targetAltar = null;
        Velocity = Vector3.Zero;

        PlayAnimation("Death_A", true);

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
        _targetEnemy = null;
        _targetAltar = null;
        GlobalPosition = spawnPosition;
        Velocity = Vector3.Zero;
        _invulnerabilityTimer = 1.0f;

        var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (col != null) col.SetDeferred("disabled", false);

        PlayAnimation("Idle", true);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        EmitSignal(SignalName.PlayerRespawned);

        // Reset living enemies to their spawn positions and full health
        var enemies = GetTree().GetNodesInGroup("enemies");
        foreach (Node node in enemies)
        {
            if (node is SkeletonEnemy enemy && !enemy.IsDead)
            {
                enemy.ResetToSpawn();
            }
        }
    }
}
