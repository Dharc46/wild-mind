using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyStateManager : EntityStateManager
{
    private static readonly HashSet<EnemyStateManager> ActiveEnemies = new HashSet<EnemyStateManager>();

    [Header("Data")]
    public EnemyData enemyData;

    [Header("Ranged Combat")]
    [Tooltip("Optional projectile prefab this enemy will fire when using ranged states.")]
    public GameObject projectilePrefab;
    [Tooltip("Optional override for the projectile spawn position. Defaults to transform position if null.")]
    public Transform projectileSpawnPoint;
    [Tooltip("Velocity magnitude applied to fired projectiles.")]
    public float projectileSpeed = 10f;
    [Tooltip("How long spawned projectiles live before being destroyed.")]
    public float projectileLifetime = 6f;
    [Tooltip("Cooldown between projectile shots when attackSpeed is unavailable.")]
    public float rangedFireCooldown = 1.25f;
    [Tooltip("Random spread applied to projectile direction in degrees.")]
    public float projectileSpreadDegrees = 0f;

    [Header("Detection")]
    [Tooltip("Seconds between automatic player proximity checks.")]
    public float detectionCheckInterval = 0.25f;
    [Tooltip("How far (as a multiplier of detection radius) the player can move away before combat naturally times out.")]
    public float detectionLoseMultiplier = 1.5f;

    [Header("Cover & Flee")]
    [Tooltip("Layers considered solid for cover checks when fleeing.")]
    public LayerMask coverObstructionMask = ~0;

    // Cached health component (optional)
    private EnemyHealth _enemyHealth;
    private EnemyAgent _agent;

    private EnemyIdleState _idleState;
    private EnemyWalkState _walkState;
    private EnemyKnockedState _knockedState;
    private EnemyRangedState _rangedState;
    private EnemyHealState _healState;
    private EnemyFleeState _fleeState;
    private Vector2 _hitDir;
    private PlayerStateManager _player;
    private float _detectionTimer;

    [Header("Combat Logic")]
    [Tooltip("Seconds to remain flagged in-combat after last detection/damage event.")]
    public float combatExitDelay = 5f;

    private Coroutine _combatTimerRoutine;
    private bool _isInCombat;
    private float _attackCooldownDuration;
    private float _attackCooldownRemaining;
    private float _lastAttackRangeUsed;

    public EnemyWalkState WalkState { get => _walkState; }
    public EnemyIdleState IdleState { get => _idleState; }
    public EnemyKnockedState KnockState { get => _knockedState; }
    public EnemyRangedState RangedState { get => _rangedState; }
    public EnemyHealState HealState { get => _healState; }
    public EnemyFleeState FleeState { get => _fleeState; }
    public bool IsInCombat => _isInCombat;
    public PlayerStateManager PlayerTarget
    {
        get
        {
            if (_player == null)
            {
                _player = FindObjectOfType<PlayerStateManager>();
            }

            return _player;
        }
    }

    public Vector2 HitDirection { get => _hitDir; }
    public float ThrustForce = 13.0f;
    public float AttackCooldownNormalized => _attackCooldownDuration > 0f
        ? Mathf.Clamp01(_attackCooldownRemaining / _attackCooldownDuration)
        : 0f;
    public float CurrentHealthNormalized
    {
        get
        {
            if (_enemyHealth == null || _enemyHealth.maxHealth <= 0f)
                return 0f;
            return Mathf.Clamp01(_enemyHealth.CurrentHealth / _enemyHealth.maxHealth);
        }
    }
    public float PreferredAttackRange
    {
        get
        {
            if (enemyData != null)
            {
                if (enemyData.preferredAttackRange > 0f)
                    return enemyData.preferredAttackRange;
                if (enemyData.attackRange > 0f)
                    return enemyData.attackRange;
            }
            return 2f;
        }
    }
    public float MaxDetectionRange
    {
        get
        {
            if (enemyData != null && enemyData.detectionRadius > 0f)
                return enemyData.detectionRadius;
            return 10f;
        }
    }

    private void Awake()
    {
        _idleState = new EnemyIdleState(this);
        _walkState = new EnemyWalkState(this);
        _knockedState = new EnemyKnockedState(this);
        _rangedState = new EnemyRangedState(this);
        _healState = new EnemyHealState(this);
        _fleeState = new EnemyFleeState(this);

        _direction = Vector2.down;
        _agent = GetComponent<EnemyAgent>();

    }
    protected override void Start()
    {
        base.Start();
        // Initialize health from data if available
        _enemyHealth = GetComponent<EnemyHealth>();
        if (_enemyHealth != null && enemyData != null)
        {
            _enemyHealth.Initialize(enemyData.maxHealth);
            _enemyHealth.OnDamaged += HandleDamaged;
            _enemyHealth.OnDeath += HandleDeath;
        }

        TransitionToState(_walkState);
    }

    private void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    // Called from Player when hit an NPC with the sword
    // Stores the player direction to apply thrust force (inversed) to NPC
    public void Hit(Vector2 direction)
    {
        _hitDir = direction;
        BeginCombatWindow();
        TransitionToState(KnockState);
    }

    protected override void Update()
    {
        base.Update();
        UpdatePlayerAwareness();
        EvaluateStateTransitions();
        TickAttackCooldown(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDamaged -= HandleDamaged;
            _enemyHealth.OnDeath -= HandleDeath;
        }
    }

    private void EvaluateStateTransitions()
    {
        if (_currentState == null || _currentState == _knockedState)
            return;

        if (!IsInCombat && ShouldHeal())
        {
            TryTransition(_healState);
            return;
        }

        if (IsInCombat)
        {
            if (ShouldFlee())
            {
                TryTransition(_fleeState);
                return;
            }

            if (ShouldUseRanged())
            {
                TryTransition(_rangedState);
                return;
            }
        }

        if (_currentState == _healState || _currentState == _rangedState || _currentState == _fleeState)
        {
            TryTransition(_walkState);
        }
    }

    private void TryTransition(IState next)
    {
        if (next == null || _currentState == next)
            return;
        TransitionToState(next);
    }

    private bool ShouldHeal()
    {
        if (_enemyHealth == null || !_enemyHealth.IsAlive)
            return false;

        return _enemyHealth.CurrentHealth < (_enemyHealth.maxHealth - 0.01f);
    }

    private bool ShouldFlee()
    {
        if (_enemyHealth == null || enemyData == null)
            return false;

        if (enemyData.fleeHealthThreshold <= 0f)
            return false;

        float hpPercent = _enemyHealth.CurrentHealth / Mathf.Max(1f, _enemyHealth.maxHealth);
        return hpPercent <= Mathf.Clamp01(enemyData.fleeHealthThreshold);
    }

    private bool ShouldUseRanged()
    {
        if (enemyData == null || !enemyData.isRanged)
            return false;

        if (projectilePrefab == null)
            return false;

        return true;
    }

    public void NotifyAttackPerformed(float cooldownDuration = 0f)
    {
        BeginCombatWindow();
        if (cooldownDuration > 0f)
        {
            BeginAttackCooldown(cooldownDuration);
        }
    }

    private void HandleDamaged(EnemyHealth health, float _)
    {
        BeginCombatWindow();
    }

    private void HandleDeath(EnemyHealth health)
    {
        _isInCombat = false;
        if (_combatTimerRoutine != null)
        {
            StopCoroutine(_combatTimerRoutine);
            _combatTimerRoutine = null;
        }
    }

    private void BeginCombatWindow()
    {
        _isInCombat = true;
        if (_combatTimerRoutine != null)
        {
            StopCoroutine(_combatTimerRoutine);
        }
        _combatTimerRoutine = StartCoroutine(CombatTimeoutRoutine());
    }

    private void UpdatePlayerAwareness()
    {
        if (enemyData == null)
            return;

        _detectionTimer -= Time.deltaTime;
        if (_detectionTimer > 0f)
            return;

        _detectionTimer = Mathf.Max(0.05f, detectionCheckInterval);

        var player = PlayerTarget;
        if (player == null)
            return;

        Vector2 delta = player.transform.position - transform.position;
        float distance = delta.magnitude;
        float detectionRadius = Mathf.Max(0.5f, enemyData.detectionRadius);

        if (distance <= detectionRadius)
        {
            BeginCombatWindow();
        }
    }

    private IEnumerator CombatTimeoutRoutine()
    {
        float timeout = Mathf.Max(0.1f, combatExitDelay);
        yield return new WaitForSeconds(timeout);
        _isInCombat = false;
        _combatTimerRoutine = null;
    }

    private void TickAttackCooldown(float deltaTime)
    {
        if (_attackCooldownRemaining <= 0f)
        {
            _attackCooldownRemaining = 0f;
            return;
        }

        _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - Mathf.Max(0f, deltaTime));
    }

    private void BeginAttackCooldown(float duration)
    {
        _attackCooldownDuration = Mathf.Max(0.001f, duration);
        _attackCooldownRemaining = _attackCooldownDuration;
    }

    public bool TryPerformAttack()
    {
        if (PlayerTarget == null)
            return false;

        if (_attackCooldownRemaining > 0f)
            return false;

        Vector2 enemyPos = transform.position;
        Vector2 playerPos = PlayerTarget.transform.position;
        Vector2 toPlayer = playerPos - enemyPos;
        float distance = toPlayer.magnitude;
        float damage = enemyData != null ? enemyData.damage : 5f;
        float attackRange = enemyData != null && enemyData.attackRange > 0f ? enemyData.attackRange : 1.5f;
        float cooldown = Mathf.Max(0.1f, enemyData != null && enemyData.attackSpeed > 0f
            ? 1f / enemyData.attackSpeed
            : rangedFireCooldown);

        if (enemyData != null && enemyData.isRanged && projectilePrefab != null)
        {
            if (!TrySpawnProjectile(playerPos))
                return false;

            _lastAttackRangeUsed = distance;
            NotifyAttackPerformed(cooldown);
            return true;
        }

        if (distance > attackRange)
            return false;

        PlayerHealth playerHealth = PlayerTarget.PlayerHealth;
        float appliedDamage = 0f;
        bool killed = false;

        if (playerHealth != null)
        {
            float previousHealth = playerHealth.CurrentHealth;
            playerHealth.TakeDamage(damage);
            appliedDamage = Mathf.Max(0f, previousHealth - playerHealth.CurrentHealth);
            killed = !playerHealth.IsAlive;
        }
        else
        {
            PlayerTarget.TakeDamage(damage);
            appliedDamage = damage;
        }

        _lastAttackRangeUsed = distance;
        NotifyAttackPerformed(cooldown);

        if (appliedDamage > 0f)
        {
            NotifyPlayerDamaged(appliedDamage, killed);
        }

        return true;
    }

    public bool TryHealSelf(float amount)
    {
        if (_enemyHealth == null || amount <= 0f)
            return false;

        float healed = _enemyHealth.Heal(amount);
        return healed > 0f;
    }

    public float GetLastAttackRange() => _lastAttackRangeUsed;

    public void NotifyPlayerDamaged(float amount, bool killed)
    {
        if (_agent == null)
        {
            _agent = GetComponent<EnemyAgent>();
        }

        _agent?.OnPlayerDamaged(amount, killed);
    }

    public static int CountAlliesNearby(EnemyStateManager origin, float radius)
    {
        if (origin == null)
            return 0;

        int count = 0;
        float radiusSqr = radius * radius;
        foreach (var enemy in ActiveEnemies)
        {
            if (enemy == null || enemy == origin)
                continue;

            if ((enemy.transform.position - origin.transform.position).sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }

        return count;
    }

    private bool TrySpawnProjectile(Vector2 targetPosition)
    {
        if (projectilePrefab == null || PlayerTarget == null)
            return false;

        Vector2 enemyPos = transform.position;
        Vector2 direction = (targetPosition - enemyPos).sqrMagnitude > Mathf.Epsilon
            ? (targetPosition - enemyPos).normalized
            : Direction.sqrMagnitude > Mathf.Epsilon ? Direction : Vector2.right;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject projectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (projectile == null)
            return false;

        if (projectile.TryGetComponent<Rigidbody2D>(out var rb))
        {
            float speed = Mathf.Max(0.1f, projectileSpeed);
            rb.velocity = direction * speed;
        }

        var projectileBehaviour = projectile.GetComponent<EnemyProjectile>();
        if (projectileBehaviour != null)
        {
            float dmg = enemyData != null ? enemyData.damage : 0f;
            projectileBehaviour.Initialize(this, dmg);
        }

        if (projectileLifetime > 0f)
        {
            Object.Destroy(projectile, projectileLifetime);
        }

        return true;
    }
}
