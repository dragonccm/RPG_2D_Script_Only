using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ?? UNIFIED CORE ENEMY SYSTEM
/// File chính th?ng nh?t t?t c? enemy logic
/// Tích h?p: AI, Patrol, Target Detection, Combat, Movement Direction
/// CORE c?a h? th?ng - ??m b?o tính nh?t quán
/// </summary>
[RequireComponent(typeof(Character), typeof(NavMeshAgent), typeof(Rigidbody2D))]
public class CoreEnemy : MonoBehaviour, IDamageable
{
    [Header("?? CORE SETTINGS")]
    public float detectionRange = 10f;
    public float chaseRange = 15f;
    public float attackRange = 3f;
    public float baseDamage = 25f;
    public float attackCooldown = 1.5f;
    public LayerMask playerLayerMask = 1 << 7;
    
    [Header("?? PATROL SETTINGS")]
    public PatrolMode patrolMode = PatrolMode.None;
    public Transform anchor;
    public Transform[] patrolPoints;
    public float randomRadius = 5f;
    public float arriveThreshold = 1f;
    
    [Header("?? VISUAL")]
    public SpriteRenderer spriteRenderer;
    public bool flipWithMovement = true;
    
    [Header("?? ADVANCED")]
    [SerializeField] private bool enablePerformanceMode = true;
    [SerializeField] private float updateInterval = 0.2f;
    
    public enum PatrolMode { None, Loop, PingPong, RandomAroundAnchor }
    public enum EnemyState { Idle, Patrol, Chase, Attack, Dead }
    
    // Components - UNIFIED ACCESS
    protected Character character;
    private NavMeshAgent agent;
    private EnemyType enemyType;
    private SpecialMovement specialMovement;
    private EnemySkillManager skillManager;
    
    // Multipliers for stat modifications
    private float damageMultiplier = 1f;
    private float speedMultiplier = 1f;
    private float healthMultiplier = 1f;
    
    // State & targeting - CORE LOGIC
    private EnemyState currentState = EnemyState.Idle;
    private Transform currentTarget;
    private float lastAttackTime = -999f;
    
    // Patrol system - UNIFIED PATROL
    private int currentPatrolIndex = 0;
    private bool patrolForward = true;
    private Vector3 randomPatrolTarget;
    private bool hasRandomTarget = false;
    
    // Performance optimization - THROTTLED UPDATES
    private float nextTargetUpdate = 0f;
    private float nextStateUpdate = 0f;
    
    // Events - UNIFIED EVENT SYSTEM
    public System.Action<Transform> OnTargetChanged;
    public System.Action<EnemyState> OnStateChanged;
    public System.Action OnDeath;
    
    #region INITIALIZATION
    
    private void Awake()
    {
        InitializeComponents();
        InitializeEvents();
    }
    
    private void Start()
    {
        SetupEnemy();
    }
    
    private void InitializeComponents()
    {
        character = GetComponent<Character>();
        agent = GetComponent<NavMeshAgent>();
        enemyType = GetComponent<EnemyType>();
        specialMovement = GetComponent<SpecialMovement>();
        skillManager = GetComponent<EnemySkillManager>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Setup NavMeshAgent for 2D - UNIFIED SETUP
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.stoppingDistance = attackRange * 0.8f;
        }
        
