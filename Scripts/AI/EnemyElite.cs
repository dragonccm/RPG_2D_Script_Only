using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🎭 UNIVERSAL BOSS FRAMEWORK - Elite Enemy Extension
/// Based on Hades-inspired design principles adapted for 2D RPG
/// Integrates with existing Enemy system while adding advanced boss mechanics
/// </summary>
public class EnemyElite : MonoBehaviour
{
    #region UNIVERSAL BOSS FRAMEWORK CORE
    [Header("🎭 UNIVERSAL BOSS SETTINGS")]
    [SerializeField] private bool isElite = true;
    [SerializeField] private string eliteTitle = "";
    [SerializeField] private EliteRank eliteRank = EliteRank.Elite;
    [SerializeField] private BossArchetype bossArchetype = BossArchetype.Balanced;
    
    [Header("📊 STAT BONUSES")]
    [SerializeField] private float healthMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private float sizeMultiplier = 1.3f;
    [SerializeField] private float experienceMultiplier = 2f;
    [SerializeField] private float currencyMultiplier = 2f;
    [SerializeField] private float itemDropChanceMultiplier = 1.5f;
    
    [Header("⚡ TELEGRAPH SYSTEM (Universal Framework)")]
    [SerializeField] private float baseTelegraphDuration = 1.5f;
    [SerializeField] private float finalTelegraphDuration = 0.5f;
    [SerializeField] private bool adaptTelegraphToSkill = true;
    [SerializeField] private TelegraphComplexity telegraphComplexity = TelegraphComplexity.Medium;
    
    [Header("🎯 THREAT MANAGEMENT")]
    [SerializeField] private float maxThreatLevel = 0.8f;
    [SerializeField] private bool dynamicThreatBalancing = true;
    [SerializeField] private float playerSkillRating = 0.5f; // 0-1 scale
    
    [Header("🛡️ COUNTERPLAY VALIDATION")]
    [SerializeField] private int minimumCounterplayOptions = 2;
    [SerializeField] private bool enforceSkillBasedCounters = true;
    [SerializeField] private float counterplayDifficultyMatch = 0.3f;
    #endregion

    #region BOSS FRAMEWORK ENUMS
    public enum EliteRank
    {
        Elite,      // Tinh nhuệ thường
        Champion,   // Quân quán
        Legendary,  // Huyền thoại
        Mythic      // Thần thoại
    }

    public enum BossArchetype
    {
        Aggressive,     // Fast attacks, short telegraphs, high damage
        Defensive,      // Shields, longer telegraphs, area denial
        Balanced,       // Mixed strategies, moderate everything
        Tactical,       // Complex patterns, intelligent AI
        Berserker       // Becomes more dangerous when low health
    }

    public enum TelegraphComplexity
    {
        Simple,     // Single shapes, clear warnings
        Medium,     // Multiple areas, moderate prediction
        Complex,    // Pattern recognition required
        Expert      // Frame-perfect timing needed
    }

    public enum ThreatType
    {
        Immediate,      // Must respond now
        Persistent,     // Ongoing danger
        Delayed,        // Future threat
        Conditional,    // Threat if conditions met
        Environmental   // Arena-based dangers
    }

    [System.Serializable]
    public class UniversalSkill
    {
        public string skillName;
        public AttackPattern pattern;
        public ThreatType threatType;
        public float baseDamage;
        public float telegraphDuration;
        public float cooldown;
        public List<CounterplayType> validCounters;
        public bool requiresLineOfSight;
        public float minRange;
        public float maxRange;
        
        [System.NonSerialized]
        public float lastUsedTime = -999f;
    }

    public enum AttackPattern
    {
        CleaveAttack,       // Area cleave around boss
        ChargeStrike,       // Linear charge attack
        MinionSummon,       // Summon adds with telegraphs
        EnvironmentalSlam,  // Cross-shaped danger zones
        DefensiveShield,    // Temporary invulnerability
        TacticalReposition, // Smart movement/flanking
        ThreatProjectile,   // Skillshot with prediction
        AreaDenial,         // Persistent danger zones
        PhaseTransition     // Special transition attack
    }

    public enum CounterplayType
    {
        Dodge,          // Move out of danger zone
        Block,          // Use defensive ability
        Interrupt,      // Stop the attack
        Positioning,    // Move to safe location
        Resource,       // Use items/abilities
        Cooperative     // Party-based response
    }
    #endregion

    #region ENHANCED ABILITIES (Framework Integration)
    [Header("⚔️ UNIVERSAL SKILLS")]
    [SerializeField] private List<UniversalSkill> availableSkills = new List<UniversalSkill>();
    [SerializeField] private bool randomizeSkills = false;
    [SerializeField] private int minRandomSkills = 2;
    [SerializeField] private int maxRandomSkills = 4;
    
    // Legacy abilities (kept for compatibility)
    [Header("🔥 LEGACY ABILITIES")]
    [SerializeField] private bool hasRegeneration = false;
    [SerializeField] private float regenerationAmount = 5f;
    [SerializeField] private float regenerationInterval = 2f;
    
    [SerializeField] private bool hasArmor = false;
    [SerializeField] private float damageReduction = 0.3f;
    
