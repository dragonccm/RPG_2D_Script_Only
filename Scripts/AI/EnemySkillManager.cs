using UnityEngine;

/// <summary>
/// ?? ENEMY SKILL MANAGER - Simplified version
/// Basic skill management for enemies without complex dependencies
/// </summary>
public class EnemySkillManager : MonoBehaviour
{
    [Header("?? BASIC SKILLS")]
    public float attackDamage = 30f;
    public float attackRange = 3f;
    public float attackCooldown = 1.5f;
    public bool useAdvancedSkills = false;
    
    private float lastAttackTime = -999f;
    private Character character;
    private CoreEnemy coreEnemy;
    
    private void Awake()
    {
        character = GetComponent<Character>();
        coreEnemy = GetComponent<CoreEnemy>();
    }
    
    public bool CanUseSkill()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    public void UseSkill()
    {
        if (!CanUseSkill()) return;
        
        lastAttackTime = Time.time;
        
        // Get target from CoreEnemy
        Transform target = coreEnemy?.GetCurrentTarget();
        if (target == null) return;
        
        // Check if target is in range
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange) return;
        
        // Apply damage
        var targetCharacter = target.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(attackDamage);
            UnityEngine.Debug.Log($"[EnemySkillManager] {gameObject.name} attacked {target.name} for {attackDamage} damage");
        }
    }
    
    public void UseSkill(string skillName)
    {
        // For backward compatibility
        UseSkill();
    }
    
    public bool CanUseSkill(string skillName)
    {
        return CanUseSkill();
    }
}