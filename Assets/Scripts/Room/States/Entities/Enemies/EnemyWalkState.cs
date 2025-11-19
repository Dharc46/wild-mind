using UnityEngine;

public class EnemyWalkState : EnemyBaseState
{
    private float _moveDuration;
    private float _movementTimer;
    // keeps track of whether we just hit a wall
    private bool _bumped;

    private readonly Vector2[] _directions = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };

    private readonly ICharacterController _controller;

    public EnemyWalkState(EnemyStateManager entity) : base(entity)
    {
        _controller = enemy.GetComponent<ICharacterController>();
    }

    public override void EnterState()
    {
        enemy.Animator.SetBool("IsWalking", true);
        _moveDuration = 0;
        _movementTimer = 0;
        _bumped = false;
    }

    public override void Update()
    {
        if (_moveDuration == 0 || _bumped)
        {
            // Set an initial move duration and direction
            _moveDuration = Random.Range(1, 3);
            _movementTimer = 0;
            Vector2 nextDir = _directions[Random.Range(0, _directions.Length)];
            if (nextDir == enemy.Direction) return;

            enemy.Direction = nextDir;

        }
        else if (_movementTimer > _moveDuration)
        {
            _movementTimer = 0;

            // chance to go idle
            if (Random.Range(1, 4) == 1)
            {
                enemy.TransitionToState(enemy.IdleState);
            }
            else
            {
                _moveDuration = Random.Range(1, 3);
                enemy.Direction = _directions[Random.Range(0, _directions.Length)];
            }
        }

        _movementTimer += Time.deltaTime;
        _bumped = false;

    }

    public override void FixedUpdate()
    {
        UpdateAnimation();
        // Prefer cached ICharacterController if available so states depend on the interface
        if (_controller != null)
        {
            _bumped = _controller.MovePosition(enemy.Direction, enemy.WalkSpeed);
        }
        else if (enemy.CharacterController != null)
        {
            _bumped = enemy.CharacterController.MovePosition(enemy.Direction, enemy.WalkSpeed);
        }
        else
        {
            // Fallback to existing wrapper which calls the concrete controller
            _bumped = enemy.MoveController(enemy.Direction, enemy.WalkSpeed);
        }
    }

    private void UpdateAnimation()
    {
        enemy.Animator.SetFloat("MoveX", enemy.Direction.x);
        enemy.Animator.SetFloat("MoveY", enemy.Direction.y);
    }
}
