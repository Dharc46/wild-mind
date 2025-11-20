using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyStateManager : EntityStateManager
{
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

    [Header("Cover & Flee")]
    [Tooltip("Layers considered solid for cover checks when fleeing.")]
    public LayerMask coverObstructionMask = ~0;

    // Cached health component (optional)
    private EnemyHealth _enemyHealth;

    private EnemyIdleState _idleState;
    private EnemyWalkState _walkState;
    private EnemyKnockedState _knockedState;
    private EnemyRangedState _rangedState;
    private EnemyHealState _healState;
    private EnemyFleeState _fleeState;
    private Vector2 _hitDir;

    [Header("Combat Logic")]
    [Tooltip("Seconds to remain flagged in-combat after last detection/damage event.")]
    public float combatExitDelay = 5f;

    private Coroutine _combatTimerRoutine;
    private bool _isInCombat;

    public EnemyWalkState WalkState { get => _walkState; }
    public EnemyIdleState IdleState { get => _idleState; }
    public EnemyKnockedState KnockState { get => _knockedState; }
    public EnemyRangedState RangedState { get => _rangedState; }
    public EnemyHealState HealState { get => _healState; }
    public EnemyFleeState FleeState { get => _fleeState; }
    public bool IsInCombat => _isInCombat;

    public Vector2 HitDirection { get => _hitDir; }
    public float ThrustForce = 13.0f;

    private void Awake()
    {
        _idleState = new EnemyIdleState(this);
        _walkState = new EnemyWalkState(this);
        _knockedState = new EnemyKnockedState(this);
        _rangedState = new EnemyRangedState(this);
        _healState = new EnemyHealState(this);
        _fleeState = new EnemyFleeState(this);

        _direction = Vector2.down;

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
        EvaluateStateTransitions();
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

    public void NotifyAttackPerformed()
    {
        BeginCombatWindow();
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

    private IEnumerator CombatTimeoutRoutine()
    {
        float timeout = Mathf.Max(0.1f, combatExitDelay);
        yield return new WaitForSeconds(timeout);
        _isInCombat = false;
        _combatTimerRoutine = null;
    }
}