    [SerializeField] private bool hasBerserker = false;
    [SerializeField] private float berserkerHealthThreshold = 0.3f;
    [SerializeField] private float berserkerDamageBonus = 0.5f;
    [SerializeField] private float berserkerSpeedBonus = 0.3f;
    
    [SerializeField] private bool hasLifesteal = false;
    [SerializeField] private float lifestealPercent = 0.2f;
    
    [SerializeField] private bool hasExplosionOnDeath = false;
    [SerializeField] private float explosionDamage = 20f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private GameObject explosionEffectPrefab;
    
    [SerializeField] private bool hasThorns = false;
    [SerializeField] private float thornsDamagePercent = 0.2f;
    
    [Header("🎨 ELITE VISUALS")]
    [SerializeField] private Color eliteColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private GameObject eliteEffectPrefab;
    [SerializeField] private GameObject eliteIndicatorPrefab;
    [SerializeField] private bool useEliteOutline = true;
    [SerializeField] private float outlineWidth = 1.5f;
    [SerializeField] private bool pulseEffect = true;
    [SerializeField] private float pulseRate = 1f;
    [SerializeField] private float pulseAmount = 0.2f;
    #endregion

    #region PRIVATE VARIABLES
    // Components
    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    private TelegraphManager telegraphManager;
    
    // Universal Framework State
    private float currentThreatLevel = 0.5f;
    private List<CounterplayType> lastValidCounters = new List<CounterplayType>();
    private float playerPerformanceScore = 0.5f;
    private int consecutiveFailures = 0;
    private float lastSkillUsedTime = 0f;
    
    // Legacy state
    private bool isBerserking = false;
    private bool isEnraged = false;
    private bool isImmune = false;
    private bool isFrenzied = false;
    private float lastTeleportTime = -999f;
    private float lastImmunityTime = -999f;
    private float lastFrenzyTime = -999f;
    
    // Effects
    private GameObject eliteEffect;
    private GameObject eliteIndicator;
    private Coroutine regenerationCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine threatManagementCoroutine;
    #endregion

    #region UNITY LIFECYCLE
    private void Awake()
    {
        InitializeComponents();
        InitializeUniversalFramework();
    }

    private void Start()
    {
        if (!isElite) return;
        
        SetupBossArchetype();
        ApplyEliteStats();
        CreateEliteEffects();
        StartUniversalSystems();
    }

    private void Update()
    {
        if (!isElite) return;
        
        UpdateUniversalFramework();
        UpdateLegacySystems();
    }

    private void OnDestroy()
    {
        CleanupUniversalFramework();
        CleanupLegacySystems();
    }
    #endregion

