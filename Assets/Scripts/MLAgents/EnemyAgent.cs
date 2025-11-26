using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// ML-Agents wrapper that exposes the simplified observation/action/reward
/// contract used for training combat behaviors.
/// </summary>
[RequireComponent(typeof(EnemyStateManager))]
public class EnemyAgent : Agent
{
    [Header("Observation Settings")]
    [SerializeField] private float maxRangeNormalization = 10f;
    [SerializeField] private float allyDetectionRadius = 6f;
    [SerializeField] private int maxAlliesForNormalization = 5;

    [Header("Reward Settings")]
    [SerializeField] private float damageRewardScale = 0.1f;
    [SerializeField] private float damagePenaltyScale = 0.1f;
    [SerializeField] private float killReward = 1f;
    [SerializeField] private float deathPenalty = -1f;
    [SerializeField] private float survivalRewardPerStep = 0.001f;
    [SerializeField] private float idealRangeRewardPerStep = 0.002f;
    [SerializeField] private float attackTooFarPenalty = -0.005f;
    [SerializeField] private float healAmountPerAction = 3f;
    [SerializeField] private float idealRangeTolerance = 0.5f;
    [SerializeField] private float meleeTouchRadius = 0.75f;
    [SerializeField] private float meleeGoalReward = 0.5f;
    [SerializeField] private float rangedGoalReward = 0.5f;
    [SerializeField] private float rangedTooClosePenalty = -0.002f;
    [Header("Distance Shaping")]
    [SerializeField] private float distancePenaltyScale = 0.002f;
    [SerializeField] private float approachProgressReward = 0.01f;
    [SerializeField] private float approachProgressThreshold = 0.1f;

    private EnemyStateManager _enemy;
    private EnemyHealth _enemyHealth;
    private float _previousDistance = float.PositiveInfinity;

    private void Awake()
    {
        _enemy = GetComponent<EnemyStateManager>();
    }

    public override void Initialize()
    {
        base.Initialize();
        CacheReferences();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        if (_enemyHealth != null)
        {
            _enemyHealth.RestoreToFull();
        }

        _previousDistance = float.PositiveInfinity;

        var player = _enemy != null ? _enemy.PlayerTarget : null;
        if (player != null && player.PlayerHealth != null)
        {
            player.PlayerHealth.RestoreToFull();
        }
    }

    private void RewardDistanceProgress(float currentDistance)
    {
        if (_enemy == null)
            return;

        float maxRange = Mathf.Max(0.1f, _enemy.MaxDetectionRange);
        if (distancePenaltyScale != 0f)
        {
            float normalized = Mathf.Clamp01(currentDistance / maxRange);
            AddReward(-distancePenaltyScale * normalized);
        }

        if (approachProgressReward <= 0f)
        {
            _previousDistance = currentDistance;
            return;
        }

        if (!float.IsPositiveInfinity(_previousDistance))
        {
            float delta = _previousDistance - currentDistance;
            if (delta > approachProgressThreshold)
            {
                AddReward(approachProgressReward);
            }
            else if (delta < -approachProgressThreshold)
            {
                AddReward(-approachProgressReward);
            }
        }

        _previousDistance = currentDistance;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_enemy == null)
        {
            AddZeroObservations(sensor);
            return;
        }

        Vector2 enemyPos = _enemy.transform.position;
        PlayerStateManager playerTarget = _enemy.PlayerTarget;
        Vector2 playerPos = playerTarget != null
            ? (Vector2)playerTarget.transform.position
            : enemyPos;
        Vector2 delta = playerPos - enemyPos;
        float maxRange = Mathf.Max(1f, Mathf.Max(_enemy.MaxDetectionRange, maxRangeNormalization));
        float relativePosX = Mathf.Clamp(delta.x / maxRange, -1f, 1f);
        float relativePosY = Mathf.Clamp(delta.y / maxRange, -1f, 1f);
        float distanceNormalized = Mathf.Clamp01(delta.magnitude / maxRange);
        float inCombatFlag = _enemy.IsInCombat ? 1f : 0f;
        float cooldownNormalized = _enemy.AttackCooldownNormalized;
        float preferredRangeNormalized = Mathf.Clamp01(_enemy.PreferredAttackRange / maxRange);
        int allies = EnemyStateManager.CountAlliesNearby(_enemy, allyDetectionRadius);
        float alliesNormalized = maxAlliesForNormalization > 0
            ? Mathf.Clamp01((float)allies / maxAlliesForNormalization)
            : 0f;

