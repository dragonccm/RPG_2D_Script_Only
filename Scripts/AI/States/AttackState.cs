using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Attack State - sử dụng hệ thống unified mới
/// </summary>
public class AttackState : State
{
    private float stateUpdateInterval = 0.3f;
    private float nextStateUpdate;

    public AttackState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        aiController.animatorController?.PlayAttackAnimation();
    }

    public override void Execute()
    {
        base.Execute();

        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;

        // Get CoreEnemy component instead of old Enemy
        var coreEnemy = aiController.GetComponent<CoreEnemy>();
        if (coreEnemy == null) return;

        if (aiController.playerTarget == null)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
        float attackRange = coreEnemy.attackRange;

        if (distanceToPlayer > attackRange * 1.2f)
        {
            stateMachine.ChangeState(aiController.chaseState);
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            // Use EnemySkillManager for attacks
            var skillManager = aiController.GetComponent<EnemySkillManager>();
            if (skillManager != null && skillManager.CanUseSkill())
            {
                skillManager.UseSkill();
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        aiController.animatorController?.PlayIdleAnimation();
    }
}
