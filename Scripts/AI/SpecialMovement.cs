using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ? UNIFIED SPECIAL MOVEMENT SYSTEM
/// Central controller for all special movements with auto-trigger logic
/// CORE c?a h? th?ng special movements
/// </summary>
public class SpecialMovement : MonoBehaviour
{
    [Header("? TRIGGER SETTINGS")]
    public bool enableAutoTrigger = true;
    public float lowHealthThreshold = 0.3f;
    public float playerNearDistance = 4f;
    public float cooldownBetweenMovements = 8f;
    
    [Header("?? MOVEMENT PRIORITY")]
    [SerializeField] private MovementPriority movementPriority = MovementPriority.Random;
    [SerializeField] private bool allowRepeatedMovements = true;
    
    public enum MovementPriority
    {
        Random,         // Pick random available movement
        Sequential,     // Use movements in order
        HealthBased,    // Lower health = more desperate movements
        DistanceBased   // Closer player = different movement types
    }
    
    // UNIFIED COMPONENTS ACCESS
    private CoreEnemy coreEnemy;
    private Character character;
    private List<IUnifiedSpecialMovement> specialMovements = new List<IUnifiedSpecialMovement>();
    
    // UNIFIED STATE TRACKING
    private float lastSpecialMovementTime = -999f;
    private float lastCheckTime = 0f;
    private string lastUsedMovement = "";
    private const float CHECK_INTERVAL = 1f;
    
    // UNIFIED STATISTICS
    private Dictionary<string, int> movementUsageCount = new Dictionary<string, int>();
    private Dictionary<string, float> movementLastUsed = new Dictionary<string, float>();
    
    private void Awake()
    {
        InitializeComponents();
        CollectSpecialMovements();
    }
    
    private void Update()
    {
        if (!enableAutoTrigger) return;
        if (Time.time - lastCheckTime < CHECK_INTERVAL) return;
        
        lastCheckTime = Time.time;
        CheckAndTriggerSpecialMovement();
    }
    
    private void InitializeComponents()
    {
        coreEnemy = GetComponent<CoreEnemy>();
        character = GetComponent<Character>();
    }
    
