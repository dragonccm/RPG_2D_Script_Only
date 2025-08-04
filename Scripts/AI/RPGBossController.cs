using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// ?? MEGAERA-INSPIRED 2D TOP-DOWN RPG BOSS SYSTEM
/// Based on Hades boss mechanics adapted for strategic RPG combat
/// Focus: Telegraph system, strategic positioning, party-based encounters
/// </summary>
public class RPGBossController : MonoBehaviour, IDamageable
{
    #region CORE BOSS DATA
    [Header("?? BOSS IDENTITY")]
    [SerializeField] private string bossName = "Megaera";
    [SerializeField] private int bossLevel = 1;
    [SerializeField] private BossArchetype archetype = BossArchetype.Balanced;
    
    [Header("?? COMBAT STATS")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth = 1000f;
    [SerializeField] private float baseDamage = 50f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float combatRange = 8f;
    
    [Header("?? PHASE SYSTEM")]
    [SerializeField] private int totalPhases = 3;
    [SerializeField] private float[] phaseThresholds = { 0.7f, 0.4f };
    [SerializeField] private bool autoPhaseTransition = true;
    
    [Header("? TELEGRAPH SETTINGS (RPG Adapted)")]
    [SerializeField] private float baseWarningDuration = 1.5f; // 2D RPG needs longer telegraphs
    [SerializeField] private float finalWarningDuration = 0.5f;
    [SerializeField] private Color warningColor = new Color(1f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color finalWarningColor = new Color(1f, 0f, 0f, 0.8f);
    #endregion

    #region BOSS ARCHETYPES
    public enum BossArchetype
    {
        Aggressive,     // Fast attacks, short telegraphs, high damage
        Defensive,      // Shields, longer telegraphs, area denial
        Balanced,       // Mixed strategies, moderate everything
        Tactical,       // Complex patterns, intelligent AI
        Berserker       // Becomes more dangerous when low health
    }

    public enum BossState
    {
        Idle,
        Engaging,
        Combat,
        Casting,
        PhaseTransition,
        Stunned,
        Dead
    }

    public enum AttackPattern
    {
        CleaveAttack,       // Megaera's whip combo -> 3x3 area cleave
        ChargeStrike,       // Lunge attack -> Linear charge
        MinionSummon,       // Fury summon -> Strategic minion spawn
        EnvironmentalSlam,  // Arena slam -> Battlefield modification
        DefensiveShield,    // Defensive stance
        TacticalReposition  // Smart movement
    }
    #endregion

    #region PRIVATE VARIABLES
    private BossState currentState = BossState.Idle;
    private int currentPhase = 0;
    private bool isPhaseTransitioning = false;
    
    // Components
    private NavMeshAgent agent;
    private Character character;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    
    // Combat variables
    private Transform currentTarget;
    private float lastAttackTime = 0f;
    private float lastSkillTime = 0f;
    private List<Transform> detectedPlayers = new List<Transform>();
    
    // Telegraph system
    private TelegraphManager telegraphManager;
    private List<GameObject> activeTelegraphs = new List<GameObject>();
    
    // Attack patterns
    private List<AttackPattern> availablePatterns = new List<AttackPattern>();
    private AttackPattern lastUsedPattern;
    private float patternCooldown = 3f;
    
    // Performance optimization
    private float nextUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.1f;
    #endregion

    #region UNITY LIFECYCLE
    private void Awake()
    {
        InitializeComponents();
        InitializeTelegraphSystem();
    }

    private void Start()
    {
        SetupBossArchetype();
        InitializePhaseSystem();
        ChangeState(BossState.Idle);
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateBossLogic();
            nextUpdateTime = Time.time + UPDATE_INTERVAL;
        }
    }
    #endregion

    #region INITIALIZATION
    private void InitializeComponents()
    {
        // Get or add required components
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        
        character = GetComponent<Character>();
        if (character == null) character = gameObject.AddComponent<Character>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
        
        // Setup NavAgent for 2D
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = combatRange * 0.8f;
        }
        
        // Setup health
        if (character != null)
        {
            character.MaxHealth = maxHealth;
            character.CurrentHealth = maxHealth;
            currentHealth = maxHealth;
        }
    }

