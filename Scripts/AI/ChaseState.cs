using UnityEngine;

/// <summary>
/// Trạng thái truy đuổi mục tiêu của kẻ địch.
/// </summary>
public class ChaseState : State
{
    public ChaseState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter ChaseState: target={aiController.playerTarget?.name}");
        var moveCtrl = aiController.GetComponent<EnemyMovementController>();
        if (moveCtrl != null)
        {
            // EnemyMovementController sẽ tự động di chuyển đến playerTarget
            moveCtrl.playerTarget = aiController.playerTarget;
            Debug.Log($"[{aiController.enemyType}] ChaseState: Set MovementController target to {moveCtrl.playerTarget?.name}");
        }
    }

    public override void Execute()
    {
        base.Execute();
        var enemy = aiController.GetComponent<Enemy>();
        var attackController = aiController.GetComponent<EnemyAttackController>();

        // Lấy phạm vi truy đuổi và tấn công từ Enemy và EnemyAttackController
        float chaseRange = enemy != null ? enemy.chaseRange : 20f;
        float attackRange = attackController != null ? attackController.AttackRange : 2f; // Sử dụng AttackRange property

        if (aiController.playerTarget == null)
        {
            Debug.Log($"[{aiController.enemyType}] ChaseState: Lost target, switching to PatrolState");
            stateMachine.ChangeState(aiController.patrolState); // Hoặc idleState nếu không có tuần tra
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
        Debug.Log($"[{aiController.enemyType}] ChaseState: Distance to Player: {distanceToPlayer:F2}m. Attack Range: {attackRange:F2}m. Chase Range: {chaseRange:F2}m.");


        // Nếu player ra khỏi vùng chaseRange → Patrol/Idle
        if (distanceToPlayer > chaseRange)
        {
            Debug.Log($"[{aiController.enemyType}] ChaseState: Player out of chase range ({distanceToPlayer:F2}m > {chaseRange:F2}m), switching to PatrolState");
            stateMachine.ChangeState(aiController.patrolState); // Hoặc idleState nếu không có tuần tra
            return;
        }

        // Nếu trong vùng tấn công, chuyển sang Attack
        if (attackController != null && distanceToPlayer <= attackRange)
        {
            Debug.Log($"[{aiController.enemyType}] ChaseState: Player in attack range ({distanceToPlayer:F2}m <= {attackRange:F2}m), switching to AttackState");
            stateMachine.ChangeState(aiController.attackState);
        }
        // Không cần gọi MoveTo nữa vì EnemyMovementController.Update sẽ tự động set destination
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit ChaseState");
        var moveCtrl = aiController.GetComponent<EnemyMovementController>();
        if (moveCtrl != null)
        {
            moveCtrl.playerTarget = null; // Dừng di chuyển theo player khi thoát trạng thái Chase
            Debug.Log($"[{aiController.enemyType}] ChaseState: Cleared MovementController target.");
        }
    }
}
