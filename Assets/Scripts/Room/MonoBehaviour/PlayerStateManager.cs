using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerStateManager : EntityStateManager
{
    [Header("Combat")]
    [Tooltip("Damage dealt by the sword swing state per hit.")]
    public float swordDamage = 5f;

    private PlayerIdleState _idleState;
    private PlayerWalkState _walkState;
    private PlayerShiftState _shiftState;
    private PlayerSwingSwordState _swingSwordState;
    [SerializeField]
    private PlayerHealth _playerHealth;

    public PlayerIdleState IdleState { get => _idleState; }
    public PlayerWalkState WalkState { get => _walkState; }
    public PlayerShiftState ShiftState { get => _shiftState; }
    public PlayerSwingSwordState SwingSwordState { get => _swingSwordState; }
    public PlayerHealth PlayerHealth => _playerHealth;

    private void Awake()
    {
        if (_playerHealth == null)
        {
            _playerHealth = GetComponent<PlayerHealth>();
        }
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
        if (_playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth component missing. Cannot apply damage.", this);
            return;
        }

        _playerHealth.TakeDamage(amount);
    }
}
