using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Legacy EnemyAIController - Bridge for old systems
/// Provides compatibility while using new CoreEnemy system
/// </summary>
public class EnemyAIController : MonoBehaviour
{
    // Legacy properties for compatibility
    public StateMachine stateMachine;
    public IdleState idleState;
    public ChaseState chaseState;
    public AttackState attackState;
    public State patrolState;
    public DeadState deadState;
    
    public EnemyAnimatorController animatorController;
    public NavMeshAgent agent;
    public Transform playerTarget;
    
    // Bridge to new system
    private CoreEnemy coreEnemy;
    
    // Add enemyType property for legacy compatibility
    public EnemyType.Type enemyType 
    {
        get 
        {
            var typeComponent = GetComponent<EnemyType>();
            return typeComponent != null ? typeComponent.enemyType : EnemyType.Type.Melee;
        }
    }
    
    protected virtual void Awake()
    {
        // Get new system reference
        coreEnemy = GetComponent<CoreEnemy>();
        
        // Initialize legacy components
        stateMachine = new StateMachine();
        animatorController = GetComponent<EnemyAnimatorController>();
        agent = GetComponent<NavMeshAgent>();
        
        // Initialize states
        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        patrolState = new PatrolState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);
        
        stateMachine.Initialize(idleState);
    }
    
    protected virtual void Update()
    {
        // Sync with CoreEnemy
        if (coreEnemy != null)
        {
            playerTarget = coreEnemy.GetCurrentTarget();
        }
        
        // Run legacy state machine for compatibility
        stateMachine.currentState?.Execute();
    }
    
    public virtual void Alert(Transform target) { }
    
    public void ChangeState(State newState) => stateMachine.ChangeState(newState);
    
    public virtual Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        
        foreach (var t in availableTargets)
        {
            if (t != null && t.CompareTag("Player"))
            {
                float dist = Vector3.Distance(transform.position, t.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPlayer = t;
                }
            }
        }
        return closestPlayer;
    }
    
    public virtual bool IsPlayerInDetectionRange()
    {
        if (coreEnemy != null)
        {
            return coreEnemy.GetCurrentTarget() != null;
        }
        return false;
    }
    
    public virtual bool IsPlayerInChaseRange()
    {
        if (coreEnemy != null && playerTarget != null)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            return distance <= coreEnemy.chaseRange;
        }
        return false;
    }
    
    public void SetNavDestination(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
        }
    }
}