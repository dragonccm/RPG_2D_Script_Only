using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ??? UNIFIED ENEMY GROUP MANAGER
/// Qu?n l� nh�m enemy tu?n tra - T??NG TH�CH V?I H? TH?NG M?I
/// K�o th? enemy v�o ?�y ?? t?o ??i h�nh tu?n tra
/// </summary>
public class UnifiedEnemyGroupManager : MonoBehaviour
{
    [Header("??? THI?T L?P NH�M TU?N TRA")]
    public PatrolGroupType patrolGroupType = PatrolGroupType.WaypointRoute;
    public Transform anchor;
    public List<Transform> patrolPoints = new List<Transform>();
    public CoreEnemy.PatrolMode patrolMode = CoreEnemy.PatrolMode.Loop;
    public float randomPatrolRadius = 5f;
    
    [Header("?? DANH S�CH ENEMY")]
    [Tooltip("K�o c�c enemy v�o ?�y ?? t?o nh�m tu?n tra")]
    public List<CoreEnemy> enemies = new List<CoreEnemy>();
    
    [Header("?? FORMATION SETTINGS")]
    public FormationType formationType = FormationType.Line;
    public float formationSpacing = 2f;
    public List<Vector3> customOffsets;
    
    [Header("?? COMBAT SETTINGS")]
    public float detectionRadius = 10f;
    public LayerMask playerLayer = 1 << 7;
    public bool shareTargetBetweenMembers = true;
    
    public enum PatrolGroupType { WaypointRoute, RandomAroundAnchor }
    public enum FormationType { Line, V, Circle, Custom }
    
    private Transform groupTarget;
    private bool isInCombat = false;
    private Vector3 lastAnchorPosition;
    private int currentPatrolIndex = 0;
    private bool patrolForward = true;
    
    private void Start()
    {
        SetupGroupPatrol();
        CalculateFormationOffsets();
    }
    
    private void Update()
    {
        if (shareTargetBetweenMembers)
        {
            UpdateGroupCombat();
        }
        
        if (!isInCombat)
        {
            UpdateGroupFormation();
        }
    }
    
    private void SetupGroupPatrol()
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            // G�n anchor cho t?t c? enemies
            enemy.anchor = anchor;
            
