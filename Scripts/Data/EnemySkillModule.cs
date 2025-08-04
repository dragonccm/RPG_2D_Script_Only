using UnityEngine;

/// <summary>
/// ?? ENEMY SKILL MODULE
/// Pre-configured SkillModule specifically designed for Enemy use
/// Automatically configured for enemy combat without mana requirements
/// </summary>
[CreateAssetMenu(fileName = "EnemySkillModule", menuName = "RPG/Enemy Skill Module")]
public class EnemySkillModule : SkillModule
{
    [Header("?? ENEMY-SPECIFIC SETTINGS")]
    [Tooltip("Enemy skill level (for boss phases)")]
    public int enemySkillLevel = 1;
    
    [Tooltip("AI priority - higher means more likely to be used")]
    [Range(1, 10)]
    public int aiPriority = 5;
    
    [Tooltip("Preferred distance to use this skill")]
    public float preferredDistance = 3f;
    
    [Tooltip("Can this skill be used when low health?")]
    public bool canUseWhenLowHealth = true;
    
    [Tooltip("Health threshold to activate this skill (0-1)")]
    [Range(0f, 1f)]
    public float healthThreshold = 0.3f;
    
    private void OnEnable()
    {
        ConfigureForEnemy();
    }
    
    private void ConfigureForEnemy()
    {
        // Enemy skills don't require mana
        manaCost = 0f;
        
        // Set reasonable defaults for enemy use
        if (requiredLevel <= 0) requiredLevel = 1;
        if (cooldown <= 0) cooldown = 2f;
        if (damage <= 0) damage = 25f;
        if (range <= 0) range = 3f;
        
        // Configure animation for enemies
        if (string.IsNullOrEmpty(animationTrigger))
        {
            animationTrigger = "Attack";
        }
        
        // Enable damage area for better visual feedback
        showDamageArea = true;
        
        // Set appropriate colors for enemy skills
        if (skillColor == Color.white)
        {
            skillColor = skillType switch
            {
                SkillType.Melee => Color.red,
                SkillType.Projectile => Color.orange,
                SkillType.Area => Color.magenta,
                SkillType.Support => Color.green,
                SkillType.Instant => Color.yellow,
                _ => Color.red
            };
        }
        
        UpdateDamageAreaColorForEnemy();
    }
    
    private void UpdateDamageAreaColorForEnemy()
    {
        // Make enemy damage areas more threatening
        var baseColor = skillColor;
        damageAreaColor = new Color(baseColor.r, baseColor.g * 0.5f, baseColor.b * 0.5f, 0.5f);
    }
    
    /// <summary>
    /// Check if this skill should be used at current distance
    /// </summary>
    public bool IsPreferredAtDistance(float currentDistance)
    {
        float tolerance = range * 0.3f; // 30% tolerance
        return Mathf.Abs(currentDistance - preferredDistance) <= tolerance;
    }
    
    /// <summary>
    /// Check if this skill can be used at current health
    /// </summary>
    public bool CanUseAtHealth(float currentHealthPercent)
    {
        if (!canUseWhenLowHealth && currentHealthPercent < healthThreshold)
        {
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Get AI score for this skill based on context
    /// </summary>
    public float GetAIScore(float distanceToTarget, float currentHealthPercent)
    {
        float score = aiPriority;
        
        // Distance preference bonus
        if (IsPreferredAtDistance(distanceToTarget))
        {
            score += 2f;
        }
        
        // Health-based bonus
        if (currentHealthPercent < healthThreshold && canUseWhenLowHealth)
        {
            score += 3f; // Desperate skills get priority
        }
        
        // Range check
        if (distanceToTarget > range)
        {
            score = 0f; // Can't use if out of range
        }
        
        return score;
    }
    
    protected override void OnValidate()
    {
        // Call parent validation first
        base.OnValidate();
        
        // Ensure enemy-specific values are valid
        aiPriority = Mathf.Clamp(aiPriority, 1, 10);
        preferredDistance = Mathf.Max(0.1f, preferredDistance);
        healthThreshold = Mathf.Clamp01(healthThreshold);
        enemySkillLevel = Mathf.Max(1, enemySkillLevel);
        
        // Auto-configure for enemy use
        ConfigureForEnemy();
    }
    
    /// <summary>
    /// Create a quick enemy skill in code
    /// </summary>
    public static EnemySkillModule CreateEnemySkill(string name, SkillType type, float damage, float range, float cooldown)
    {
        var skill = CreateInstance<EnemySkillModule>();
        skill.skillName = name;
        skill.skillType = type;
        skill.damage = damage;
        skill.range = range;
        skill.cooldown = cooldown;
        skill.ConfigureForEnemy();
        return skill;
    }
}