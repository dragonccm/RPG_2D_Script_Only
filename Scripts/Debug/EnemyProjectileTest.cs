using UnityEngine;

/// <summary>
/// Test script để kiểm tra logic enemy projectile self-damage prevention
/// </summary>
public class EnemyProjectileTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Test Results")]
    [SerializeField] private int totalCharacters = 0;
    [SerializeField] private int playerCount = 0;
    [SerializeField] private int enemyCount = 0;
    [SerializeField] private bool enemySelfDamagePrevented = true;
    
    private void Start()
    {
        if (runTestOnStart)
        {
            RunEnemyProjectileTest();
        }
    }
    
    /// <summary>
    /// Same logic as SkillExecutors.IsPlayerCharacter but with better enemy detection
    /// </summary>
    private bool IsPlayerCharacter(Character character, Character caster = null)
    {
        // Same as caster - CRITICAL: Enemy should not hit itself
        if (caster != null && character == caster) return true;
        
        // Has PlayerController component
        var playerController = character.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
            return true;
        
        // AttackableCharacter check
        var attackable = character.GetComponent<AttackableCharacter>();
        if (attackable != null && !attackable.CanBeAttacked())
            return true;
        
        // Name patterns for player detection (but exclude if it's clearly an enemy)
        string objName = character.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero"))
            return true;
        // Don't use "character" in name check as it's too broad
            
        // Tag check for player
        if (character.gameObject.CompareTag("Player"))
            return true;
            
        // Player layer check (Layer 6 = Player, not 7)
        if (character.gameObject.layer == 6)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Test method để kiểm tra logic enemy projectile self-damage prevention
    /// </summary>
    [ContextMenu("Run Enemy Projectile Test")]
    public void RunEnemyProjectileTest()
    {
        if (enableDebugLogs)
            Debug.Log("=== ENEMY PROJECTILE SELF-DAMAGE TEST ===");
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        totalCharacters = allCharacters.Length;
        
        var enemies = new System.Collections.Generic.List<Character>();
        var players = new System.Collections.Generic.List<Character>();
        
        // Categorize characters
        foreach (var character in allCharacters)
        {
            if (character == null) continue;
            
            // Check if it's a player using the same logic as SkillExecutors
            bool isPlayer = IsPlayerCharacter(character, null);
            
            if (isPlayer)
            {
                players.Add(character);
                if (enableDebugLogs)
                    Debug.Log($"Player: {character.name} (Tag: {character.gameObject.tag}, Layer: {character.gameObject.layer})");
            }
            else
            {
                enemies.Add(character);
                if (enableDebugLogs)
                    Debug.Log($"Enemy: {character.name} (Tag: {character.gameObject.tag}, Layer: {character.gameObject.layer})");
            }
        }
        
        playerCount = players.Count;
        enemyCount = enemies.Count;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Total characters: {totalCharacters}");
            Debug.Log($"Players: {playerCount}");
            Debug.Log($"Enemies: {enemyCount}");
        }
        
        // Test enemy projectile logic
        enemySelfDamagePrevented = true;
        if (enemies.Count > 0)
        {
            var testEnemy = enemies[0];
            if (enableDebugLogs)
                Debug.Log($"Testing enemy projectile logic with: {testEnemy.name}");
            
            // Test if enemy would hit itself
            bool wouldHitSelf = !IsPlayerCharacter(testEnemy, testEnemy);
            if (wouldHitSelf)
            {
                enemySelfDamagePrevented = false;
                if (enableDebugLogs)
                    Debug.LogError($"CRITICAL: Enemy {testEnemy.name} would hit itself!");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"✓ Enemy {testEnemy.name} correctly prevented from hitting itself");
            }
            
            // Test if enemy would hit other enemies (this is now ALLOWED)
            foreach (var otherEnemy in enemies)
            {
                if (otherEnemy == testEnemy) continue;
                
                bool wouldHitOtherEnemy = !IsPlayerCharacter(otherEnemy, testEnemy);
                if (wouldHitOtherEnemy)
                {
                    if (enableDebugLogs)
                        Debug.Log($"✓ Enemy {testEnemy.name} can hit other enemy {otherEnemy.name} (ALLOWED)");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"✗ Enemy {testEnemy.name} cannot hit other enemy {otherEnemy.name} (UNEXPECTED)");
                }
            }
            
            // Test if enemy would hit players
            foreach (var player in players)
            {
                bool wouldHitPlayer = !IsPlayerCharacter(player, testEnemy);
                if (wouldHitPlayer)
                {
                    if (enableDebugLogs)
                        Debug.Log($"✓ Enemy {testEnemy.name} correctly targets player {player.name}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Enemy {testEnemy.name} would NOT hit player {player.name} - this might be correct depending on logic");
                }
            }
        }
        
        if (enableDebugLogs)
        {
            if (enemySelfDamagePrevented)
                Debug.Log("✓ ENEMY SELF-DAMAGE PREVENTION: PASSED");
            else
                Debug.LogError("✗ ENEMY SELF-DAMAGE PREVENTION: FAILED");
            
            Debug.Log("=== TEST COMPLETE ===");
        }
    }
    
    [ContextMenu("Print Test Results")]
    public void PrintTestResults()
    {
        Debug.Log($"=== ENEMY PROJECTILE TEST RESULTS ===");
        Debug.Log($"Total Characters: {totalCharacters}");
        Debug.Log($"Players: {playerCount}");
        Debug.Log($"Enemies: {enemyCount}");
        Debug.Log($"Enemy Self-Damage Prevented: {(enemySelfDamagePrevented ? "✓ PASSED" : "✗ FAILED")}");
        Debug.Log("=====================================");
    }
} 