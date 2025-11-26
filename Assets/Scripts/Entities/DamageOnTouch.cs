using UnityEngine;

public class DamageOnTouch : MonoBehaviour
{
    public int damage = 10;
    public string targetTag = "Player";
    public float damageCooldown = 1f;

    private float lastDamageTime = 0f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag(targetTag))
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        var health = collision.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        lastDamageTime = Time.time;

        var enemyState = GetComponentInParent<EnemyStateManager>();
        if (enemyState != null && enemyState.enemyData != null && !enemyState.enemyData.isRanged)
        {
            enemyState.NotifyPlayerDamaged(damage, false);
        }
    }
}
