using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Enemy state that keeps a preferred distance to the player and fires projectiles.
/// Requires a projectile prefab to be assigned on the EnemyStateManager.
/// </summary>
public class EnemyRangedState : EnemyBaseState
{
    private readonly ICharacterController _controller;
    private PlayerStateManager _player;

    private float _fireCooldownTimer;
    private float _preferredRange;
    private float _minRange;
    private float _maxRange;
    private float _distanceTolerance;

    private const float ReacquireInterval = 1.5f;
    private float _reacquireTimer;

    public EnemyRangedState(EnemyStateManager entity) : base(entity)
    {
        _controller = enemy.GetComponent<ICharacterController>();
    }

    public override void EnterState()
    {
        _fireCooldownTimer = 0f;
        _reacquireTimer = 0f;
        CacheRangeWindow();
        enemy.Animator.SetBool("IsWalking", false);
        AcquirePlayer(true);
        Debug.Log("GHOST ĐÃ VÀO RangedAttackState", enemy);
    }

    public override void Update()
    {
        if (!EnsurePlayer())
            return;

        Vector2 enemyPos = enemy.transform.position;
        Vector2 playerPos = _player.transform.position;
        Vector2 toPlayer = playerPos - enemyPos;
        float distance = toPlayer.magnitude;

        float maxDetection = enemy.enemyData != null && enemy.enemyData.detectionRadius > 0f
            ? enemy.enemyData.detectionRadius
            : _maxRange + 2f;

        if (distance > maxDetection * 1.25f)
        {
            enemy.TransitionToState(enemy.WalkState);
            return;
        }

        Vector2 aimDirection = toPlayer.sqrMagnitude > Mathf.Epsilon
            ? toPlayer.normalized
            : enemy.Direction;

        UpdateAnimation(aimDirection);
        HandleMovement(distance, aimDirection);
        HandleFiring(distance, aimDirection);
    }

    private void HandleMovement(float distance, Vector2 aimDirection)
    {
        float delta = distance - _preferredRange;
        if (Mathf.Abs(delta) <= _distanceTolerance)
        {
            StopMotion();
            return;
        }

        Vector2 moveDir = delta > 0f ? aimDirection : -aimDirection;
        moveDir.Normalize();

        float moveSpeed = enemy.enemyData != null && enemy.enemyData.movementSpeed > 0f
            ? enemy.enemyData.movementSpeed
            : enemy.WalkSpeed;

        bool bumped = false;
        if (_controller != null)
            bumped = _controller.MovePosition(moveDir, moveSpeed);
        else if (enemy.CharacterController != null)
            bumped = enemy.CharacterController.MovePosition(moveDir, moveSpeed);
        else
            bumped = enemy.MoveController(moveDir, moveSpeed);

        enemy.Animator.SetBool("IsWalking", !bumped);
    }

    private void HandleFiring(float distance, Vector2 aimDirection)
    {
        _fireCooldownTimer -= Time.deltaTime;
        if (_fireCooldownTimer > 0f)
            return;

        // only fire when roughly within preferred window to keep rhythm
        if (distance < _minRange || distance > _maxRange)
            return;

        if (!FireProjectile(aimDirection))
            return;

        _fireCooldownTimer = GetFireCooldown();
        enemy.NotifyAttackPerformed();
    }

    private void CacheRangeWindow()
    {
        _preferredRange = enemy.enemyData != null && enemy.enemyData.preferredAttackRange > 0f
            ? enemy.enemyData.preferredAttackRange
            : 5f;

        _distanceTolerance = 0.35f;
        float window = 0.75f;
        _minRange = Mathf.Max(0.5f, _preferredRange - window);
        _maxRange = _preferredRange + window;
    }

    private float GetFireCooldown()
    {
        if (enemy.enemyData != null && enemy.enemyData.attackSpeed > 0f)
        {
            return Mathf.Max(0.1f, 1f / enemy.enemyData.attackSpeed);
        }

        return Mathf.Max(0.1f, enemy.rangedFireCooldown);
    }

    private bool FireProjectile(Vector2 direction)
    {
        if (enemy.projectilePrefab == null)
        {
            Debug.LogWarning($"Enemy '{enemy.name}' tried to fire without a projectile prefab assigned.", enemy);
            return false;
        }

        // Trigger attack animation
        if (enemy.Animator != null)
        {
            enemy.Animator.SetTrigger("Attack");
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = Vector2.right;

        if (enemy.projectileSpreadDegrees > 0f)
        {
            float offset = Random.Range(-enemy.projectileSpreadDegrees * 0.5f, enemy.projectileSpreadDegrees * 0.5f);
            direction = (Quaternion.AngleAxis(offset, Vector3.forward) * direction).normalized;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 enemyPos = enemy.transform.position;
        Vector3 spawnPos = enemy.projectileSpawnPoint != null
            ? enemy.projectileSpawnPoint.position
            : enemyPos;
        Vector3 playerPos = _player != null
            ? _player.transform.position
            : enemy.transform.position;

        if (spawnPos.sqrMagnitude < 1f)
        {
            Vector3 dir = (playerPos - enemyPos).normalized;
            spawnPos = enemyPos + dir * 0.6f;
        }

        Debug.Log($"[GHOST DEBUG] ENEMY: {enemyPos} | SPAWN: {spawnPos} | PLAYER: {playerPos} | DELTA: {Vector3.Distance(spawnPos, playerPos):F1}m");
        Debug.Log($"[GHOST DEBUG] SPAWN POINT NULL? {enemy.projectileSpawnPoint == null}", enemy.gameObject);

        GameObject projectile = Object.Instantiate(enemy.projectilePrefab, spawnPos, Quaternion.identity);

        var projectileBehaviour = projectile.GetComponent<EnemyProjectile>();
        if (projectileBehaviour != null)
        {
            float damage = enemy.enemyData != null ? enemy.enemyData.damage : 0f;
            projectileBehaviour.Initialize(enemy, damage);
        }

        float speed = Mathf.Max(0.1f, enemy.projectileSpeed);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }
        else
        {
            projectile.transform.position = spawnPos;
            projectile.transform.right = direction;
        }

        if (enemy.projectileLifetime > 0f)
        {
            Object.Destroy(projectile, enemy.projectileLifetime);
        }

        return true;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        enemy.Direction = direction;
        enemy.Animator.SetFloat("MoveX", direction.x);
        enemy.Animator.SetFloat("MoveY", direction.y);
    }

    private void StopMotion()
    {
        enemy.SetControllerVelocity(Vector2.zero);
        enemy.StopController();
        enemy.Animator.SetBool("IsWalking", false);
    }

    private bool EnsurePlayer()
    {
        if (_player != null)
            return true;

        if (!AcquirePlayer(false))
        {
            enemy.TransitionToState(enemy.IdleState);
            return false;
        }

        return true;
    }

    private bool AcquirePlayer(bool force)
    {
        if (!force)
        {
            _reacquireTimer += Time.deltaTime;
            if (_reacquireTimer < ReacquireInterval)
                return _player != null;
        }

        _reacquireTimer = 0f;
        _player = GameObject.FindObjectOfType<PlayerStateManager>();
        return _player != null;
    }
}