    private void CollectSpecialMovements()
    {
        specialMovements.Clear();
        
        // Find all IUnifiedSpecialMovement components
        var components = GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component is IUnifiedSpecialMovement specialMovement)
            {
                specialMovements.Add(specialMovement);
                
                // Initialize usage tracking
                string movementName = specialMovement.MovementName;
                if (!movementUsageCount.ContainsKey(movementName))
                {
                    movementUsageCount[movementName] = 0;
                    movementLastUsed[movementName] = -999f;
                }
            }
        }
        
        UnityEngine.Debug.Log($"[SpecialMovement] Found {specialMovements.Count} special movements on {gameObject.name}");
    }
    
    private void CheckAndTriggerSpecialMovement()
    {
        if (specialMovements.Count == 0) return;
        if (Time.time - lastSpecialMovementTime < cooldownBetweenMovements) return;
        if (coreEnemy == null || !coreEnemy.IsAlive) return;
        
        bool shouldTrigger = false;
        TriggerReason reason = TriggerReason.None;
        
        // UNIFIED TRIGGER CONDITIONS
        
        // Condition 1: Low health
        if (coreEnemy.GetHealthPercent() <= lowHealthThreshold)
        {
            shouldTrigger = true;
            reason = TriggerReason.LowHealth;
        }
        
        // Condition 2: Player too close
        var currentTarget = coreEnemy.GetCurrentTarget();
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance <= playerNearDistance)
            {
                shouldTrigger = true;
                if (reason == TriggerReason.None)
                    reason = TriggerReason.PlayerNear;
                else
                    reason = TriggerReason.Combined;
            }
        }
        
        // Condition 3: Under heavy pressure (being attacked frequently)
        if (character != null && character.CurrentHealth < character.MaxHealth * 0.5f)
        {
            // Check if health decreased recently
            // This would require health change tracking - simplified for now
        }
        
        if (shouldTrigger)
        {
            TriggerBestMovement(reason);
        }
    }
    
    private void TriggerBestMovement(TriggerReason reason)
    {
        var availableMovements = GetAvailableMovements();
        if (availableMovements.Count == 0) return;
        
        IUnifiedSpecialMovement selectedMovement = null;
        
        // UNIFIED MOVEMENT SELECTION
        switch (movementPriority)
        {
            case MovementPriority.Random:
                selectedMovement = SelectRandomMovement(availableMovements);
                break;
            case MovementPriority.Sequential:
                selectedMovement = SelectSequentialMovement(availableMovements);
                break;
            case MovementPriority.HealthBased:
                selectedMovement = SelectHealthBasedMovement(availableMovements);
                break;
            case MovementPriority.DistanceBased:
                selectedMovement = SelectDistanceBasedMovement(availableMovements);
                break;
        }
        
        if (selectedMovement != null)
        {
            ExecuteMovement(selectedMovement, reason);
        }
    }
    
    private List<IUnifiedSpecialMovement> GetAvailableMovements()
    {
        var available = new List<IUnifiedSpecialMovement>();
        
        foreach (var movement in specialMovements)
        {
            if (movement.CanActivate())
            {
                // Check if we should avoid repeated movements
                if (!allowRepeatedMovements && movement.MovementName == lastUsedMovement)
                {
                    continue;
                }
                
                available.Add(movement);
            }
        }
        
        return available;
    }
    
    private IUnifiedSpecialMovement SelectRandomMovement(List<IUnifiedSpecialMovement> available)
    {
        if (available.Count == 0) return null;
        int randomIndex = Random.Range(0, available.Count);
        return available[randomIndex];
    }
    
    private IUnifiedSpecialMovement SelectSequentialMovement(List<IUnifiedSpecialMovement> available)
    {
        // Find the movement that was used least recently
        IUnifiedSpecialMovement oldest = null;
        float oldestTime = float.MaxValue;
        
        foreach (var movement in available)
        {
            float lastUsed = movementLastUsed.GetValueOrDefault(movement.MovementName, -999f);
            if (lastUsed < oldestTime)
            {
                oldestTime = lastUsed;
                oldest = movement;
            }
        }
        
        return oldest;
    }
    
    private IUnifiedSpecialMovement SelectHealthBasedMovement(List<IUnifiedSpecialMovement> available)
    {
        float healthPercent = coreEnemy.GetHealthPercent();
        
        // At very low health, prefer escape movements
        if (healthPercent < 0.2f)
        {
            var escapeMovement = available.Find(m => m.MovementName.ToLower().Contains("teleport"));
            if (escapeMovement != null) return escapeMovement;
        }
        
        // At medium health, prefer aggressive movements
        if (healthPercent < 0.5f)
        {
            var aggressiveMovement = available.Find(m => m.MovementName.ToLower().Contains("dash"));
            if (aggressiveMovement != null) return aggressiveMovement;
        }
        
        // Default to random
        return SelectRandomMovement(available);
    }
    
    private IUnifiedSpecialMovement SelectDistanceBasedMovement(List<IUnifiedSpecialMovement> available)
    {
        var target = coreEnemy.GetCurrentTarget();
        if (target == null) return SelectRandomMovement(available);
        
        float distance = Vector3.Distance(transform.position, target.position);
        
        // If very close, prefer escape
        if (distance < 2f)
        {
            var escapeMovement = available.Find(m => m.MovementName.ToLower().Contains("teleport"));
            if (escapeMovement != null) return escapeMovement;
        }
        
        // If medium distance, prefer dash attacks
        if (distance < 6f)
        {
            var dashMovement = available.Find(m => m.MovementName.ToLower().Contains("dash"));
            if (dashMovement != null) return dashMovement;
        }
        
        // Default to random
        return SelectRandomMovement(available);
    }
    
    private void ExecuteMovement(IUnifiedSpecialMovement movement, TriggerReason reason)
    {
        movement.Activate();
        
        // Update tracking
        lastSpecialMovementTime = Time.time;
        lastUsedMovement = movement.MovementName;
        movementUsageCount[movement.MovementName]++;
        movementLastUsed[movement.MovementName] = Time.time;
        
        UnityEngine.Debug.Log($"[SpecialMovement] {gameObject.name} used {movement.MovementName} (Reason: {reason})");
    }
    
    #region UNIFIED PUBLIC API
    
    public void TriggerSpecialMovement(string movementName)
    {
        var movement = specialMovements.Find(m => m.MovementName == movementName);
        if (movement != null && movement.CanActivate())
        {
            ExecuteMovement(movement, TriggerReason.Manual);
        }
    }
    
    public void TriggerAnyAvailable()
    {
        TriggerBestMovement(TriggerReason.Manual);
    }
    
    public bool HasSpecialMovements()
    {
        return specialMovements.Count > 0;
    }
    
    public List<string> GetAvailableMovementNames()
    {
        var available = GetAvailableMovements();
        var names = new List<string>();
        foreach (var movement in available)
        {
            names.Add(movement.MovementName);
        }
        return names;
    }
    
    public List<string> GetAllMovementNames()
    {
        var names = new List<string>();
        foreach (var movement in specialMovements)
        {
            names.Add(movement.MovementName);
        }
        return names;
    }
    
    public void RefreshMovements()
    {
        CollectSpecialMovements();
    }
    
    public Dictionary<string, int> GetMovementUsageStats()
    {
        return new Dictionary<string, int>(movementUsageCount);
    }
    
    #endregion
    
    #region CONTEXT MENU HELPERS
    
    [ContextMenu("? Refresh Special Movements")]
    public void RefreshMovementsFromMenu()
    {
        RefreshMovements();
        UnityEngine.Debug.Log($"Refreshed movements. Found: {specialMovements.Count}");
    }
    
    [ContextMenu("?? Trigger Random Movement")]
    public void TriggerRandomFromMenu()
    {
        TriggerAnyAvailable();
    }
    
    [ContextMenu("?? Show Movement Stats")]
    public void ShowMovementStats()
    {
        UnityEngine.Debug.Log($"=== MOVEMENT STATS for {gameObject.name} ===");
        foreach (var kvp in movementUsageCount)
        {
            float lastUsed = movementLastUsed.GetValueOrDefault(kvp.Key, -999f);
            string lastUsedStr = lastUsed > 0 ? $"{Time.time - lastUsed:F1}s ago" : "Never";
            UnityEngine.Debug.Log($"- {kvp.Key}: Used {kvp.Value} times, Last: {lastUsedStr}");
        }
    }
    
    #endregion
    
    // UNIFIED ENUMS
    private enum TriggerReason
    {
        None,
        LowHealth,
        PlayerNear,
        Combined,
        Manual
    }
}

// UNIFIED INTERFACE - Renamed to avoid conflicts
public interface IUnifiedSpecialMovement
{
    string MovementName { get; }
    bool CanActivate();
    void Activate();
}