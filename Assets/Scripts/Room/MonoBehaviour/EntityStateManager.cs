using UnityEngine;

public abstract class EntityStateManager : MonoBehaviour
{
    // Physics
    protected Rigidbody2D _rigidbody2d;
    protected Collider2D _collider;
    protected KinematicTopDownController _kinematicController;
    protected ICharacterController _characterController;
    [HideInInspector] public Rigidbody2D Rigidbody2d { get { return _rigidbody2d; } }
    [HideInInspector] public Collider2D Collider { get { return _collider; } }
    [HideInInspector] public KinematicTopDownController KinematicController { get => _kinematicController; }
    [HideInInspector] public ICharacterController CharacterController { get => _characterController; }

    // Animator
    protected Animator _animator;
    [HideInInspector] public Animator Animator { get { return _animator; } }

    // States
    protected IState _currentState;
    public IState CurrentState { get { return _currentState; } }

    // Others
    [SerializeField]
    protected float walkSpeed = 3.0f;
    public float WalkSpeed { get { return walkSpeed; } }
    protected Vector2 _direction;
    public Vector2 Direction { get => _direction; set => _direction = value; }

    protected virtual void Start()
    {
        _rigidbody2d = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        _kinematicController = GetComponent<KinematicTopDownController>();
        _characterController = GetComponent<ICharacterController>();
        // Note: wrapper methods below allow states to request movement without depending on concrete controller API.
    }

    // Wrapper to move the underlying kinematic controller and return whether it bumped into an obstacle.
    public bool MoveController(Vector2 movement, float speed)
    {
        if (_kinematicController == null)
            return false;

        return _kinematicController.MovePosition(movement, speed);
    }

    // Wrapper to set direct velocity on the underlying controller.
    public void SetControllerVelocity(Vector2 velocity)
    {
        if (_kinematicController == null)
            return;

        _kinematicController.SetVelocity(velocity);
    }

    // Wrapper to stop the underlying controller.
    public void StopController()
    {
        if (_kinematicController == null)
            return;

        _kinematicController.Stop();
    }

    protected virtual void Update()
    {
        if (_currentState == null)
            return;

        _currentState.Update();
    }

    protected virtual void FixedUpdate()
    {
        if (_currentState == null)
            return;

        _currentState.FixedUpdate();
    }

    public void TransitionToState(IState state)
    {
        _currentState = state;
        _currentState.EnterState();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_currentState == null)
            return;

        _currentState.OnCollisionEnter2D(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (_currentState == null)
            return;

        _currentState.OnCollisionExit2D(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_currentState == null)
            return;

        _currentState.OnTriggerEnter2D(collision);
    }
}