    private void InitializeTelegraphSystem()
    {
        telegraphManager = GetComponent<TelegraphManager>();
        if (telegraphManager == null)
        {
            telegraphManager = gameObject.AddComponent<TelegraphManager>();
        }
        
        telegraphManager.Initialize(baseWarningDuration, finalWarningDuration, warningColor, finalWarningColor);
    }

    private void SetupBossArchetype()
    {
        switch (archetype)
        {
            case BossArchetype.Aggressive:
                SetupAggressiveArchetype();
                break;
            case BossArchetype.Defensive:
                SetupDefensiveArchetype();
                break;
            case BossArchetype.Tactical:
                SetupTacticalArchetype();
                break;
            case BossArchetype.Berserker:
                SetupBerserkerArchetype();
                break;
            default:
                SetupBalancedArchetype();
                break;
        }
    }

    private void SetupAggressiveArchetype()
    {
        baseWarningDuration = 1.0f; // Shorter telegraphs
        moveSpeed *= 1.2f;
        baseDamage *= 1.3f;
        patternCooldown = 2f;
        
        availablePatterns.AddRange(new[] { 
            AttackPattern.CleaveAttack, 
            AttackPattern.ChargeStrike,
            AttackPattern.CleaveAttack // Favor aggressive patterns
        });
    }

    private void SetupDefensiveArchetype()
    {
        baseWarningDuration = 2.0f; // Longer telegraphs
        combatRange *= 1.5f;
        patternCooldown = 4f;
        
        availablePatterns.AddRange(new[] { 
            AttackPattern.DefensiveShield,
            AttackPattern.EnvironmentalSlam,
            AttackPattern.MinionSummon
        });
    }

    private void SetupTacticalArchetype()
    {
        baseWarningDuration = 1.5f;
        patternCooldown = 3f;
        
        // All patterns available for maximum variety
        availablePatterns.AddRange(System.Enum.GetValues(typeof(AttackPattern)).Cast<AttackPattern>());
    }

    private void SetupBerserkerArchetype()
    {
        baseWarningDuration = 1.2f;
        patternCooldown = 2.5f;
        
        availablePatterns.AddRange(new[] { 
            AttackPattern.CleaveAttack,
            AttackPattern.ChargeStrike,
            AttackPattern.EnvironmentalSlam
        });
    }

    private void SetupBalancedArchetype()
    {
        // Default values are already balanced
        availablePatterns.AddRange(new[] { 
            AttackPattern.CleaveAttack,
            AttackPattern.ChargeStrike,
            AttackPattern.MinionSummon,
            AttackPattern.EnvironmentalSlam
        });
    }

    private void InitializePhaseSystem()
    {
        if (phaseThresholds.Length != totalPhases - 1)
        {
            phaseThresholds = new float[totalPhases - 1];
            for (int i = 0; i < phaseThresholds.Length; i++)
            {
                phaseThresholds[i] = 1f - ((float)(i + 1) / totalPhases);
            }
        }
    }
    #endregion

    #region MAIN UPDATE LOGIC
    private void UpdateBossLogic()
    {
        if (currentHealth <= 0)
        {
            ChangeState(BossState.Dead);
            return;
        }

        UpdateTargeting();
        UpdatePhaseTransitions();
        ExecuteCurrentState();
    }

    private void UpdateTargeting()
    {
        detectedPlayers.Clear();
        
        var players = GameObject.FindGameObjectsWithTag("Player")
            .Where(p => Vector3.Distance(transform.position, p.transform.position) <= detectionRange)
            .Select(p => p.transform);
        
        detectedPlayers.AddRange(players);
        currentTarget = SelectBestTarget();
    }

    private Transform SelectBestTarget()
    {
        if (detectedPlayers.Count == 0) return null;
        
        // Strategic target selection based on archetype
        switch (archetype)
        {
            case BossArchetype.Aggressive:
                return SelectClosestTarget();
            case BossArchetype.Tactical:
                return SelectWeakestTarget();
            default:
                return SelectClosestTarget();
        }
    }

    private Transform SelectClosestTarget()
    {
        return detectedPlayers
            .OrderBy(p => Vector3.Distance(transform.position, p.position))
            .FirstOrDefault();
    }

