using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/EnemyData", fileName = "NewEnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyID;
    public string enemyName;
    public Sprite sprite;

    [Header("Core Stats")]
    public float maxHealth = 30f;
    public float damage = 5f;
    public float attackSpeed = 1f; // attacks per second
    public float movementSpeed = 3f;
    public float detectionRadius = 8f;
    public float attackRange = 1f;

    [Header("Loot / Drops")]
    // Placeholder for drop table; define ItemDrop elsewhere if needed
    // public List<ItemDrop> lootTable;

    [Header("AI / RL Parameters")]
    [Tooltip("If true, this enemy prefers ranged attacks and will try to keep distance.")]
    public bool isRanged = false;

    [Range(0f, 1f), Tooltip("Fraction of max health at which the enemy will consider fleeing.")]
    public float fleeHealthThreshold = 0.25f;

    [Tooltip("Seconds to wait after last combat before starting to heal.")]
    public float outOfCombatHealDelay = 3f;

    [Tooltip("Health points regained per second while healing out of combat.")]
    public float healPerSecond = 5f;
    [Range(0f, 1f), Tooltip("Fraction of max HP to heal up to when out of combat (1 = full).")]
    public float healStopFraction = 1f;

    [Tooltip("Preferred attack distance for ranged enemies (world units).")]
    public float preferredAttackRange = 6f;

    [Tooltip("Minimum safe distance for ranged enemies; if player is closer than this, enemy will try to back off.")]
    public float minimumSafeDistance = 3f;

    [Tooltip("Radius (world units) to search for nearby cover positions when fleeing.")]
    public float coverSearchRadius = 10f;

    [Header("Debug / Tuning")]
    public string notes = "";
}