        // Ensure proper tag - CONSISTENCY
        if (!gameObject.CompareTag("Enemy"))
            gameObject.tag = "Enemy";
    }
    
    private void InitializeEvents()
    {
        // Connect character death event
        if (character != null)
        {
            character.OnDeath += HandleDeath;
        }
    }
    
    private void SetupEnemy()
    {
        // Auto-setup based on EnemyType if available
        if (enemyType != null)
        {
            enemyType.ApplyStatsToEnemy(this);
        }
        
        ChangeState(patrolMode != PatrolMode.None ? EnemyState.Patrol : EnemyState.Idle);
    }
    
    #endregion
    
    #region UPDATE LOOP - UNIFIED UPDATE
    
    private void Update()
    {
        if (character.CurrentHealth <= 0)
        {
            if (currentState != EnemyState.Dead)
                ChangeState(EnemyState.Dead);
            return;
        }
        
        // Performance mode - throttled updates
        if (enablePerformanceMode)
        {
            if (Time.time >= nextTargetUpdate)
            {
                UpdateTarget();
                nextTargetUpdate = Time.time + updateInterval;
            }
            
            if (Time.time >= nextStateUpdate)
            {
                UpdateState();
                nextStateUpdate = Time.time + updateInterval;
            }
        }
        else
        {
            UpdateTarget();
            UpdateState();
        }
        
        // Always update movement direction for sprite flipping
        UpdateMovementDirection();
    }
    
    #endregion
    
    #region TARGET SYSTEM - UNIFIED TARGETING
    
    private void UpdateTarget()
    {
        Transform bestTarget = FindBestTarget();
        
        if (bestTarget != currentTarget)
        {
            SetCurrentTarget(bestTarget);
        }
    }
    
    private Transform FindBestTarget()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > detectionRange) return null;
        
        var playerCharacter = player.GetComponent<Character>();
        if (playerCharacter != null && playerCharacter.CurrentHealth <= 0) return null;
        
        return player.transform;
    }
    
    private void SetCurrentTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        OnTargetChanged?.Invoke(newTarget);
        
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distance <= attackRange && CanAttack())
            {
                ChangeState(EnemyState.Attack);
            }
            else if (distance <= chaseRange)
            {
                ChangeState(EnemyState.Chase);
            }
        }
        else
        {
            // No target, return to patrol or idle
            ChangeState(patrolMode != PatrolMode.None ? EnemyState.Patrol : EnemyState.Idle);
        }
    }
    
    #endregion
    
    #region STATE SYSTEM - UNIFIED STATE MACHINE
    
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
        
        OnStateChanged?.Invoke(newState);
    }
    
    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Dead:
                StopMovement();
                break;
            case EnemyState.Chase:
                if (currentTarget != null)
                    MoveTowards(currentTarget.position);
                break;
        }
    }
    
    private void ExitState(EnemyState state)
    {
        // Cleanup when exiting states
    }
    
    private void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Chase:
                HandleChaseState();
                break;
            case EnemyState.Attack:
                HandleAttackState();
                break;
            case EnemyState.Dead:
                HandleDeadState();
                break;
        }
    }
    
    private void HandleIdleState()
    {
        if (currentTarget != null)
        {
            ChangeState(EnemyState.Chase);
        }
        else if (patrolMode != PatrolMode.None)
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    private void HandlePatrolState()
    {
        if (currentTarget != null)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        UpdatePatrolLogic();
    }
    
    private void HandleChaseState()
    {
        if (currentTarget == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distance > chaseRange)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (distance <= attackRange && CanAttack())
        {
            ChangeState(EnemyState.Attack);
        }
        else
        {
            MoveTowards(currentTarget.position);
        }
    }
    
    private void HandleAttackState()
    {
        if (currentTarget == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distance > attackRange * 1.2f)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        if (CanAttack())
        {
            PerformAttack();
        }
        
        FaceTarget(currentTarget);
    }
    
    private void HandleDeadState()
    {
        // Death animation/effects here
        OnDeath?.Invoke();
        Destroy(gameObject, 2f);
    }
    
    private void HandleDeath()
    {
        ChangeState(EnemyState.Dead);
    }
    
    #endregion
    
    #region PATROL SYSTEM - UNIFIED PATROL
    
    private void UpdatePatrolLogic()
    {
        switch (patrolMode)
        {
            case PatrolMode.Loop:
            case PatrolMode.PingPong:
                HandleWaypointPatrol();
                break;
            case PatrolMode.RandomAroundAnchor:
                HandleRandomPatrol();
                break;
        }
    }
    
    private void HandleWaypointPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        if (currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;
        
        Transform currentWaypoint = patrolPoints[currentPatrolIndex];
        if (currentWaypoint == null) return;
        
        float distance = Vector3.Distance(transform.position, currentWaypoint.position);
        
        if (distance <= arriveThreshold)
        {
            AdvancePatrolIndex();
        }
        else
        {
            MoveTowards(currentWaypoint.position);
        }
    }
    
    private void AdvancePatrolIndex()
    {
        switch (patrolMode)
        {
            case PatrolMode.Loop:
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                break;
            case PatrolMode.PingPong:
                if (patrolForward)
                {
                    if (currentPatrolIndex < patrolPoints.Length - 1)
                        currentPatrolIndex++;
                    else
                        patrolForward = false;
                }
                else
                {
                    if (currentPatrolIndex > 0)
                        currentPatrolIndex--;
                    else
                        patrolForward = true;
                }
                break;
        }
    }
    
    private void HandleRandomPatrol()
    {
        if (anchor == null) return;
        
        if (!hasRandomTarget || Vector3.Distance(transform.position, randomPatrolTarget) <= arriveThreshold)
        {
            Vector2 randomOffset = Random.insideUnitCircle * randomRadius;
            randomPatrolTarget = anchor.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            hasRandomTarget = true;
        }
        
        MoveTowards(randomPatrolTarget);
    }
    
    #endregion
    
    #region MOVEMENT SYSTEM - UNIFIED MOVEMENT
    
    private void MoveTowards(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }
    
    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }
    
    private void UpdateMovementDirection()
    {
        if (!flipWithMovement || spriteRenderer == null || agent == null) return;
        
        // Flip sprite based on movement direction
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            if (agent.velocity.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (agent.velocity.x < -0.01f)
                spriteRenderer.flipX = true;
        }
    }
    
    private void FaceTarget(Transform target)
    {
        if (target == null || spriteRenderer == null) return;
        
        float direction = target.position.x - transform.position.x;
        if (Mathf.Abs(direction) > 0.1f)
        {
            spriteRenderer.flipX = direction < 0f;
        }
    }
    
    #endregion
    
    #region COMBAT SYSTEM - UNIFIED COMBAT
    
    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    private void PerformAttack()
    {
        if (currentTarget == null) return;
        
        lastAttackTime = Time.time;
        
        // Try skill system first - PRIORITIZE SKILLS
        if (skillManager != null && skillManager.CanUseSkill())
        {
            skillManager.UseSkill();
            return;
        }
        
        // Fallback to basic attack
        var targetCharacter = currentTarget.GetComponent<Character>();
        if (targetCharacter != null)
        {
            float actualDamage = baseDamage * damageMultiplier;
            targetCharacter.TakeDamage(actualDamage);
        }
    }
    
    public void TakeDamage(float damage)
    {
        character?.TakeDamage(damage);
    }
    
    #endregion
    
    #region STAT MULTIPLIERS - FOR ELITE SYSTEM
    
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
        if (skillManager != null)
        {
            skillManager.attackDamage = baseDamage * damageMultiplier;
        }
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
        if (agent != null)
        {
            agent.speed *= speedMultiplier;
        }
    }
    
    public void SetHealthMultiplier(float multiplier)
    {
        healthMultiplier = multiplier;
        if (character != null)
        {
            float oldMax = character.MaxHealth;
            character.MaxHealth = oldMax * healthMultiplier;
            character.CurrentHealth = character.MaxHealth; // Full health with new multiplier
        }
    }
    
    public float GetDamageMultiplier() => damageMultiplier;
    public float GetSpeedMultiplier() => speedMultiplier;
    public float GetHealthMultiplier() => healthMultiplier;
    
    #endregion
    
    #region PUBLIC API - UNIFIED INTERFACE
    
    // State queries
    public EnemyState GetCurrentState() => currentState;
    public Transform GetCurrentTarget() => currentTarget;
    public bool IsAlive => character != null && character.CurrentHealth > 0;
    public float GetHealthPercent() => character != null ? character.CurrentHealth / character.MaxHealth : 0f;
    
    // Configuration methods
    public void SetupPatrol(PatrolMode mode, Transform anchorPoint, Transform[] waypoints = null, float radius = 5f)
    {
        patrolMode = mode;
        anchor = anchorPoint;
        patrolPoints = waypoints;
        randomRadius = radius;
    }
    
    public void SetStats(float health, float damage, float attackRng, float detectionRng)
    {
        if (character != null)
        {
            character.MaxHealth = health * healthMultiplier;
            character.CurrentHealth = character.MaxHealth;
        }
        baseDamage = damage;
        attackRange = attackRng;
        detectionRange = detectionRng;
        chaseRange = detectionRng * 1.5f;
    }
    
    // Force target (for group systems)
    public void ForceTarget(Transform target)
    {
        SetCurrentTarget(target);
    }
    
    // Force state (for group systems)
    public void ForceState(EnemyState state)
    {
        ChangeState(state);
    }
    
    #endregion
    
    #region COMPONENT ACCESS - UNIFIED ACCESS
    
    public Character Character => character;
    public NavMeshAgent Agent => agent;
    public EnemyType EnemyType => enemyType;
    public SpecialMovement SpecialMovement => specialMovement;
    public EnemySkillManager SkillManager => skillManager;
    
    #endregion
}