    private Transform SelectWeakestTarget()
    {
        Transform weakest = null;
        float lowestHealth = float.MaxValue;
        
        foreach (var player in detectedPlayers)
        {
            var playerCharacter = player.GetComponent<Character>();
            if (playerCharacter != null && playerCharacter.CurrentHealth < lowestHealth)
            {
                lowestHealth = playerCharacter.CurrentHealth;
                weakest = player;
            }
        }
        
        return weakest ?? SelectClosestTarget();
    }

    private void UpdatePhaseTransitions()
    {
        if (!autoPhaseTransition || isPhaseTransitioning) return;
        
        float healthPercent = currentHealth / maxHealth;
        
        for (int i = currentPhase; i < phaseThresholds.Length; i++)
        {
            if (healthPercent <= phaseThresholds[i])
            {
                StartPhaseTransition(i + 1);
                break;
            }
        }
    }
    #endregion

    #region STATE MACHINE
    private void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    private void EnterState(BossState state)
    {
        switch (state)
        {
            case BossState.Idle:
                if (agent != null) agent.isStopped = true;
                break;
            case BossState.Engaging:
                if (agent != null) agent.isStopped = false;
                break;
            case BossState.Combat:
                lastAttackTime = Time.time;
                break;
            case BossState.Dead:
                HandleDeath();
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case BossState.Idle:
                ExecuteIdleState();
                break;
            case BossState.Engaging:
                ExecuteEngagingState();
                break;
            case BossState.Combat:
                ExecuteCombatState();
                break;
            case BossState.Casting:
                ExecuteCastingState();
                break;
            case BossState.PhaseTransition:
                // Handled by coroutine
                break;
        }
    }

    private void ExitState(BossState state)
    {
        // Cleanup when exiting states
    }

    private void ExecuteIdleState()
    {
        if (currentTarget != null)
        {
            ChangeState(BossState.Engaging);
        }
    }

    private void ExecuteEngagingState()
    {
        if (currentTarget == null)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToTarget <= combatRange)
        {
            ChangeState(BossState.Combat);
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private void ExecuteCombatState()
    {
        if (currentTarget == null)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToTarget > combatRange * 1.5f)
        {
            ChangeState(BossState.Engaging);
            return;
        }

        // Stop movement for combat
        if (agent != null) agent.isStopped = true;
        
        // Face target
        FaceTarget(currentTarget);
        
        // Attack logic
        if (CanUseAttackPattern())
        {
            StartAttackPattern();
        }
    }

    private void ExecuteCastingState()
    {
        // Handled by attack pattern coroutines
    }
    #endregion

