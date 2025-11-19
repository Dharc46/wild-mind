using UnityEngine;

public interface ICharacterController
{
    // Moves using the controller's collision-aware movement. Returns true if movement was occluded (bumped).
    bool MovePosition(Vector2 movement, float speed);
    // Move toward a world-space target position (may be pathfinding-aware)
    void MoveTo(Vector2 worldTarget);

    // Set direct velocity (world-space units per second)
    void SetVelocity(Vector2 velocity);

    // Stop movement immediately
    void Stop();

    // Optional: query whether controller is currently moving
    bool IsMoving { get; }
}
