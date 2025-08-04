using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chase State - sử dụng hệ thống 4 file mới
/// </summary>
public class ChaseState : State
{
    private float stateUpdateInterval = 0.3f;
    private float nextStateUpdate;
    private NavMeshAgent agent;
    
    public ChaseState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
        agent = aiController.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
        base.Enter();
        
        if (agent != null && aiController.playerTarget != null)
        {
            agent.SetDestination(aiController.playerTarget.position);
            aiController.animatorController?.PlayMoveAnimation(agent.speed);
        }
    }

    public override void Execute()
    {
        base.Execute();
        
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;
        
        var coreEnemy = aiController.GetComponent<CoreEnemy>();
        if (coreEnemy == null) return;

        float chaseRange = coreEnemy.chaseRange;
        float attackRange = coreEnemy.attackRange;

        if (aiController.playerTarget == null)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        if (distanceToPlayer > chaseRange)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }
        
        if (distanceToPlayer <= attackRange)
        {
            stateMachine.ChangeState(aiController.attackState);
            return;
        }
        
        // Continue chase
        if (agent != null && aiController.playerTarget != null)
        {
            agent.SetDestination(aiController.playerTarget.position);
        }
    }

    public override void Exit()
    {
        base.Exit();
        aiController.animatorController?.PlayIdleAnimation();
    }
}
