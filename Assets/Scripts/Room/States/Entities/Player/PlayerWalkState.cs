using UnityEngine;

public class PlayerWalkState : PlayerBaseState
{
    private readonly ICharacterController _controller;

    public PlayerWalkState(EntityStateManager entity) : base(entity)
    {
        _controller = player.GetComponent<ICharacterController>();
    }

    public override void EnterState()
    {
        player.Animator.SetBool("IsWalking", true);
    }

    public override void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.TransitionToState(player.SwingSwordState);
        }

        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");

        // Allow the character to move one direction at time!
        Vector2 movement = Vector2.zero;
        if (horizontalMovement != 0.0f)
        {
            movement.x = horizontalMovement;
        }
        else if (verticalMovement != 0.0f)
        {
            movement.y = verticalMovement;
        }

        // Update the Animator only when motion
        if (movement != Vector2.zero)
        {
            player.Direction = movement;
            player.Animator.SetFloat("MoveX", horizontalMovement);
            player.Animator.SetFloat("MoveY", verticalMovement);
        }
        else
        {
            // No motion, transition to Idle keeping the previous animation
            player.TransitionToState(player.IdleState);
        }
    }

    public override void FixedUpdate()
    {
        // Use ICharacterController if available (cached); fallback to state manager wrapper
        if (_controller != null)
        {
            _controller.SetVelocity(player.Direction * player.WalkSpeed);
        }
        else if (player.CharacterController != null)
        {
            player.CharacterController.SetVelocity(player.Direction * player.WalkSpeed);
        }
        else
        {
            player.SetControllerVelocity(player.Direction * player.WalkSpeed);
        }
    }
}
