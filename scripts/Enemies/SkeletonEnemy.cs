using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Core;
using Gravebound.Player;
using Gravebound.UI;
using Gravebound.Inventory;

namespace Gravebound.Enemies;

public partial class SkeletonEnemy : CharacterBody3D
{
    [Export] public string EnemyName { get; set; } = "Скелет-вартовий Мор-Гула";
    [Export] public float MoveSpeed { get; set; } = 3.2f;
    [Export] public float AggroRange { get; set; } = 8.5f;
    [Export] public float AttackRange { get; set; } = 1.8f;
    [Export] public int AttackDamage { get; set; } = 8;
    [Export] public int MaxHealth { get; set; } = 50;

    public int CurrentHealth { get; private set; }
    public bool IsDead => _isDead;

    private NavigationAgent3D? _navAgent;
    private AnimationPlayer? _animPlayer;
    private PlayerController? _playerTarget;
    private Node3D? _healthBarNode;
    private Node3D? _barFillAnchor;

    private bool _isDead;
    private bool _isAttacking;
    private float _attackCooldown;
    private float _staggerTime;
    private int _attackId;
    private float _fallSpeed;
    private string _currentAnim = "";
    private Vector3 _spawnPosition;

    [Signal] public delegate void EnemyDiedEventHandler(SkeletonEnemy enemy);