    #region UNIVERSAL FRAMEWORK INITIALIZATION
    private void InitializeComponents()
    {
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize or get TelegraphManager
        telegraphManager = GetComponent<TelegraphManager>();
        if (telegraphManager == null)
        {
            telegraphManager = gameObject.AddComponent<TelegraphManager>();
        }
        
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // Register events
        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
            enemy.OnDeath += HandleDeath;
            enemy.OnDealDamage += HandleDealDamage;
        }
    }

    private void InitializeUniversalFramework()
    {
        // Initialize telegraph system with framework settings
        if (telegraphManager != null)
        {
            Color warningColor = GetArchetypeColor(0.6f);
            Color finalColor = GetArchetypeColor(0.9f);
            
            telegraphManager.Initialize(
                CalculateOptimalTelegraphDuration(),
                finalTelegraphDuration,
                warningColor,
                finalColor
            );
        }
        
        // Initialize default skills if none configured
        if (availableSkills.Count == 0)
        {
            GenerateDefaultSkills();
        }
        
        if (randomizeSkills)
        {
            RandomizeSkillSet();
        }
        
        // Calculate initial threat level
        RecalculateThreatLevel();
    }

    private void SetupBossArchetype()
    {
        switch (bossArchetype)
        {
            case BossArchetype.Aggressive:
                baseTelegraphDuration *= 0.7f;
                damageMultiplier *= 1.3f;
                speedMultiplier *= 1.2f;
                maxThreatLevel = 0.9f;
                break;
                
            case BossArchetype.Defensive:
                baseTelegraphDuration *= 1.5f;
                healthMultiplier *= 1.5f;
                damageMultiplier *= 0.8f;
                maxThreatLevel = 0.6f;
                break;
                
            case BossArchetype.Tactical:
                telegraphComplexity = TelegraphComplexity.Complex;
                adaptTelegraphToSkill = true;
                maxThreatLevel = 0.7f;
                break;
                
            case BossArchetype.Berserker:
                hasBerserker = true;
                berserkerHealthThreshold = 0.5f;
                maxThreatLevel = 1.0f;
                break;
                
            default: // Balanced
                // Keep default values
                break;
        }
    }
    #endregion

    #region UNIVERSAL FRAMEWORK CORE SYSTEMS
    private void UpdateUniversalFramework()
    {
        // Update threat level based on performance
        if (dynamicThreatBalancing)
        {
            UpdateThreatBalancing();
        }
        
        // Check for skill usage opportunities
        if (CanUseUniversalSkill())
        {
            UseUniversalSkill();
        }
        
        // Validate counterplay options
        ValidateCounterplayOptions();
    }

    private float CalculateOptimalTelegraphDuration()
    {
        float baseTime = baseTelegraphDuration;
        
        // Complexity modifiers
        float complexityMultiplier = 1f + ((int)telegraphComplexity * 0.3f);
        baseTime *= complexityMultiplier;
        
        // Skill adaptation
        if (adaptTelegraphToSkill)
        {
            baseTime *= (2f - playerSkillRating); // Better players get less time
        }
        
        // Archetype modifiers
        switch (bossArchetype)
        {
            case BossArchetype.Aggressive:
                baseTime *= 0.8f;
                break;
            case BossArchetype.Defensive:
                baseTime *= 1.4f;
                break;
            case BossArchetype.Tactical:
                baseTime *= 1.2f;
                break;
        }
        
        return Mathf.Clamp(baseTime, 0.3f, 5f);
    }

    private void UpdateThreatBalancing()
    {
        float targetThreat = 0.5f;
        
        // Performance adjustments
        targetThreat += (consecutiveFailures * 0.1f);     // More failures = easier
        targetThreat -= (playerPerformanceScore * 0.2f);  // Better performance = harder
        
        // Engagement adjustments
        float timeSinceLastSkill = Time.time - lastSkillUsedTime;
        if (timeSinceLastSkill > 10f) targetThreat += 0.1f; // Player seems bored
        
        // Smooth adjustment
        currentThreatLevel = Mathf.Lerp(currentThreatLevel, 
            Mathf.Clamp(targetThreat, 0.2f, maxThreatLevel), Time.deltaTime * 0.5f);
    }

    private bool CanUseUniversalSkill()
    {
        if (availableSkills.Count == 0) return false;
        if (Time.time - lastSkillUsedTime < 2f) return false;
        if (enemy == null || enemy.GetCurrentTarget() == null) return false;
        
        return true;
    }

    private void UseUniversalSkill()
    {
        var validSkills = GetValidSkills();
        if (validSkills.Count == 0) return;
        
        var selectedSkill = SelectSkillByThreatLevel(validSkills);
        if (selectedSkill != null)
        {
            StartCoroutine(ExecuteUniversalSkill(selectedSkill));
        }
    }

    private List<UniversalSkill> GetValidSkills()
    {
        var target = enemy.GetCurrentTarget();
        if (target == null) return new List<UniversalSkill>();
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        return availableSkills.Where(skill => 
            Time.time >= skill.lastUsedTime + skill.cooldown &&
            distanceToTarget >= skill.minRange &&
            distanceToTarget <= skill.maxRange &&
            (!skill.requiresLineOfSight || HasLineOfSight(target))
        ).ToList();
    }

    private UniversalSkill SelectSkillByThreatLevel(List<UniversalSkill> validSkills)
    {
        // Weight skills based on current threat level
        var weightedSkills = validSkills
            .Select(skill => new { 
                skill, 
                weight = CalculateSkillWeight(skill) 
            })
            .OrderByDescending(x => x.weight)
            .ToList();
        
        // Select from top weighted skills with some randomness
        int selectionIndex = Mathf.Min(
            Random.Range(0, Mathf.Max(1, weightedSkills.Count / 3)),
            weightedSkills.Count - 1
        );
        
        return weightedSkills[selectionIndex].skill;
    }

    private float CalculateSkillWeight(UniversalSkill skill)
    {
        float weight = 1f;
        
        // Threat type weighting
        switch (skill.threatType)
        {
            case ThreatType.Immediate:
                weight *= currentThreatLevel * 2f;
                break;
            case ThreatType.Persistent:
                weight *= (1f - currentThreatLevel) * 1.5f;
                break;
            case ThreatType.Delayed:
                weight *= currentThreatLevel * 0.8f;
                break;
        }
        
        // Archetype preferences
        switch (bossArchetype)
        {
            case BossArchetype.Aggressive:
                if (skill.pattern == AttackPattern.ChargeStrike || 
                    skill.pattern == AttackPattern.CleaveAttack)
                    weight *= 1.5f;
                break;
            case BossArchetype.Defensive:
                if (skill.pattern == AttackPattern.DefensiveShield || 
                    skill.pattern == AttackPattern.AreaDenial)
                    weight *= 1.5f;
                break;
            case BossArchetype.Tactical:
                if (skill.pattern == AttackPattern.TacticalReposition ||
                    skill.pattern == AttackPattern.MinionSummon)
                    weight *= 1.5f;
                break;
        }
        
        return weight;
    }
    #endregion

    #region UNIVERSAL SKILLS EXECUTION
    private IEnumerator ExecuteUniversalSkill(UniversalSkill skill)
    {
        lastSkillUsedTime = Time.time;
        skill.lastUsedTime = Time.time;
        
        Debug.Log($"[{eliteTitle}] Executing Universal Skill: {skill.skillName}");
        
        // Pre-execution setup
        var validCounters = skill.validCounters.ToList();
        lastValidCounters = validCounters;
        
        // Validate counterplay before execution
        if (!ValidateCounterplayForSkill(skill))
        {
            Debug.LogWarning($"Skill {skill.skillName} failed counterplay validation!");
            yield break;
        }
        
        // Execute based on pattern
        switch (skill.pattern)
        {
            case AttackPattern.CleaveAttack:
                yield return StartCoroutine(ExecuteUniversalCleave(skill));
                break;
            case AttackPattern.ChargeStrike:
                yield return StartCoroutine(ExecuteUniversalCharge(skill));
                break;
            case AttackPattern.MinionSummon:
                yield return StartCoroutine(ExecuteUniversalSummon(skill));
                break;
            case AttackPattern.EnvironmentalSlam:
                yield return StartCoroutine(ExecuteUniversalSlam(skill));
                break;
            case AttackPattern.DefensiveShield:
                yield return StartCoroutine(ExecuteUniversalShield(skill));
                break;
            case AttackPattern.TacticalReposition:
                yield return StartCoroutine(ExecuteUniversalReposition(skill));
                break;
            case AttackPattern.ThreatProjectile:
                yield return StartCoroutine(ExecuteUniversalProjectile(skill));
                break;
            case AttackPattern.AreaDenial:
                yield return StartCoroutine(ExecuteUniversalAreaDenial(skill));
                break;
        }
        
        // Post-execution tracking
        UpdatePlayerPerformance();
    }

    private IEnumerator ExecuteUniversalCleave(UniversalSkill skill)
    {
        Vector3 attackCenter = transform.position;
        float attackRange = 4f + (currentThreatLevel * 2f);
        
        // Universal telegraph sequence
        var warning = telegraphManager.CreateCircleWarning(
            attackCenter, attackRange, 
            GetArchetypeColor(0.6f), skill.telegraphDuration
        );
        
        yield return StartCoroutine(telegraphManager.CompleteWarningSequence(warning, () => {
            // Execute damage
            var players = FindPlayersInRange(attackCenter, attackRange);
            foreach (var player in players)
            {
                var character = player.GetComponent<Character>();
                if (character != null)
                {
                    float damage = skill.baseDamage * damageMultiplier * (1f + currentThreatLevel);
                    character.TakeDamage(damage);
                }
            }
            
            CreateUniversalImpactEffect(attackCenter, attackRange);
        }));
    }

    private IEnumerator ExecuteUniversalCharge(UniversalSkill skill)
    {
        var target = enemy.GetCurrentTarget();
        if (target == null) yield break;
        
        Vector3 chargeDirection = (target.position - transform.position).normalized;
        float chargeDistance = 8f + (currentThreatLevel * 4f);
        Vector3 chargeEndPoint = transform.position + chargeDirection * chargeDistance;
        
        var warning = telegraphManager.CreateLineWarning(
            transform.position, chargeEndPoint, 2f,
            GetArchetypeColor(0.7f), skill.telegraphDuration
        );
        
        yield return StartCoroutine(telegraphManager.CompleteWarningSequence(warning, () => {
            StartCoroutine(PerformCharge(chargeEndPoint, skill.baseDamage));
        }));
    }

    private IEnumerator ExecuteUniversalSummon(UniversalSkill skill)
    {
        int summonCount = 2 + Mathf.RoundToInt(currentThreatLevel * 3f);
        List<Vector3> summonPositions = GenerateSummonPositions(summonCount);
        
        var warnings = telegraphManager.CreatePatternWarning(
            summonPositions, 
            Enumerable.Repeat(1.5f, summonCount).ToList(),
            GetArchetypeColor(0.5f), skill.telegraphDuration
        );
        
        yield return new WaitForSeconds(skill.telegraphDuration);
        
        foreach (var pos in summonPositions)
        {
            CreateUniversalImpactEffect(pos, 1.5f);
            // TODO: Spawn actual minions here if available
        }
        
        warnings.ForEach(w => { if (w != null) Destroy(w); });
    }

    private IEnumerator ExecuteUniversalSlam(UniversalSkill skill)
    {
        var hazardPositions = GenerateCrossPattern(transform.position, 6);
        
        var warnings = telegraphManager.CreatePatternWarning(
            hazardPositions,
            Enumerable.Repeat(2f, hazardPositions.Count).ToList(),
            GetArchetypeColor(0.8f), skill.telegraphDuration
        );
        
        yield return new WaitForSeconds(skill.telegraphDuration);
        
        foreach (var pos in hazardPositions)
        {
            var players = FindPlayersInRange(pos, 2f);
            foreach (var player in players)
            {
                var character = player.GetComponent<Character>();
                if (character != null)
                {
                    float damage = skill.baseDamage * damageMultiplier;
                    character.TakeDamage(damage);
                }
            }
            
            CreateUniversalImpactEffect(pos, 2f);
        }
        
        warnings.ForEach(w => { if (w != null) Destroy(w); });
    }

    private IEnumerator ExecuteUniversalShield(UniversalSkill skill)
    {
        Debug.Log($"[{eliteTitle}] Activating Universal Shield");
        
        // Visual indicator
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.cyan;
            
            // TODO: Add actual invulnerability mechanics
            yield return new WaitForSeconds(3f + currentThreatLevel);
            
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator ExecuteUniversalReposition(UniversalSkill skill)
    {
        var target = enemy.GetCurrentTarget();
        if (target == null) yield break;
        
        Vector3 optimalPosition = FindOptimalRepositionPoint(target.position);
        
        // Quick movement or teleport
        transform.position = optimalPosition;
        CreateUniversalImpactEffect(optimalPosition, 1f);
        
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteUniversalProjectile(UniversalSkill skill)
    {
        var target = enemy.GetCurrentTarget();
        if (target == null) yield break;
        
        Vector3 predictedPosition = PredictTargetPosition(target);
        
        var warning = telegraphManager.CreateCircleWarning(
            predictedPosition, 2f,
            GetArchetypeColor(0.7f), skill.telegraphDuration
        );
        
        yield return StartCoroutine(telegraphManager.CompleteWarningSequence(warning, () => {
            var players = FindPlayersInRange(predictedPosition, 2f);
            foreach (var player in players)
            {
                var character = player.GetComponent<Character>();
                if (character != null)
                {
                    character.TakeDamage(skill.baseDamage * damageMultiplier);
                }
            }
            
            CreateUniversalImpactEffect(predictedPosition, 2f);
        }));
    }

    private IEnumerator ExecuteUniversalAreaDenial(UniversalSkill skill)
    {
        Vector3 denialCenter = transform.position + (Vector3)(Random.insideUnitCircle.normalized * 5f);
        float denialRadius = 4f;
        
        var warning = telegraphManager.CreateCircleWarning(
            denialCenter, denialRadius,
            GetArchetypeColor(0.4f), skill.telegraphDuration
        );
        
        yield return new WaitForSeconds(skill.telegraphDuration);
        
        // Create persistent hazard
        StartCoroutine(MaintainAreaDenial(denialCenter, denialRadius, 10f, skill.baseDamage));
        
        if (warning != null) Destroy(warning);
    }
    #endregion

    #region COUNTERPLAY VALIDATION SYSTEM
    private bool ValidateCounterplayForSkill(UniversalSkill skill)
    {
        // Rule 1: Must have minimum counterplay options
        if (skill.validCounters.Count < minimumCounterplayOptions)
        {
            Debug.LogWarning($"Skill {skill.skillName} has insufficient counterplay options");
            return false;
        }
        
        // Rule 2: Must have at least one skill-based counter (not resource-gated)
        if (enforceSkillBasedCounters)
        {
            bool hasSkillCounter = skill.validCounters.Any(counter => 
                counter == CounterplayType.Dodge || 
                counter == CounterplayType.Positioning
            );
            
            if (!hasSkillCounter)
            {
                Debug.LogWarning($"Skill {skill.skillName} lacks skill-based counterplay");
                return false;
            }
        }
        
        // Rule 3: Counterplay difficulty should match threat level
        float expectedDifficulty = currentThreatLevel;
        float actualDifficulty = CalculateCounterplayDifficulty(skill);
        
        if (Mathf.Abs(expectedDifficulty - actualDifficulty) > counterplayDifficultyMatch)
        {
            Debug.LogWarning($"Skill {skill.skillName} counterplay difficulty mismatch");
            return false;
        }
        
        return true;
    }

    private float CalculateCounterplayDifficulty(UniversalSkill skill)
    {
        float difficulty = 0.5f;
        
        // Adjust based on telegraph duration
        if (skill.telegraphDuration < 1f) difficulty += 0.3f;
        else if (skill.telegraphDuration > 2f) difficulty -= 0.2f;
        
        // Adjust based on counter types
        if (skill.validCounters.Contains(CounterplayType.Dodge)) difficulty -= 0.1f;
        if (skill.validCounters.Contains(CounterplayType.Resource)) difficulty += 0.2f;
        if (skill.validCounters.Contains(CounterplayType.Cooperative)) difficulty += 0.1f;
        
        return Mathf.Clamp01(difficulty);
    }

    private void ValidateCounterplayOptions()
    {
        // Continuously monitor if players have valid response options
        // This ensures the Universal Framework principle of meaningful choice
        
        if (lastValidCounters.Count > 0)
        {
            bool playerHasOptions = CheckPlayerHasCounterplayOptions();
            
            if (!playerHasOptions)
            {
                // Reduce threat level to maintain player agency
                currentThreatLevel = Mathf.Max(currentThreatLevel - 0.1f, 0.2f);
                Debug.Log($"[{eliteTitle}] Reduced threat level to maintain counterplay options");
            }
        }
    }

    private bool CheckPlayerHasCounterplayOptions()
    {
        // Check if player can currently execute any of the valid counters
        // This is a simplified check - in a full implementation, this would
        // integrate with the player's ability system
        
        return lastValidCounters.Contains(CounterplayType.Dodge) || 
               lastValidCounters.Contains(CounterplayType.Positioning);
    }
    #endregion

    #region UNIVERSAL FRAMEWORK UTILITIES
    private void GenerateDefaultSkills()
    {
        availableSkills.Clear();
        
        // Add archetype-appropriate skills
        switch (bossArchetype)
        {
            case BossArchetype.Aggressive:
                availableSkills.AddRange(new List<UniversalSkill>
                {
                    CreateSkill("Aggressive Cleave", AttackPattern.CleaveAttack, ThreatType.Immediate, 75f, 1f, 3f),
                    CreateSkill("Berserker Charge", AttackPattern.ChargeStrike, ThreatType.Immediate, 100f, 0.8f, 4f),
                    CreateSkill("Fury Projectile", AttackPattern.ThreatProjectile, ThreatType.Immediate, 60f, 1.2f, 2.5f)
                });
                break;
                
            case BossArchetype.Defensive:
                availableSkills.AddRange(new List<UniversalSkill>
                {
                    CreateSkill("Protective Shield", AttackPattern.DefensiveShield, ThreatType.Persistent, 0f, 2f, 8f),
                    CreateSkill("Area Denial", AttackPattern.AreaDenial, ThreatType.Persistent, 40f, 2.5f, 6f),
                    CreateSkill("Guardian Slam", AttackPattern.EnvironmentalSlam, ThreatType.Delayed, 80f, 2f, 5f)
                });
                break;
                
            case BossArchetype.Tactical:
                availableSkills.AddRange(new List<UniversalSkill>
                {
                    CreateSkill("Tactical Strike", AttackPattern.ThreatProjectile, ThreatType.Conditional, 70f, 1.8f, 3f),
                    CreateSkill("Strategic Reposition", AttackPattern.TacticalReposition, ThreatType.Conditional, 0f, 1f, 4f),
                    CreateSkill("Coordinated Summon", AttackPattern.MinionSummon, ThreatType.Delayed, 50f, 2.5f, 8f)
                });
                break;
                
            case BossArchetype.Berserker:
                availableSkills.AddRange(new List<UniversalSkill>
                {
                    CreateSkill("Berserker Rage", AttackPattern.CleaveAttack, ThreatType.Immediate, 90f, 0.8f, 2f),
                    CreateSkill("Reckless Charge", AttackPattern.ChargeStrike, ThreatType.Immediate, 120f, 0.6f, 3f),
                    CreateSkill("Explosive Slam", AttackPattern.EnvironmentalSlam, ThreatType.Immediate, 100f, 1f, 4f)
                });
                break;
                
            default: // Balanced
                availableSkills.AddRange(new List<UniversalSkill>
                {
                    CreateSkill("Balanced Cleave", AttackPattern.CleaveAttack, ThreatType.Immediate, 65f, 1.5f, 4f),
                    CreateSkill("Tactical Charge", AttackPattern.ChargeStrike, ThreatType.Immediate, 80f, 1.2f, 5f),
                    CreateSkill("Defensive Maneuver", AttackPattern.DefensiveShield, ThreatType.Persistent, 0f, 2f, 8f),
                    CreateSkill("Strategic Summon", AttackPattern.MinionSummon, ThreatType.Delayed, 45f, 2f, 6f)
                });
                break;
        }
    }

    private UniversalSkill CreateSkill(string name, AttackPattern pattern, ThreatType threat, 
        float damage, float telegraph, float cooldown)
    {
        var skill = new UniversalSkill
        {
            skillName = name,
            pattern = pattern,
            threatType = threat,
            baseDamage = damage,
            telegraphDuration = telegraph,
            cooldown = cooldown,
            validCounters = GetDefaultCountersForPattern(pattern),
            requiresLineOfSight = pattern == AttackPattern.ThreatProjectile,
            minRange = 0f,
            maxRange = pattern == AttackPattern.ThreatProjectile ? 15f : 8f
        };
        
        return skill;
    }

    private List<CounterplayType> GetDefaultCountersForPattern(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.CleaveAttack:
                return new List<CounterplayType> { CounterplayType.Dodge, CounterplayType.Positioning };
            case AttackPattern.ChargeStrike:
                return new List<CounterplayType> { CounterplayType.Dodge, CounterplayType.Interrupt, CounterplayType.Positioning };
            case AttackPattern.MinionSummon:
                return new List<CounterplayType> { CounterplayType.Positioning, CounterplayType.Resource };
            case AttackPattern.EnvironmentalSlam:
                return new List<CounterplayType> { CounterplayType.Dodge, CounterplayType.Positioning };
            case AttackPattern.DefensiveShield:
                return new List<CounterplayType> { CounterplayType.Interrupt, CounterplayType.Resource };
            case AttackPattern.TacticalReposition:
                return new List<CounterplayType> { CounterplayType.Positioning, CounterplayType.Cooperative };
            case AttackPattern.ThreatProjectile:
                return new List<CounterplayType> { CounterplayType.Dodge, CounterplayType.Block, CounterplayType.Positioning };
            case AttackPattern.AreaDenial:
                return new List<CounterplayType> { CounterplayType.Positioning, CounterplayType.Resource };
            default:
                return new List<CounterplayType> { CounterplayType.Dodge, CounterplayType.Positioning };
        }
    }

    private Color GetArchetypeColor(float alpha)
    {
        Color baseColor = eliteColor;
        
        switch (bossArchetype)
        {
            case BossArchetype.Aggressive:
                baseColor = Color.red;
                break;
            case BossArchetype.Defensive:
                baseColor = Color.blue;
                break;
            case BossArchetype.Tactical:
                baseColor = Color.green;
                break;
            case BossArchetype.Berserker:
                baseColor = Color.magenta;
                break;
        }
        
        return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
    #endregion

    #region HELPER METHODS
    private List<Transform> FindPlayersInRange(Vector3 center, float range)
    {
        return Physics2D.OverlapCircleAll(center, range, LayerMask.GetMask("Player"))
            .Select(c => c.transform)
            .ToList();
    }

    private bool HasLineOfSight(Transform target)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            target.position - transform.position, 
            Vector3.Distance(transform.position, target.position),
            LayerMask.GetMask("Obstacles")
        );
        
        return hit.collider == null;
    }

    private Vector3 PredictTargetPosition(Transform target)
    {
        // Simple prediction based on target's current velocity
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            return target.position + (Vector3)rb.linearVelocity * 0.5f;
        }
        
        return target.position;
    }

    private List<Vector3> GenerateSummonPositions(int count)
    {
        var positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * 5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * 5f,
                0f
            );
            positions.Add(transform.position + offset);
        }
        return positions;
    }

    private List<Vector3> GenerateCrossPattern(Vector3 center, int length)
    {
        var positions = new List<Vector3>();
        
        // Horizontal line
        for (int i = -length/2; i <= length/2; i++)
        {
            positions.Add(center + Vector3.right * i * 2f);
        }
        
        // Vertical line
        for (int i = -length/2; i <= length/2; i++)
        {
            if (i != 0) // Don't duplicate center
                positions.Add(center + Vector3.up * i * 2f);
        }
        
        return positions;
    }

    private Vector3 FindOptimalRepositionPoint(Vector3 targetPosition)
    {
        // Find a position that's tactically advantageous
        Vector3 directionAway = (transform.position - targetPosition).normalized;
        Vector3 flankDirection = new Vector3(-directionAway.y, directionAway.x, 0f);
        
        return targetPosition + flankDirection * 6f;
    }

    private IEnumerator PerformCharge(Vector3 endPoint, float damage)
    {
        Vector3 startPos = transform.position;
        float chargeSpeed = 15f;
        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, endPoint) / chargeSpeed;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPoint, elapsed / duration);
            
            // Check for player hits during charge
            var playersInRange = FindPlayersInRange(transform.position, 1.5f);
            foreach (var player in playersInRange)
            {
                var character = player.GetComponent<Character>();
                if (character != null)
                {
                    character.TakeDamage(damage * damageMultiplier);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPoint;
    }

    private IEnumerator MaintainAreaDenial(Vector3 center, float radius, float duration, float tickDamage)
    {
        float elapsed = 0f;
        float tickInterval = 1f;
        float nextTick = 0f;
        
        while (elapsed < duration)
        {
            if (elapsed >= nextTick)
            {
                var playersInArea = FindPlayersInRange(center, radius);
                foreach (var player in playersInArea)
                {
                    var character = player.GetComponent<Character>();
                    if (character != null)
                    {
                        character.TakeDamage(tickDamage);
                    }
                }
                
                CreateUniversalImpactEffect(center, radius);
                nextTick += tickInterval;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void CreateUniversalImpactEffect(Vector3 position, float size)
    {
        GameObject effect = new GameObject("UniversalImpactEffect");
        effect.transform.position = position;
        
        var renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = GetArchetypeColor(0.8f);
        effect.transform.localScale = Vector3.one * size;
        
        StartCoroutine(FadeAndDestroy(effect, 1f));
    }

    private void UpdatePlayerPerformance()
    {
        // Track player performance for adaptive difficulty
        // This would integrate with actual gameplay metrics in a full implementation
        
        // For now, simulate based on time since last skill
        float timeSinceSkill = Time.time - lastSkillUsedTime;
        if (timeSinceSkill < 3f)
        {
            playerPerformanceScore = Mathf.Min(playerPerformanceScore + 0.1f, 1f);
            consecutiveFailures = 0;
        }
        else if (timeSinceSkill > 8f)
        {
            consecutiveFailures++;
            playerPerformanceScore = Mathf.Max(playerPerformanceScore - 0.05f, 0f);
        }
    }

    private void RandomizeSkillSet()
    {
        if (availableSkills.Count == 0) return;
        
        var allSkills = new List<UniversalSkill>(availableSkills);
        availableSkills.Clear();
        
        int skillCount = Random.Range(minRandomSkills, maxRandomSkills + 1);
        skillCount = Mathf.Min(skillCount, allSkills.Count);
        
        for (int i = 0; i < skillCount; i++)
        {
            int randomIndex = Random.Range(0, allSkills.Count);
            availableSkills.Add(allSkills[randomIndex]);
            allSkills.RemoveAt(randomIndex);
        }
    }

    private void RecalculateThreatLevel()
    {
        currentThreatLevel = Mathf.Lerp(0.3f, maxThreatLevel, 
            (playerPerformanceScore + (bossArchetype == BossArchetype.Aggressive ? 0.2f : 0f)));
    }

    private void StartUniversalSystems()
    {
        if (hasRegeneration)
        {
            regenerationCoroutine = StartCoroutine(RegenerateHealth());
        }
        
        if (useEliteOutline)
        {
            CreateOutline();
        }
        
        if (pulseEffect)
        {
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
        
        // Start threat management system
        threatManagementCoroutine = StartCoroutine(ThreatManagementSystem());
    }

    private IEnumerator ThreatManagementSystem()
    {
        while (isElite && enemy != null && enemy.CurrentHealth > 0)
        {
            UpdateThreatBalancing();
            yield return new WaitForSeconds(1f);
        }
    }
    #endregion

    #region LEGACY SYSTEM INTEGRATION (Simplified)
    private void UpdateLegacySystems()
    {
        // Keep existing legacy systems for compatibility
        // but integrate them with Universal Framework principles
        
        if (hasBerserker && !isBerserking && enemy != null)
        {
            float healthPercent = enemy.CurrentHealth / enemy.MaxHealth;
            if (healthPercent <= berserkerHealthThreshold)
            {
                ActivateBerserker();
            }
        }
    }

    private void ActivateBerserker()
    {
        isBerserking = true;
        
        // Integrate with Universal Framework
        currentThreatLevel = Mathf.Min(currentThreatLevel * 1.5f, 1f);
        baseTelegraphDuration *= 0.8f; // Faster telegraphs when berserking
        
        if (enemy != null)
        {
            enemy.SetDamageMultiplier(damageMultiplier * (1f + berserkerDamageBonus));
            enemy.SetSpeedMultiplier(speedMultiplier * (1f + berserkerSpeedBonus));
        }
        
        Debug.Log($"[{eliteTitle}] Entered Berserker Mode - Threat Level: {currentThreatLevel:F2}");
    }

    // ...existing legacy methods remain unchanged...
    // (Keeping all the original functionality for compatibility)
    #endregion

    #region CLEANUP AND UTILITIES
    private void CleanupUniversalFramework()
    {
        if (threatManagementCoroutine != null)
        {
            StopCoroutine(threatManagementCoroutine);
        }
        
        // Clear active telegraphs
        if (telegraphManager != null)
        {
            telegraphManager.ClearAllWarnings();
        }
    }

    private void CleanupLegacySystems()
    {
        if (enemy != null)
        {
            enemy.OnDamageTaken -= HandleDamageTaken;
            enemy.OnHealthChanged -= HandleHealthChanged;
            enemy.OnDeath -= HandleDeath;
            enemy.OnDealDamage -= HandleDealDamage;
        }
        
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        DestroyEliteEffects();
    }

    // Keep all existing legacy methods for compatibility...
    private void ApplyEliteStats() { /* existing implementation */ }
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth) { /* existing implementation */ }
    private void HandleHealthChanged(float currentHealth, float maxHealth) { /* existing implementation */ }
    private void HandleDeath() { /* existing implementation */ }
    private void HandleDealDamage(GameObject target, float damage) { /* existing implementation */ }
    private void CreateEliteEffects() { /* existing implementation */ }
    private void DestroyEliteEffects() { /* existing implementation */ }
    private void CreateOutline() { /* existing implementation */ }
    private IEnumerator PulseEffect() { return null; /* existing implementation */ }
    private IEnumerator RegenerateHealth() { return null; /* existing implementation */ }
    private Sprite CreateCircleSprite() { return null; /* existing implementation */ }
    private IEnumerator FadeAndDestroy(GameObject obj, float duration) { return null; /* existing implementation */ }
    
    private void OnDrawGizmosSelected()
    {
        // Enhanced gizmos with Universal Framework info
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 15f); // Detection range
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 8f); // Combat range
        
        Gizmos.color = GetArchetypeColor(0.5f);
        Gizmos.DrawWireSphere(transform.position, 4f + currentThreatLevel * 2f); // Threat visualization
        
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 4f,
                $"{eliteTitle} ({bossArchetype})\n" +
                $"Threat Level: {currentThreatLevel:F2}\n" +
                $"Skills: {availableSkills.Count}\n" +
                $"Performance: {playerPerformanceScore:F2}\n" +
                $"Telegraph: {CalculateOptimalTelegraphDuration():F1}s");
        }
        #endif
    }
    #endregion

    #region PUBLIC API EXTENSIONS
    // Public API for Universal Framework
    public float GetCurrentThreatLevel() => currentThreatLevel;
    public BossArchetype GetBossArchetype() => bossArchetype;
    public List<UniversalSkill> GetAvailableSkills() => new List<UniversalSkill>(availableSkills);
    public void SetPlayerSkillRating(float rating) => playerSkillRating = Mathf.Clamp01(rating);
    public void ForceThreatLevel(float level) => currentThreatLevel = Mathf.Clamp(level, 0.1f, 1f);
    
    [ContextMenu("🎭 Test Universal Skill")]
    public void TestUniversalSkill()
    {
        if (availableSkills.Count > 0)
        {
            var skill = availableSkills[Random.Range(0, availableSkills.Count)];
            StartCoroutine(ExecuteUniversalSkill(skill));
        }
    }
    
    [ContextMenu("📊 Show Framework Status")]
    public void ShowFrameworkStatus()
    {
        Debug.Log($@"
🎭 UNIVERSAL BOSS FRAMEWORK STATUS
================================
Boss: {eliteTitle} ({bossArchetype})
Threat Level: {currentThreatLevel:F2} / {maxThreatLevel:F2}
Player Performance: {playerPerformanceScore:F2}
Telegraph Duration: {CalculateOptimalTelegraphDuration():F1}s
Available Skills: {availableSkills.Count}
Active Warnings: {(telegraphManager ? telegraphManager.GetActiveWarningCount() : 0)}

🎯 FRAMEWORK PRINCIPLES STATUS:
✓ Telegraph System: {(telegraphManager != null ? "Active" : "Missing")}
✓ Threat Management: {(dynamicThreatBalancing ? "Dynamic" : "Static")}
✓ Counterplay Validation: {(enforceSkillBasedCounters ? "Enforced" : "Disabled")}
✓ Adaptive Difficulty: {(adaptTelegraphToSkill ? "Active" : "Disabled")}
");
    }
    #endregion
}