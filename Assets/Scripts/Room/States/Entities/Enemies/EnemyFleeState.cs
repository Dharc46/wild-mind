using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyFleeState : EnemyBaseState
{
    private readonly ICharacterController _controller;
    private Vector2 _targetPosition;
    private bool _hasTargetPosition;

    private float _fleeDuration;
    private float _timer;
    private float _maxTimeout = 6.0f;
    private float _repathTimer;
    private const float RepathInterval = 1.5f;

    private Transform _playerTransform;

    public EnemyFleeState(EnemyStateManager entity) : base(entity)
    {
        _controller = enemy.GetComponent<ICharacterController>();
    }

    public override void EnterState()
    {
        enemy.Animator.SetBool("IsWalking", true);
        _timer = 0f;
        _repathTimer = 0f;
        // Determine flee time based on data or default
        _fleeDuration = Random.Range(2.0f, 4.0f);
        _maxTimeout = 6.0f;
        _hasTargetPosition = false;

        // Try to locate player
        var player = GameObject.FindObjectOfType<PlayerStateManager>();
        if (player != null)
            _playerTransform = player.transform;
        else
            _playerTransform = null;

        _hasTargetPosition = SelectFleeTarget();
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        // If timed out, resume roaming
        if (_timer > _maxTimeout)
        {
            enemy.TransitionToState(enemy.WalkState);
            return;
        }

        if (_repathTimer >= RepathInterval && _playerTransform != null)
        {
            _repathTimer = 0f;
            if (SelectFleeTarget())
                _hasTargetPosition = true;
        }

        // If we have a target cover position and reached it, stop fleeing
        if (_hasTargetPosition)
        {
            if (Vector2.Distance(enemy.transform.position, _targetPosition) < 0.5f)
            {
                enemy.TransitionToState(enemy.IdleState);
                return;
            }
        }
        else if (_playerTransform != null)
        {
            // If we've backed away enough from the player, stop fleeing
            float currentDist = Vector2.Distance(enemy.transform.position, _playerTransform.position);
            float safeDist = (enemy.enemyData != null) ? enemy.enemyData.minimumSafeDistance : 3f;
            if (currentDist >= safeDist || _timer > _fleeDuration)
            {
                enemy.TransitionToState(enemy.IdleState);
                return;
            }
        }
    }

    public override void FixedUpdate()
    {
        UpdateAnimation();

        float speed = (enemy.enemyData != null) ? enemy.enemyData.movementSpeed : enemy.WalkSpeed;

        if (!_hasTargetPosition)
            return;

        Vector2 toTarget = _targetPosition - (Vector2)enemy.transform.position;
        float distance = toTarget.magnitude;
        Vector2 moveDir = distance > Mathf.Epsilon ? toTarget / distance : Vector2.zero;
        enemy.Direction = moveDir;

        if (_controller != null)
        {
            _controller.MoveTo(_targetPosition);
        }
        else if (enemy.CharacterController != null)
        {
            enemy.CharacterController.MovePosition(moveDir, speed);
        }
        else
        {
            enemy.MoveController(moveDir, speed);
        }
    }

    private void UpdateAnimation()
    {
        enemy.Animator.SetFloat("MoveX", enemy.Direction.x);
        enemy.Animator.SetFloat("MoveY", enemy.Direction.y);
    }

    // Attempts to find a cover point within enemyData.coverSearchRadius that increases distance to player
    private bool TryFindCoverPoint(out Vector2 coverPoint)
    {
        coverPoint = Vector2.zero;
        if (_playerTransform == null)
            return false;

        float radius = enemy.enemyData != null
            ? Mathf.Max(1f, enemy.enemyData.coverSearchRadius)
            : 6f;

        int samples = 16;
        LayerMask mask = enemy.coverObstructionMask;
        return LineOfSight.TryFindCover(enemy.transform.position, _playerTransform, radius, samples, mask, out coverPoint);
    }

    private bool SelectFleeTarget()
    {
        if (TryFindCoverPoint(out Vector2 cover))
        {
            _targetPosition = cover;
            _hasTargetPosition = true;
            return true;
        }

        return SelectFallbackTarget();
    }

    private bool SelectFallbackTarget()
    {
        Vector2 origin = enemy.transform.position;
        Vector2 direction = GetAwayDirection();
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        float distance = enemy.enemyData != null
            ? Mathf.Max(enemy.enemyData.minimumSafeDistance, 2f)
            : 3f;

        _targetPosition = origin + direction * distance;
        _hasTargetPosition = true;
        return true;
    }

    private Vector2 GetAwayDirection()
    {
        if (_playerTransform != null)
        {
            Vector2 dir = (Vector2)(enemy.transform.position - _playerTransform.position);
            if (dir.sqrMagnitude > Mathf.Epsilon)
                return dir.normalized;
        }

        Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        if (randomDir.sqrMagnitude <= Mathf.Epsilon)
            randomDir = Vector2.up;
        return randomDir.normalized;
    }
}