// UNIFIED INTERFACES
public interface IDamageable
{
    void TakeDamage(float damage);
}

// LEGACY COMPATIBILITY - Enemy alias
public class Enemy : CoreEnemy
{
    // Legacy properties for backward compatibility
    public float _currentHealth => Character != null ? Character.CurrentHealth : 0f;
    public float _maxHealth => Character != null ? Character.MaxHealth : 100f;  
    
    // Legacy events
    public System.Action<float, float> OnHealthChanged;
    public System.Action<Enemy, float, float> OnDamageTaken;
    public System.Action<GameObject, float> OnDealDamage;
    
    // Legacy properties
    public float CurrentHealth => Character != null ? Character.CurrentHealth : 0f;
    public float MaxHealth => Character != null ? Character.MaxHealth : 100f;
    public bool IsDead => !IsAlive;
    
    // Legacy compatibility properties
    public float randomPatrolRadius 
    {
        get => randomRadius;
        set => randomRadius = value;
    }
    
    public new System.Collections.Generic.List<Transform> patrolPoints
    {
        get => new System.Collections.Generic.List<Transform>(base.patrolPoints ?? new Transform[0]);
        set => base.patrolPoints = value?.ToArray();
    }
    
    public EnemyAnimatorController EnemyAnimatorController => GetComponent<EnemyAnimatorController>();
    
    // Legacy event triggers
    public void TriggerDealDamageEvent(GameObject target, float damage)
    {
        OnDealDamage?.Invoke(target, damage);
    }
}