            if (patrolGroupType == PatrolGroupType.WaypointRoute)
            {
                // Patrol theo waypoints
                enemy.patrolMode = patrolMode;
                enemy.patrolPoints = patrolPoints.ToArray();
                enemy.randomRadius = 0f;
            }
            else // RandomAroundAnchor
            {
                // Patrol ng?u nhi�n quanh anchor
                enemy.patrolMode = CoreEnemy.PatrolMode.RandomAroundAnchor;
                enemy.patrolPoints = new Transform[0];
                enemy.randomRadius = randomPatrolRadius;
            }
        }
    }
    
    private void UpdateGroupCombat()
    {
        // T�m target chung cho nh�m
        Transform bestTarget = FindNearestPlayer();
        
        if (bestTarget != null && !isInCombat)
        {
            // B?t ??u combat mode
            isInCombat = true;
            groupTarget = bestTarget;
            
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    // Force target cho t?t c? members
                    SetEnemyTarget(enemy, bestTarget);
                }
            }
        }
        else if (bestTarget == null && isInCombat)
        {
            // K?t th�c combat mode
            isInCombat = false;
            groupTarget = null;
            
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    // Clear target v� return to patrol
                    SetEnemyTarget(enemy, null);
                }
            }
        }
    }
    
    private void UpdateGroupFormation()
    {
        if (anchor == null || enemies.Count == 0) return;
        
        // T�nh to�n formation positions
        Vector3 anchorPos = anchor.position;
        Vector3 moveDirection = (anchorPos - lastAnchorPosition).normalized;
        
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            
            Vector3 formationOffset = GetFormationOffset(i, moveDirection);
            Vector3 targetPos = anchorPos + formationOffset;
            
            // ?i?u ch?nh enemy position n?u c?n
            float distanceToTarget = Vector3.Distance(enemies[i].transform.position, targetPos);
            if (distanceToTarget > formationSpacing * 2f)
            {
                // Enemy qu� xa formation, ?i?u ch?nh
                MoveEnemyToPosition(enemies[i], targetPos);
            }
        }
        
        lastAnchorPosition = anchorPos;
    }
    
    private Vector3 GetFormationOffset(int index, Vector3 moveDirection)
    {
        if (customOffsets != null && index < customOffsets.Count)
        {
            return customOffsets[index];
        }
        
        return formationType switch
        {
            FormationType.Line => Vector3.right * formationSpacing * (index - enemies.Count / 2f),
            FormationType.V => new Vector3(
                (index - enemies.Count / 2f) * formationSpacing,
                0,
                Mathf.Abs(index - enemies.Count / 2f) * formationSpacing * 0.5f
            ),
            FormationType.Circle => GetCircleFormationOffset(index),
            _ => Vector3.zero
        };
    }
    
    private Vector3 GetCircleFormationOffset(int index)
    {
        float radius = formationSpacing * enemies.Count / Mathf.PI;
        float angle = index * Mathf.PI * 2f / enemies.Count;
        return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
    }
    
    private void CalculateFormationOffsets()
    {
        if (customOffsets == null)
            customOffsets = new List<Vector3>();
        
        customOffsets.Clear();
        
        for (int i = 0; i < enemies.Count; i++)
        {
            customOffsets.Add(GetFormationOffset(i, Vector3.forward));
        }
    }
    
    private Transform FindNearestPlayer()
    {
        Vector3 groupCenter = GetGroupCenter();
        Collider[] hitColliders = Physics.OverlapSphere(groupCenter, detectionRadius, playerLayer);
        
        Transform nearestPlayer = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                float distance = Vector3.Distance(groupCenter, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = hit.transform;
                }
            }
        }
        
        return nearestPlayer;
    }
    
    private Vector3 GetGroupCenter()
    {
        if (anchor != null) return anchor.position;
        
        Vector3 center = Vector3.zero;
        int count = 0;
        
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                center += enemy.transform.position;
                count++;
            }
        }
        
        return count > 0 ? center / count : transform.position;
    }
    
    private void SetEnemyTarget(CoreEnemy enemy, Transform target)
    {
        // S? d?ng reflection ho?c public method ?? set target
        // V� CoreEnemy c� currentTarget private, ta c?n c�ch kh�c
        
        // T?m th?i force enemy detect player b?ng c�ch ??t ch�ng g?n player
        if (target != null)
        {
            float currentDistance = Vector3.Distance(enemy.transform.position, target.position);
            if (currentDistance > enemy.detectionRange)
            {
                // Move enemy closer to detection range
                Vector3 direction = (target.position - enemy.transform.position).normalized;
                Vector3 newPos = target.position - direction * (enemy.detectionRange - 1f);
                
                // T?m th?i ?i?u ch?nh v? tr� ?? trigger detection
                enemy.transform.position = Vector3.Lerp(enemy.transform.position, newPos, Time.deltaTime);
            }
        }
    }
    
    private void MoveEnemyToPosition(CoreEnemy enemy, Vector3 targetPos)
    {
        // S? d?ng NavMeshAgent ho?c t??ng t? ?? di chuy?n
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(targetPos);
        }
    }
    
    #region PUBLIC METHODS
    
    [ContextMenu("??? Setup Group Patrol")]
    public void SetupGroup()
    {
        SetupGroupPatrol();
        CalculateFormationOffsets();
    }
    
    [ContextMenu("? Add Selected Enemies")]
    public void AddSelectedEnemies()
    {
        var selectedObjects = UnityEditor.Selection.gameObjects;
        foreach (var obj in selectedObjects)
        {
            var enemy = obj.GetComponent<CoreEnemy>();
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }
        SetupGroup();
    }
    
    public void AddEnemy(CoreEnemy enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
        {
            enemies.Add(enemy);
            SetupGroup();
        }
    }
    
    public void RemoveEnemy(CoreEnemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            CalculateFormationOffsets();
        }
    }
    
    #endregion
    
    #region GIZMOS
    
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetGroupCenter(), detectionRadius);
        
        // Draw patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 1f);
                    
                    // Draw connections
                    if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
        }
        
        // Draw formation
        if (anchor != null && enemies.Count > 0)
        {
            Gizmos.color = Color.blue;
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(anchor.position, enemy.transform.position);
                }
            }
        }
    }
    
    #endregion
}