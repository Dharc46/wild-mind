using UnityEngine;

/// <summary>
/// Enemy state responsible for regenerating health when out of combat.
/// Waits for the configured delay after the last damage event before
/// ticking heals each frame. Any new damage instantly breaks healing.
/// </summary>
public class EnemyHealState : EnemyBaseState
{
    public const float DefaultHealDelay = 3f;
    public const float DefaultHealPerSecond = 5f;

    private readonly EnemyHealth _enemyHealth;

    private float _delayTimer;
    private bool _canHeal;
    private float _healDelay;
    private float _healPerSecond;
    private float _healThreshold;

    public EnemyHealState(EnemyStateManager entity) : base(entity)
    {
        _enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDamaged += HandleDamaged;
            _enemyHealth.OnDeath += HandleDeath;
        }
    }

    public override void EnterState()
    {
        if (enemy.Animator != null)
        {
            enemy.Animator.SetBool("IsWalking", false);
        }

        enemy.SetControllerVelocity(Vector2.zero);
        enemy.StopController();

        ConfigureHealingParameters();

        _delayTimer = 0f;
        _canHeal = _healDelay <= 0f;

        if (_enemyHealth == null || !_enemyHealth.IsAlive || HasReachedHealThreshold() || enemy.IsInCombat)
        {
            enemy.TransitionToState(enemy.WalkState);
        }
    }

    public override void Update()
    {
        if (enemy.IsInCombat)
        {
            enemy.TransitionToState(enemy.WalkState);
            return;
        }

        if (_enemyHealth == null || !_enemyHealth.IsAlive)
        {
            enemy.TransitionToState(enemy.IdleState);
            return;
        }

        if (HasReachedHealThreshold())
        {
            enemy.TransitionToState(enemy.WalkState);
            return;
        }

        if (!_canHeal)
        {
            _delayTimer += Time.deltaTime;
            if (_delayTimer < _healDelay)
            {
                return;
            }

            _canHeal = true;
        }

        float healed = _enemyHealth.Heal(_healPerSecond * Time.deltaTime);

        // If healing had no effect (already capped), bail out to default behavior.
        if (healed <= Mathf.Epsilon && HasReachedHealThreshold())
        {
            enemy.TransitionToState(enemy.WalkState);
        }
    }

    private void ConfigureHealingParameters()
    {
        _healDelay = enemy.enemyData != null
            ? Mathf.Max(0f, enemy.enemyData.outOfCombatHealDelay)
            : DefaultHealDelay;

        _healPerSecond = enemy.enemyData != null
            ? Mathf.Max(0f, enemy.enemyData.healPerSecond)
            : DefaultHealPerSecond;

        float healFraction = enemy.enemyData != null
            ? Mathf.Clamp01(enemy.enemyData.healStopFraction)
            : 1f;

        float maxHealth = _enemyHealth != null ? _enemyHealth.maxHealth : 0f;
        _healThreshold = healFraction >= 1f ? maxHealth : maxHealth * healFraction;
    }

    private bool HasReachedHealThreshold()
    {
        if (_enemyHealth == null) return true;
        float threshold = _healThreshold > 0f ? _healThreshold : _enemyHealth.maxHealth;
        return _enemyHealth.CurrentHealth >= (threshold - 0.01f);
    }

    private void HandleDamaged(EnemyHealth health, float _)
    {
        _delayTimer = 0f;
        _canHeal = false;

        if (enemy.CurrentState == this)
        {
            enemy.TransitionToState(enemy.WalkState);
        }
    }

    private void HandleDeath(EnemyHealth _)
    {
        if (enemy.CurrentState == this)
        {
            enemy.TransitionToState(enemy.IdleState);
        }
    }
}


