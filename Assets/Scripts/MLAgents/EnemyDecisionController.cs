using UnityEngine;

/// <summary>
/// Adapter that consumes EnemyAgent outputs and issues commands to the FSM.
/// Keeps Agent logic decoupled from EnemyStateManager.
/// </summary>
[RequireComponent(typeof(EnemyStateManager))]
[RequireComponent(typeof(EnemyAgent))]
public class EnemyDecisionController : MonoBehaviour
{
    [Tooltip("Minimum magnitude required before movement commands are sent to the FSM.")]
    public float movementThreshold = 0.1f;

    private EnemyStateManager _enemy;
    private EnemyAgent _agent;

    private Vector2 _pendingMove;

    private void Awake()
    {
        _enemy = GetComponent<EnemyStateManager>();
        _agent = GetComponent<EnemyAgent>();
    }

    private void Update()
    {
        if (_enemy == null)
            return;

        if (_pendingMove.sqrMagnitude >= movementThreshold * movementThreshold)
        {
            _enemy.MoveController(_pendingMove.normalized, _enemy.WalkSpeed);
        }
    }

    /// <summary>
    /// Called by EnemyAgent after it decodes action buffers.
    /// </summary>
    public void SetMoveCommand(Vector2 desiredMovement)
    {
        _pendingMove = desiredMovement;
    }

    /// <summary>
    /// Illustrative hook for firing transitions (idle/walk/ranged/etc.).
    /// Actual ML training can call this with enumerated states.
    /// </summary>
    public void RequestState(string stateName)
    {
        if (_enemy == null)
            return;

        switch (stateName)
        {
            case "Idle":
                _enemy.TransitionToState(_enemy.IdleState);
                break;
            case "Walk":
                _enemy.TransitionToState(_enemy.WalkState);
                break;
            case "Ranged":
                _enemy.TransitionToState(_enemy.RangedState);
                break;
            case "Flee":
                _enemy.TransitionToState(_enemy.FleeState);
                break;
            default:
                break;
        }
    }
}
