using UnityEngine;

/// <summary>
/// ?? UNIFIED ENEMY TYPE SYSTEM
/// Defines enemy attributes and applies appropriate configurations
/// </summary>
public class EnemyType : MonoBehaviour
{
    [Header("?? ENEMY TYPE")]
    public Type enemyType = Type.Melee;
    
    [Header("?? AUTO-SETUP")]
    public bool autoApplyOnStart = true;
    
    public enum Type
    {
        Melee,      // C?n chi?n
        Ranged,     // T?m xa
        Boss        // Boss
    }
    
    [System.Serializable]
    public struct EnemyStats
    {
        public float health;
        public float damage;
        public float attackRange;
        public float detectionRange;
        public float attackCooldown;
        public float moveSpeed;
    }
    
    private readonly EnemyStats[] ENEMY_STAT_TEMPLATES = {
        new EnemyStats { // Melee
            health = 100f,
            damage = 30f,
            attackRange = 2f,
            detectionRange = 12f,
            attackCooldown = 1.2f,
            moveSpeed = 3.5f
        },
        new EnemyStats { // Ranged
            health = 80f,
            damage = 25f,
            attackRange = 8f,
            detectionRange = 15f,
            attackCooldown = 1.8f,
            moveSpeed = 2.5f
        },
        new EnemyStats { // Boss
            health = 500f,
            damage = 75f,
            attackRange = 5f,
            detectionRange = 20f,
            attackCooldown = 1f,
            moveSpeed = 4f
        }
    };
    
    private void Start()
    {
        if (autoApplyOnStart)
        {
            ApplyType();
        }
    }
    
    public void ApplyType()
    {
        var coreEnemy = GetComponent<CoreEnemy>();
        if (coreEnemy != null)
        {
            ApplyStatsToEnemy(coreEnemy);
        }
        
        SetupSkillSystem();
        SetupSpecialFeatures();
    }
    
    public void ApplyStatsToEnemy(CoreEnemy enemy)
    {
        var stats = ENEMY_STAT_TEMPLATES[(int)enemyType];
        
        enemy.SetStats(
            health: stats.health,
            damage: stats.damage,
            attackRng: stats.attackRange,
            detectionRng: stats.detectionRange
        );
        
        enemy.attackCooldown = stats.attackCooldown;
        
        // Apply NavMeshAgent speed
        var agent = enemy.Agent;
        if (agent != null)
        {
            agent.speed = stats.moveSpeed;
        }
    }
    
    private void SetupSkillSystem()
    {
        // Ensure EnemySkillManager exists
        var skillManager = GetComponent<EnemySkillManager>();
        if (skillManager == null)
        {
            skillManager = gameObject.AddComponent<EnemySkillManager>();
        }
        
        // Configure skill manager based on type
        var stats = ENEMY_STAT_TEMPLATES[(int)enemyType];
        skillManager.attackDamage = stats.damage;
        skillManager.attackRange = stats.attackRange;
        skillManager.attackCooldown = stats.attackCooldown;
    }
    
    private void SetupSpecialFeatures()
    {
        switch (enemyType)
        {
            case Type.Boss:
                SetupBossFeatures();
                break;
            case Type.Ranged:
                SetupRangedFeatures();
                break;
            case Type.Melee:
                SetupMeleeFeatures();
                break;
        }
    }
    
    private void SetupBossFeatures()
    {
        // Add boss-specific components if they don't exist
        if (GetComponent<EnemyElite>() == null)
        {
            gameObject.AddComponent<EnemyElite>();
        }
        
        if (GetComponent<TelegraphManager>() == null)
        {
            gameObject.AddComponent<TelegraphManager>();
        }
        
        // Ensure SpecialMovement for boss
        if (GetComponent<SpecialMovement>() == null)
        {
            gameObject.AddComponent<SpecialMovement>();
        }
        
        // Boss gets advanced skills
        var skillManager = GetComponent<EnemySkillManager>();
        if (skillManager != null)
        {
            skillManager.useAdvancedSkills = true;
        }
    }
    
    private void SetupRangedFeatures()
    {
        // Ranged enemies prefer to keep distance
        var coreEnemy = GetComponent<CoreEnemy>();
        if (coreEnemy != null)
        {
            // Increase chase range for ranged enemies
            coreEnemy.chaseRange = coreEnemy.detectionRange * 1.8f;
        }
    }
    
    private void SetupMeleeFeatures()
    {
        // Melee enemies are more aggressive
        var coreEnemy = GetComponent<CoreEnemy>();
        if (coreEnemy != null)
        {
            // Decrease chase range for melee enemies
            coreEnemy.chaseRange = coreEnemy.detectionRange * 1.3f;
        }
    }
    
    // Public API
    public bool IsMelee => enemyType == Type.Melee;
    public bool IsRanged => enemyType == Type.Ranged;
    public bool IsBoss => enemyType == Type.Boss;
    
    public float GetOptimalAttackRange()
    {
        return ENEMY_STAT_TEMPLATES[(int)enemyType].attackRange;
    }
    
    public float GetPreferredDistance()
    {
        return enemyType switch
        {
            Type.Melee => 1f,
            Type.Ranged => 6f,
            Type.Boss => 4f,
            _ => 3f
        };
    }
    
    public EnemyStats GetStats()
    {
        return ENEMY_STAT_TEMPLATES[(int)enemyType];
    }
    
    // CONTEXT MENU HELPERS
    [ContextMenu("?? Apply Type Settings")]
    public void ApplyTypeFromMenu()
    {
        ApplyType();
        UnityEngine.Debug.Log($"Applied {enemyType} settings to {gameObject.name}");
    }
    
    [ContextMenu("?? Show Stats")]
    public void ShowStats()
    {
        var stats = ENEMY_STAT_TEMPLATES[(int)enemyType];
        UnityEngine.Debug.Log($"[{enemyType}] HP:{stats.health} DMG:{stats.damage} Range:{stats.attackRange} Detection:{stats.detectionRange}");
    }
}