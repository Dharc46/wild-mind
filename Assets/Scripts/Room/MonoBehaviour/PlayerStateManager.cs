using UnityEngine;

public class PlayerStateManager : EntityStateManager
{
    [Header("Combat")]
    [Tooltip("Damage dealt by the sword swing state per hit.")]
    public float swordDamage = 5f;

    private PlayerIdleState _idleState;
    private PlayerWalkState _walkState;
    private PlayerShiftState _shiftState;
    private PlayerSwingSwordState _swingSwordState;

    public PlayerIdleState IdleState { get => _idleState; }
    public PlayerWalkState WalkState { get => _walkState; }
    public PlayerShiftState ShiftState { get => _shiftState; }
    public PlayerSwingSwordState SwingSwordState { get => _swingSwordState; }

    private void Awake()
    {
        _idleState = new PlayerIdleState(this);
        _walkState = new PlayerWalkState(this);
        _shiftState = new PlayerShiftState(this);
        _swingSwordState = new PlayerSwingSwordState(this);
    }

    protected override void Start()
    {
        base.Start();
        Doorway.ShiftRoomEvent += OnShiftRoom;
        TransitionToState(IdleState);
    }

    private void OnShiftRoom(Doorway door)
    {
        TransitionToState(ShiftState);
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"player hit by NPC for {amount} damage");
    }
}