        sensor.AddObservation(_enemy.CurrentHealthNormalized);        // hp normalized
        sensor.AddObservation(relativePosX);                          // relative X
        sensor.AddObservation(relativePosY);                          // relative Y
        sensor.AddObservation(distanceNormalized);                    // distance normalized
        sensor.AddObservation(inCombatFlag);                          // in combat flag
        sensor.AddObservation(cooldownNormalized);                    // attack cooldown normalized
        sensor.AddObservation(preferredRangeNormalized);              // preferred range normalized
        sensor.AddObservation(alliesNormalized);                      // allies nearby normalized
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(survivalRewardPerStep);

        if (_enemy == null)
            return;

        int action = actions.DiscreteActions.Length > 0 ? actions.DiscreteActions[0] : 0;
        Vector2 enemyPos = _enemy.transform.position;
        PlayerStateManager playerTarget = _enemy.PlayerTarget;
        Vector2 playerPos = playerTarget != null
            ? (Vector2)playerTarget.transform.position
            : enemyPos;
        Vector2 toPlayer = playerPos - enemyPos;
        Vector2 direction = toPlayer.sqrMagnitude > Mathf.Epsilon ? toPlayer.normalized : Vector2.zero;

        switch (action)
        {
            case 0: // Noop
                _enemy.StopController();
                break;
            case 1: // Move toward player
                IssueMove(direction);
                break;
            case 2: // Move away from player
                IssueMove(-direction);
                break;
            case 3: // Attack
                HandleAttackAction();
                break;
            case 4: // Heal (optional)
                HandleHealAction();
                break;
        }

        float distance = toPlayer.magnitude;
        RewardIdealRange(distance);
        RewardDistanceProgress(distance);
        EvaluateGoalState(distance);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;
        if (Input.GetKey(KeyCode.Alpha1)) discrete[0] = 1;
        if (Input.GetKey(KeyCode.Alpha2)) discrete[0] = 2;
        if (Input.GetKey(KeyCode.Alpha3)) discrete[0] = 3;
        if (Input.GetKey(KeyCode.Alpha4)) discrete[0] = 4;
    }

    private void CacheReferences()
    {
        if (_enemy == null)
            return;

        _enemyHealth = GetComponent<EnemyHealth>();
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDamaged += HandleEnemyDamaged;
            _enemyHealth.OnDeath += HandleEnemyDeath;
        }
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDamaged -= HandleEnemyDamaged;
            _enemyHealth.OnDeath -= HandleEnemyDeath;
        }
    }

    private void AddZeroObservations(VectorSensor sensor)
    {
        for (int i = 0; i < 8; i++)
        {
            sensor.AddObservation(0f);
        }
    }

    private void IssueMove(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        _enemy.MoveController(direction, _enemy.WalkSpeed);
    }

    private void HandleAttackAction()
    {
        if (_enemy.TryPerformAttack())
        {
            return;
        }

        AddReward(attackTooFarPenalty);
    }

    private void HandleHealAction()
    {
        if (healAmountPerAction <= 0f)
            return;

        _enemy.TryHealSelf(healAmountPerAction);
    }

    private void RewardIdealRange(float distanceToPlayer)
    {
        if (_enemy.enemyData == null || !_enemy.enemyData.isRanged)
            return;

        float preferred = _enemy.PreferredAttackRange;
        if (Mathf.Abs(distanceToPlayer - preferred) <= Mathf.Max(0.1f, idealRangeTolerance))
        {
            AddReward(idealRangeRewardPerStep);
        }
    }

    private void HandleEnemyDamaged(EnemyHealth _, float amount)
    {
        AddReward(-damagePenaltyScale * amount);
    }

    private void HandleEnemyDeath(EnemyHealth _)
    {
        AddReward(deathPenalty);
        EndEpisode();
    }

    private void EvaluateGoalState(float distanceToPlayer)
    {
        if (_enemy.enemyData != null && !_enemy.enemyData.isRanged)
        {
            if (distanceToPlayer <= Mathf.Max(0.05f, meleeTouchRadius))
            {
                AddReward(meleeGoalReward);
                EndEpisode();
            }
            return;
        }

        float minSafe = _enemy.enemyData != null && _enemy.enemyData.minimumSafeDistance > 0f
            ? _enemy.enemyData.minimumSafeDistance
            : 1.5f;
        if (distanceToPlayer < minSafe)
        {
            AddReward(rangedTooClosePenalty);
        }
    }

    public void OnPlayerDamaged(float amount, bool killed)
    {
        AddReward(damageRewardScale * amount);

        if (_enemy.enemyData != null && !_enemy.enemyData.isRanged)
        {
            AddReward(meleeGoalReward);
            if (killed)
            {
                AddReward(killReward);
            }
            EndEpisode();
            return;
        }

        AddReward(rangedGoalReward);
        if (killed)
        {
            AddReward(killReward);
        }
        EndEpisode();
    }
}