    #region MOVEMENT & TARGETING
    private void MoveTowardsTarget()
    {
        if (agent != null && currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
    }

    private void FaceTarget(Transform target)
    {
        if (target == null) return;
        
        Vector3 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }
    #endregion

    #region ATTACK PATTERN SYSTEM
    private bool CanUseAttackPattern()
    {
        return Time.time >= lastSkillTime + patternCooldown;
    }

    private void StartAttackPattern()
    {
        var pattern = SelectAttackPattern();
        StartCoroutine(ExecuteAttackPattern(pattern));
        lastSkillTime = Time.time;
    }

    private AttackPattern SelectAttackPattern()
    {
        // Intelligent pattern selection
        var availableNow = availablePatterns.Where(p => p != lastUsedPattern).ToList();
        if (availableNow.Count == 0) availableNow = availablePatterns;
        
        var selected = availableNow[Random.Range(0, availableNow.Count)];
        lastUsedPattern = selected;
        return selected;
    }

    private IEnumerator ExecuteAttackPattern(AttackPattern pattern)
    {
        ChangeState(BossState.Casting);
        
        switch (pattern)
        {
            case AttackPattern.CleaveAttack:
                yield return StartCoroutine(ExecuteCleaveAttack());
                break;
            case AttackPattern.ChargeStrike:
                yield return StartCoroutine(ExecuteChargeStrike());
                break;
            case AttackPattern.MinionSummon:
                yield return StartCoroutine(ExecuteMinionSummon());
                break;
            case AttackPattern.EnvironmentalSlam:
                yield return StartCoroutine(ExecuteEnvironmentalSlam());
                break;
            case AttackPattern.DefensiveShield:
                yield return StartCoroutine(ExecuteDefensiveShield());
                break;
            case AttackPattern.TacticalReposition:
                yield return StartCoroutine(ExecuteTacticalReposition());
                break;
        }
        
        ChangeState(BossState.Combat);
    }
    #endregion

    #region MEGAERA-INSPIRED ATTACK PATTERNS

    /// <summary>
    /// CLEAVE ATTACK - Adaptation of Megaera's whip combo
    /// 2D Implementation: 3x3 grid area denial around boss
    /// </summary>
    private IEnumerator ExecuteCleaveAttack()
    {
        Debug.Log($"[{bossName}] Executing Cleave Attack");
        
        // Phase 1: Threat Assessment (0.5s)
        Vector3 attackCenter = transform.position;
        float attackRange = 3f;
        
        // Show subtle warning
        var warningEffect = telegraphManager.CreateCircleWarning(attackCenter, attackRange, 
            new Color(1f, 1f, 0f, 0.3f), 0.5f);
        
        yield return new WaitForSeconds(0.5f);
        
        // Phase 2: Warning Phase (1.0s)
        if (warningEffect != null) Destroy(warningEffect);
        
        var mainWarning = telegraphManager.CreateCircleWarning(attackCenter, attackRange, 
            warningColor, baseWarningDuration);
        
        yield return new WaitForSeconds(baseWarningDuration);
        
        // Phase 3: Final Warning (0.5s)
        if (mainWarning != null)
        {
            var renderer = mainWarning.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = finalWarningColor;
        }
        
        yield return new WaitForSeconds(finalWarningDuration);
        
        // Phase 4: Execution
        if (mainWarning != null) Destroy(mainWarning);
        
        // Deal damage to all players in range
        foreach (var player in detectedPlayers)
        {
            float distance = Vector3.Distance(attackCenter, player.position);
            if (distance <= attackRange)
            {
                var playerCharacter = player.GetComponent<Character>();
                if (playerCharacter != null)
                {
                    playerCharacter.TakeDamage(baseDamage * 1.5f);
                }
            }
        }
        
        // Visual effect
        CreateImpactEffect(attackCenter, attackRange);
    }

    /// <summary>
    /// CHARGE STRIKE - Adaptation of Megaera's lunge attack
    /// 2D Implementation: Linear skillshot with prediction
    /// </summary>
    private IEnumerator ExecuteChargeStrike()
    {
        Debug.Log($"[{bossName}] Executing Charge Strike");
        
        if (currentTarget == null) yield break;
        
        // Calculate charge direction
        Vector3 chargeDirection = (currentTarget.position - transform.position).normalized;
        float chargeDistance = 8f;
        Vector3 chargeEndPoint = transform.position + chargeDirection * chargeDistance;
        
        // Phase 1-2: Warning
        var lineWarning = telegraphManager.CreateLineWarning(transform.position, chargeEndPoint, 
            1f, warningColor, baseWarningDuration);
        
        yield return new WaitForSeconds(baseWarningDuration);
        
        // Phase 3: Final Warning
        if (lineWarning != null)
        {
            var renderer = lineWarning.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = finalWarningColor;
        }
        
        yield return new WaitForSeconds(finalWarningDuration);
        
        // Phase 4: Execution - Charge forward
        if (lineWarning != null) Destroy(lineWarning);
        
        Vector3 startPos = transform.position;
        float chargeSpeed = 15f;
        float elapsed = 0f;
        float chargeDuration = chargeDistance / chargeSpeed;
        
        while (elapsed < chargeDuration)
        {
            transform.position = Vector3.Lerp(startPos, chargeEndPoint, elapsed / chargeDuration);
            
            // Check for player hits during charge
            foreach (var player in detectedPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= 1.5f)
                {
                    var playerCharacter = player.GetComponent<Character>();
                    if (playerCharacter != null)
                    {
                        playerCharacter.TakeDamage(baseDamage * 2f);
                        // Knockback effect could be added here
                    }
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = chargeEndPoint;
    }

    /// <summary>
    /// MINION SUMMON - Adaptation of Megaera's fury summon
    /// 2D Implementation: Strategic summons with clear telegraphs
    /// </summary>
    private IEnumerator ExecuteMinionSummon()
    {
        Debug.Log($"[{bossName}] Executing Minion Summon");
        
        int summonCount = 2 + currentPhase;
        List<Vector3> summonPositions = new List<Vector3>();
        
        // Generate summon positions around boss
        for (int i = 0; i < summonCount; i++)
        {
            float angle = (360f / summonCount) * i;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * 5f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * 5f,
                0f
            );
            summonPositions.Add(transform.position + offset);
        }
        
        // Show spawn warnings
        var spawnWarnings = new List<GameObject>();
        foreach (var pos in summonPositions)
        {
            var warning = telegraphManager.CreateCircleWarning(pos, 1f, 
                new Color(0.5f, 0f, 1f, 0.6f), baseWarningDuration + 0.5f);
            spawnWarnings.Add(warning);
        }
        
        yield return new WaitForSeconds(baseWarningDuration + 0.5f);
        
        // Clean up warnings and spawn minions
        foreach (var warning in spawnWarnings)
        {
            if (warning != null) Destroy(warning);
        }
        
        // TODO: Spawn actual minion prefabs here
        // For now, just create impact effects
        foreach (var pos in summonPositions)
        {
            CreateImpactEffect(pos, 1f);
        }
    }

    /// <summary>
    /// ENVIRONMENTAL SLAM - Adaptation of Megaera's arena slam
    /// 2D Implementation: Dynamic battlefield modification
    /// </summary>
    private IEnumerator ExecuteEnvironmentalSlam()
    {
        Debug.Log($"[{bossName}] Executing Environmental Slam");
        
        // Create cross-shaped danger zones
        var hazardPositions = new List<Vector3>();
        Vector3 center = transform.position;
        
        // Horizontal line
        for (int i = -3; i <= 3; i++)
        {
            hazardPositions.Add(center + Vector3.right * i * 2f);
        }
        
        // Vertical line
        for (int i = -3; i <= 3; i++)
        {
            if (i != 0) // Don't duplicate center
                hazardPositions.Add(center + Vector3.up * i * 2f);
        }
        
        // Show warnings
        var hazardWarnings = new List<GameObject>();
        foreach (var pos in hazardPositions)
        {
            var warning = telegraphManager.CreateSquareWarning(pos, 1.5f, 
                warningColor, baseWarningDuration);
            hazardWarnings.Add(warning);
        }
        
        yield return new WaitForSeconds(baseWarningDuration);
        
        // Final warning phase
        foreach (var warning in hazardWarnings)
        {
            if (warning != null)
            {
                var renderer = warning.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = finalWarningColor;
            }
        }
        
        yield return new WaitForSeconds(finalWarningDuration);
        
        // Execute slam
        foreach (var warning in hazardWarnings)
        {
            if (warning != null) Destroy(warning);
        }
        
        // Deal damage and create persistent hazards
        foreach (var pos in hazardPositions)
        {
            // Check for player hits
            foreach (var player in detectedPlayers)
            {
                float distance = Vector3.Distance(pos, player.position);
                if (distance <= 1.5f)
                {
                    var playerCharacter = player.GetComponent<Character>();
                    if (playerCharacter != null)
                    {
                        playerCharacter.TakeDamage(baseDamage);
                    }
                }
            }
            
            CreateImpactEffect(pos, 1.5f);
            // TODO: Create persistent hazard that damages players over time
        }
    }

    /// <summary>
    /// DEFENSIVE SHIELD - Boss becomes temporarily invulnerable
    /// </summary>
    private IEnumerator ExecuteDefensiveShield()
    {
        Debug.Log($"[{bossName}] Executing Defensive Shield");
        
        // Visual indicator
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.cyan;
            
            // TODO: Add invulnerability for 3 seconds
            yield return new WaitForSeconds(3f);
            
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }
    }

    /// <summary>
    /// TACTICAL REPOSITION - Smart movement for better positioning
    /// </summary>
    private IEnumerator ExecuteTacticalReposition()
    {
        Debug.Log($"[{bossName}] Executing Tactical Reposition");
        
        if (currentTarget == null) yield break;
        
        // Find optimal position (flanking the target)
        Vector3 targetPos = currentTarget.position;
        Vector3 flankPosition = targetPos + (Vector3)Random.insideUnitCircle.normalized * combatRange;
        
        // Quick teleport or fast movement
        if (agent != null)
        {
            agent.Warp(flankPosition);
        }
        else
        {
            transform.position = flankPosition;
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    #endregion

    #region TELEGRAPH SYSTEM HELPERS
    private void CreateImpactEffect(Vector3 position, float size)
    {
        // Create a simple impact effect
        GameObject effect = new GameObject("ImpactEffect");
        effect.transform.position = position;
        
        var renderer = effect.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = Color.red;
        effect.transform.localScale = Vector3.one * size;
        
        // Fade out and destroy
        StartCoroutine(FadeAndDestroy(effect, 1f));
    }

    private IEnumerator FadeAndDestroy(GameObject obj, float duration)
    {
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        Color startColor = renderer.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(obj);
    }

    private Sprite CreateCircleSprite()
    {
        // Create a simple circle sprite
        Texture2D texture = new Texture2D(64, 64);
        Vector2 center = new Vector2(32, 32);
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color color = distance <= 30 ? Color.white : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    #endregion

    #region PHASE SYSTEM
    private void StartPhaseTransition(int newPhase)
    {
        if (isPhaseTransitioning || newPhase >= totalPhases) return;
        
        currentPhase = newPhase;
        isPhaseTransitioning = true;
        
        StartCoroutine(PhaseTransitionCoroutine());
    }

    private IEnumerator PhaseTransitionCoroutine()
    {
        ChangeState(BossState.PhaseTransition);
        
        Debug.Log($"[{bossName}] Transitioning to Phase {currentPhase + 1}");
        
        // Visual effects
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            
            // Flash effect
            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.yellow;
                yield return new WaitForSeconds(0.2f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        // Apply phase bonuses
        ApplyPhaseBonus();
        
        isPhaseTransitioning = false;
        ChangeState(BossState.Combat);
    }

    private void ApplyPhaseBonus()
    {
        // Increase power based on phase
        float phaseMultiplier = 1f + (currentPhase * 0.2f);
        baseDamage *= phaseMultiplier;
        
        // Reduce cooldowns
        patternCooldown *= 0.9f;
        
        // Archetype-specific bonuses
        switch (archetype)
        {
            case BossArchetype.Berserker:
                moveSpeed *= 1.1f;
                baseWarningDuration *= 0.9f;
                break;
            case BossArchetype.Tactical:
                // Unlock new patterns
                if (currentPhase >= 1 && !availablePatterns.Contains(AttackPattern.TacticalReposition))
                {
                    availablePatterns.Add(AttackPattern.TacticalReposition);
                }
                break;
        }
    }
    #endregion

    #region DAMAGE INTERFACE
    public void TakeDamage(float damage)
    {
        if (currentState == BossState.Dead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (character != null)
        {
            character.CurrentHealth = currentHealth;
        }
        
        // Visual feedback
        if (spriteRenderer != null && currentState != BossState.PhaseTransition)
        {
            StartCoroutine(DamageFlash());
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }
    #endregion

    #region DEATH & CLEANUP
    private void HandleDeath()
    {
        Debug.Log($"[{bossName}] has been defeated!");
        
        // Stop all movement
        if (agent != null) agent.isStopped = true;
        
        // Clear active telegraphs
        foreach (var telegraph in activeTelegraphs)
        {
            if (telegraph != null) Destroy(telegraph);
        }
        activeTelegraphs.Clear();
        
        // Death animation/effects
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Fade out
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            float elapsed = 0f;
            float duration = 2f;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // TODO: Drop loot, trigger victory events, etc.
        
        Destroy(gameObject);
    }
    #endregion

    #region PUBLIC API & DEBUG
    public BossState GetCurrentState() => currentState;
    public int GetCurrentPhase() => currentPhase;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public BossArchetype GetArchetype() => archetype;

    [ContextMenu("?? Force Phase Transition")]
    public void ForcePhaseTransition()
    {
        if (currentPhase < totalPhases - 1)
        {
            StartPhaseTransition(currentPhase + 1);
        }
    }

    [ContextMenu("?? Force Attack Pattern")]
    public void ForceAttackPattern()
    {
        if (currentState == BossState.Combat)
        {
            StartAttackPattern();
        }
    }

    [ContextMenu("?? Force Death")]
    public void ForceDeath()
    {
        currentHealth = 0;
        ChangeState(BossState.Dead);
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Combat range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, combatRange);
        
        // Target line
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"{bossName} (Lv.{bossLevel})\n" +
                $"State: {currentState}\n" +
                $"Phase: {currentPhase + 1}/{totalPhases}\n" +
                $"Health: {currentHealth:F0}/{maxHealth:F0}\n" +
                $"Archetype: {archetype}");
        }
        #endif
    }
    #endregion
}