    public override void _Ready()
    {
        AddToGroup("enemies");
        CharacterMotion.Configure(this);
        _spawnPosition = GlobalPosition;
        CurrentHealth = MaxHealth;

        _navAgent = GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
        _animPlayer = FindAnimationPlayer(this);
        _healthBarNode = GetNodeOrNull<Node3D>("HealthBar");
        _barFillAnchor = GetNodeOrNull<Node3D>("HealthBar/BarFillAnchor");
        if (_healthBarNode != null)
        {
            // Detach from body rotation so the bar doesn't swing around with the skeleton
            _healthBarNode.TopLevel = true;
            _healthBarNode.GlobalPosition = GlobalPosition + Vector3.Up * 2.2f;
        }

        if (_animPlayer != null)
        {
            string[] loopAnims = { "Idle", "Walking_A", "Cheer" };
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
        UpdateHealthBarVisual();
    }

    private async void ActorSetup()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        _playerTarget = PlayerController.Instance ?? GetTree().Root.FindChild("Player", true, false) as PlayerController;
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

    public override void _Process(double delta)
    {
        if (_healthBarNode != null && !_isDead)
        {
            _healthBarNode.GlobalPosition = GlobalPosition + Vector3.Up * 2.2f;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return;

        float dt = (float)delta;

        if (_staggerTime > 0f)
        {
            _staggerTime -= dt;
            Velocity = Velocity.MoveToward(Vector3.Zero, 14.0f * dt);
            CharacterMotion.Move(this, ref _fallSpeed, dt);
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

        if (_playerTarget == null)
        {
            _playerTarget = PlayerController.Instance;
            if (_playerTarget == null) return;
        }

        // Cease combat if player is dead
        if (_playerTarget.IsDead)
        {
            _isAttacking = false;
            _attackId++;
            Velocity = Velocity.MoveToward(Vector3.Zero, 10.0f * dt);
            CharacterMotion.Move(this, ref _fallSpeed, dt);
            PlayAnimation("Idle");
            return;
        }

        float distToPlayer = GlobalPosition.DistanceTo(_playerTarget.GlobalPosition);

        if (distToPlayer <= AttackRange)
        {
            // Within striking distance
            Velocity = Velocity.MoveToward(Vector3.Zero, 12.0f * dt);
            LookAtTarget(_playerTarget.GlobalPosition, dt);

            if (!_isAttacking && _attackCooldown <= 0f)
            {
                PerformAttack();
            }
        }
        else if (distToPlayer <= AggroRange)
        {
            // Pursue player
            if (!_isAttacking && _navAgent != null)
            {
                _navAgent.TargetPosition = _playerTarget.GlobalPosition;
                Vector3 moveDir = Vector3.Zero;

                if (!_navAgent.IsNavigationFinished())
                {
                    Vector3 nextPos = _navAgent.GetNextPathPosition();
                    Vector3 diff = nextPos - GlobalPosition;
                    diff.Y = 0;
                    if (diff.Length() > 0.1f)
                    {
                        moveDir = diff.Normalized();
                    }
                }

                // Fallback direct pursuit
                if (moveDir == Vector3.Zero)
                {
                    Vector3 directDiff = _playerTarget.GlobalPosition - GlobalPosition;
                    directDiff.Y = 0;
                    if (directDiff.Length() > AttackRange)
                    {
                        moveDir = directDiff.Normalized();
                    }
                }

                // Soft separation from other skeletons so they don't clump
                Vector3 separation = Vector3.Zero;
                var enemies = GetTree().GetNodesInGroup("enemies");
                foreach (Node node in enemies)
                {
                    if (node is SkeletonEnemy other && other != this && !other.IsDead)
                    {
                        Vector3 diff = GlobalPosition - other.GlobalPosition;
                        diff.Y = 0;
                        float d = diff.Length();
                        if (d < 1.1f && d > 0.01f)
                        {
                            separation += (diff / d) * (1.1f - d) * 2.0f;
                        }
                    }
                }

                if (moveDir != Vector3.Zero)
                {
                    moveDir = (moveDir + separation).Normalized();
                    Velocity = moveDir * MoveSpeed;
                    LookAtTarget(GlobalPosition + moveDir, dt);
                    PlayAnimation("Walking_A");
                }
                else
                {
                    Velocity = Vector3.Zero;
                    PlayAnimation("Idle");
                }
            }
        }
        else if (distToPlayer > AggroRange * 1.5f && GlobalPosition.DistanceTo(_spawnPosition) > 0.5f)
        {
            // Leash: return to spawn position
            Vector3 toSpawn = _spawnPosition - GlobalPosition;
            toSpawn.Y = 0;
            if (toSpawn.Length() > 0.3f)
            {
                Vector3 returnDir = toSpawn.Normalized();
                Velocity = returnDir * MoveSpeed;
                LookAtTarget(GlobalPosition + returnDir, dt);
                PlayAnimation("Walking_A");
            }
            else
            {
                Velocity = Vector3.Zero;
                PlayAnimation("Idle");
                CurrentHealth = MaxHealth;
                UpdateHealthBarVisual();
            }
        }
        else
        {
            Velocity = Vector3.Zero;
            PlayAnimation("Idle");
        }

        CharacterMotion.Move(this, ref _fallSpeed, dt);
    }

    private void LookAtTarget(Vector3 targetPos, float dt)
    {
        Vector3 dir = targetPos - GlobalPosition;
        dir.Y = 0;
        if (dir.LengthSquared() > 0.001f)
        {
            float targetAngle = Mathf.Atan2(dir.X, dir.Z);
            float curAngle = Rotation.Y;
            float smoothAngle = Mathf.LerpAngle(curAngle, targetAngle, 10.0f * dt);
            Rotation = new Vector3(0, smoothAngle, 0);
        }
    }

    private void PerformAttack()
    {
        _isAttacking = true;
        _attackCooldown = 1.6f;
        int currentAttackId = ++_attackId;
        PlayAnimation("1H_Melee_Attack_Chop", true);

        GetTree().CreateTimer(0.45f).Timeout += () =>
        {
            if (_attackId != currentAttackId) return;
            if (_isDead || _staggerTime > 0f || !_isAttacking) return;
            if (_playerTarget != null && !_playerTarget.IsDead)
            {
                // Facing check: skeleton must be facing player within cone
                Vector3 toPlayer = (_playerTarget.GlobalPosition - GlobalPosition).Normalized();
                Vector3 forward = Transform.Basis.Z.Normalized();
                if (forward.Dot(toPlayer) > 0.25f)
                {
                    float dist = GlobalPosition.DistanceTo(_playerTarget.GlobalPosition);
                    if (dist <= AttackRange + 0.4f)
                    {
                        _playerTarget.TakeDamage(AttackDamage);
                    }
                }
            }
        };
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        _staggerTime = 0.35f;
        _isAttacking = false;
        _attackId++; // Cancel current attack!
        _attackCooldown = Mathf.Max(_attackCooldown, 0.45f);

        // Spawn floating damage numbers & play hit sound
        DamageNumber.Spawn(this, GlobalPosition + Vector3.Up * 1.8f, damage);
        SoundManager.Instance?.PlaySkeletonHit(GlobalPosition);

        UpdateHealthBarVisual();
        GraveboundHUD.Instance?.ShowTargetEnemy(EnemyName, CurrentHealth, MaxHealth);

        PlayAnimation("Hit_A", true);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBarVisual()
    {
        if (_barFillAnchor != null)
        {
            float ratio = Mathf.Clamp((float)CurrentHealth / MaxHealth, 0.0f, 1.0f);
            _barFillAnchor.Scale = new Vector3(ratio, 1.0f, 1.0f);
        }
    }

    private void Die()
    {
        _isDead = true;
        _isAttacking = false;
        Velocity = Vector3.Zero;
        RemoveFromGroup("enemies");

        if (_healthBarNode != null)
        {
            _healthBarNode.Visible = false;
        }

        SoundManager.Instance?.PlaySkeletonDeath(GlobalPosition);
        GraveboundHUD.Instance?.ClearTargetEnemy();
        PlayAnimation("Death_A", true);

        var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (col != null) col.SetDeferred("disabled", true);

        // Spawn loot drop
        bool isBoss = MaxHealth > 100;
        int roll = GD.RandRange(0, 100);
        if (isBoss || roll < 80)
        {
            var itemData = ItemData.CreateRandomDrop(isBoss);
            var worldItem = new WorldItem3D();
            worldItem.Position = GlobalPosition;
            worldItem.Setup(itemData);
            GetTree().CurrentScene?.CallDeferred("add_child", worldItem);
        }

        // Award Experience to Player
        int xp = isBoss ? 250 : (EnemyName.Contains("асасин") ? 45 : 35);
        PlayerProgression.Instance?.AddXP(xp);

        EmitSignal(SignalName.EnemyDied, this);
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

    public void ResetToSpawn()
    {
        if (_isDead) return;
        GlobalPosition = _spawnPosition;
        CurrentHealth = MaxHealth;
        _isAttacking = false;
        _attackCooldown = 0f;
        _staggerTime = 0f;
        _attackId++;
        Velocity = Vector3.Zero;
        UpdateHealthBarVisual();
        PlayAnimation("Idle", true);
    }
}
