using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Damage dealt when the projectile connects with the player.")]
    public float damage = 5f;

    [Tooltip("Enemy that spawned this projectile (ignored for collision checks).")]
    public EnemyStateManager owner;

    [Tooltip("Whether the projectile should be destroyed when hitting solid geometry even if it misses the player."), SerializeField]
    private bool destroyOnObstruction = true;

    private bool _hasHit;

    private void Awake()
    {
        // ensure collider stays as trigger for consistent interactions
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public void Initialize(EnemyStateManager spawningEnemy, float damageAmount)
    {
        owner = spawningEnemy;
        damage = damageAmount;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit)
            return;

        if (owner != null && collision.GetComponentInParent<EnemyStateManager>() == owner)
            return;

        if (collision.GetComponentInParent<EnemyStateManager>() != null)
            return; // ignore other enemies to prevent friendly fire for now

        if (collision.CompareTag("Player") || collision.CompareTag("PlayerHurtBox"))
        {
            DamagePlayer(collision);
            return;
        }

        if (destroyOnObstruction && !collision.isTrigger)
        {
            // stop when touching environment
            _hasHit = true;
            Destroy(gameObject);
        }
    }

    private void DamagePlayer(Collider2D hit)
    {
        var player = hit.GetComponent<PlayerStateManager>() ?? hit.GetComponentInParent<PlayerStateManager>();
        if (player == null)
            return;

        _hasHit = true;
        float previousHealth = player.PlayerHealth != null ? player.PlayerHealth.CurrentHealth : 0f;
        player.TakeDamage(damage);

        bool killed = player.PlayerHealth != null && !player.PlayerHealth.IsAlive;
        float appliedDamage = player.PlayerHealth != null
            ? Mathf.Max(0f, previousHealth - player.PlayerHealth.CurrentHealth)
            : damage;

        if (owner != null && appliedDamage > 0f)
        {
            owner.NotifyPlayerDamaged(appliedDamage, killed);
        }

        Destroy(gameObject);
    }
}
