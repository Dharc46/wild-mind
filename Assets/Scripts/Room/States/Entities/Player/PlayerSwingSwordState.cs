using System;
using UnityEngine;

public class PlayerSwingSwordState : PlayerBaseState
{
    //private BoxCollider2D _hitBox;
    private float _elapsedTime;
    private readonly float _animDuration = 0.33f;

    public PlayerSwingSwordState(EntityStateManager entity) : base(entity) { }

    public override void EnterState()
    {
        _elapsedTime = 0f;
        player.Animator.SetTrigger("SwingSword");
    }

    public override void Update()
    {
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _animDuration)
            player.TransitionToState(player.IdleState);

    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // The HurtBox collider will be involved as it's the only set to trigger
            var enemyManager = collision.GetComponent<EnemyStateManager>();
            if (enemyManager != null)
            {
                enemyManager.Hit(player.Direction);

                if (player.swordDamage > 0f)
                {
                    var enemyHealth = collision.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(player.swordDamage);
                    }
                }
            }
        }
    }
}
