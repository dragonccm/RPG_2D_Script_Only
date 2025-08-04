using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ??? UNIFIED GROUP PATROL SYSTEM
/// Centralized group management compatible with CoreEnemy system
/// CORE c?a h? th?ng ??i hình
/// </summary>
public class UnifiedGroupPatrol : MonoBehaviour
{
    [Header("??? GROUP SETUP")]
    [Tooltip("Anchor point - leader of the group")]
    public Transform anchor;
    
    [Tooltip("Group members - drag CoreEnemy objects here")]
    public List<CoreEnemy> groupMembers = new List<CoreEnemy>();
    
    [Header("?? PATROL SETUP")]
    public PatrolType patrolType = PatrolType.Waypoints;
    public Transform[] patrolWaypoints;
    public CoreEnemy.PatrolMode patrolMode = CoreEnemy.PatrolMode.Loop;
    public float randomRadius = 8f;
    
    [Header("?? FORMATION")]
    public FormationType formation = FormationType.Line;
    public float spacing = 3f;
    public bool maintainFormationInCombat = false;
    
    [Header("?? COMBAT")]
    public bool shareTargets = true;
    public float groupDetectionRadius = 12f;
    public LayerMask playerLayer = 1 << 7;
    
    [Header("?? ADVANCED")]
    public bool enableGroupSync = true;
    public float syncCheckInterval = 0.5f;
    
    public enum PatrolType { None, Waypoints, RandomAroundAnchor }
    public enum FormationType { Line, Column, Circle, V, Random, Custom }
    
    // UNIFIED GROUP STATE
    private bool isInCombat = false;
    private Transform groupTarget;
    private Vector3[] formationOffsets;
    private float lastSyncCheck = 0f;
    
    // UNIFIED STATISTICS
    private Dictionary<CoreEnemy, float> memberLastPositions = new Dictionary<CoreEnemy, float>();
    private Dictionary<CoreEnemy, Vector3> memberTargetPositions = new Dictionary<CoreEnemy, Vector3>();
    
    private void Start()
    {
        InitializeGroup();
    }
    
    private void Update()
    {
        if (enableGroupSync && Time.time - lastSyncCheck >= syncCheckInterval)
        {
            UpdateGroupSync();
            lastSyncCheck = Time.time;
        }
        
        if (shareTargets)
        {
            UpdateGroupCombat();
        }
        
        if (maintainFormationInCombat || !isInCombat)
        {
            UpdateGroupFormation();
        }
    }
    
    private void InitializeGroup()
    {
        // Setup anchor
        if (anchor == null && groupMembers.Count > 0)
        {
            anchor = groupMembers[0].transform;
        }
        
        // Calculate formation offsets
        CalculateFormationOffsets();
        
        // Setup each member
        for (int i = 0; i < groupMembers.Count; i++)
        {
            SetupGroupMember(groupMembers[i], i);
        }
        
        UnityEngine.Debug.Log($"[UnifiedGroupPatrol] Initialized group with {groupMembers.Count} members");
    }
    
    private void SetupGroupMember(CoreEnemy member, int index)
    {
        if (member == null) return;
        
        // Setup patrol
        switch (patrolType)
        {
            case PatrolType.Waypoints:
                member.SetupPatrol(patrolMode, anchor, patrolWaypoints);
                break;
            case PatrolType.RandomAroundAnchor:
                member.SetupPatrol(CoreEnemy.PatrolMode.RandomAroundAnchor, anchor, null, randomRadius);
                break;
            case PatrolType.None:
                member.SetupPatrol(CoreEnemy.PatrolMode.None, anchor);
                break;
        }
        
        // Setup formation position
        if (formationOffsets != null && index < formationOffsets.Length)
        {
            Vector3 formationPos = anchor.position + formationOffsets[index];
            member.transform.position = formationPos;
            memberTargetPositions[member] = formationPos;
        }
        
        // Connect events for group coordination
        member.OnTargetChanged += OnMemberTargetChanged;
        member.OnStateChanged += OnMemberStateChanged;
        
        UnityEngine.Debug.Log($"[UnifiedGroupPatrol] Setup member {member.name} at index {index}");
    }
    
    private void CalculateFormationOffsets()
    {
        if (groupMembers.Count == 0) return;
        
        formationOffsets = new Vector3[groupMembers.Count];
        
        switch (formation)
        {
            case FormationType.Line:
                CalculateLineFormation();
                break;
            case FormationType.Column:
                CalculateColumnFormation();
                break;
            case FormationType.Circle:
                CalculateCircleFormation();
                break;
            case FormationType.V:
                CalculateVFormation();
                break;
            case FormationType.Random:
                CalculateRandomFormation();
                break;
            case FormationType.Custom:
                // Keep existing offsets or set to zero
                break;
        }
    }
    
    private void CalculateLineFormation()
    {
        for (int i = 0; i < groupMembers.Count; i++)
        {
            float x = (i - (groupMembers.Count - 1) / 2f) * spacing;
            formationOffsets[i] = new Vector3(x, 0, 0);
        }
    }
    
    private void CalculateColumnFormation()
    {
        for (int i = 0; i < groupMembers.Count; i++)
        {
            float z = (i - (groupMembers.Count - 1) / 2f) * spacing;
            formationOffsets[i] = new Vector3(0, 0, z);
        }
    }
    
    private void CalculateCircleFormation()
    {
        float radius = spacing * groupMembers.Count / (2f * Mathf.PI);
        radius = Mathf.Max(radius, spacing);
        
        for (int i = 0; i < groupMembers.Count; i++)
        {
            float angle = (360f / groupMembers.Count) * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            formationOffsets[i] = new Vector3(x, 0, z);
        }
    }
    
    private void CalculateVFormation()
    {
        for (int i = 0; i < groupMembers.Count; i++)
        {
            float halfCount = (groupMembers.Count - 1) / 2f;
            float x = (i - halfCount) * spacing;
            float z = -Mathf.Abs(i - halfCount) * spacing * 0.5f;
            formationOffsets[i] = new Vector3(x, 0, z);
        }
    }
    
    private void CalculateRandomFormation()
    {
        for (int i = 0; i < groupMembers.Count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(spacing * 0.5f, spacing * 1.5f);
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            formationOffsets[i] = new Vector3(x, 0, z);
        }
    }
    
    private void UpdateGroupSync()
    {
        if (!enableGroupSync || anchor == null) return;
        
        // Check if any member is too far from formation
        for (int i = 0; i < groupMembers.Count; i++)
        {
            var member = groupMembers[i];
            if (member == null) continue;
            
            Vector3 expectedPos = anchor.position + formationOffsets[i];
            float distance = Vector3.Distance(member.transform.position, expectedPos);
            
            // If member is too far, guide them back
            if (distance > spacing * 2f && !isInCombat)
            {
                memberTargetPositions[member] = expectedPos;
                // Force member to move towards formation position
                // This could be done through CoreEnemy's ForceTarget method
            }
        }
    }
    
    private void UpdateGroupCombat()
    {
        if (!shareTargets) return;
        
        // Find best target for the group
        Transform bestTarget = FindBestGroupTarget();
        
        if (bestTarget != null && !isInCombat)
        {
            // Start group combat
            isInCombat = true;
            groupTarget = bestTarget;
            
            foreach (var member in groupMembers)
            {
                if (member != null)
                {
                    member.ForceTarget(bestTarget);
                    member.ForceState(CoreEnemy.EnemyState.Chase);
                }
            }
            
            UnityEngine.Debug.Log($"[UnifiedGroupPatrol] Group entering combat with {bestTarget.name}");
        }
        else if (bestTarget == null && isInCombat)
        {
            // End group combat
            isInCombat = false;
            groupTarget = null;
            
            foreach (var member in groupMembers)
            {
                if (member != null)
                {
                    member.ForceTarget(null);
                    // Let members return to their individual patrol states
                }
            }
            
            UnityEngine.Debug.Log("[UnifiedGroupPatrol] Group exiting combat, returning to patrol");
        }
    }
    
    private Transform FindBestGroupTarget()
    {
        Vector3 groupCenter = GetGroupCenter();
        Collider[] hits = Physics.OverlapSphere(groupCenter, groupDetectionRadius, playerLayer);
        
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var playerChar = hit.GetComponent<Character>();
                if (playerChar == null || playerChar.CurrentHealth <= 0) continue;
                
                float distance = Vector3.Distance(groupCenter, hit.transform.position);
                float score = 1f - (distance / groupDetectionRadius); // Closer = higher score
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = hit.transform;
                }
            }
        }
        
        return bestTarget;
    }
    
    private void UpdateGroupFormation()
    {
        if (anchor == null || formationOffsets == null) return;
        
        for (int i = 0; i < groupMembers.Count; i++)
        {
            var member = groupMembers[i];
            if (member == null || i >= formationOffsets.Length) continue;
            
            Vector3 targetPos = anchor.position + formationOffsets[i];
            memberTargetPositions[member] = targetPos;
            
            // Check if member needs guidance to formation position
            float distance = Vector3.Distance(member.transform.position, targetPos);
            if (distance > spacing * 1.5f)
            {
                // Member is too far from formation, guide them
                // This could be enhanced with NavMesh pathfinding
            }
        }
    }
    
    private Vector3 GetGroupCenter()
    {
        if (anchor != null) return anchor.position;
        
        Vector3 center = Vector3.zero;
        int count = 0;
        
        foreach (var member in groupMembers)
        {
            if (member != null)
            {
                center += member.transform.position;
                count++;
            }
        }
        
        return count > 0 ? center / count : transform.position;
    }
    
    #region EVENT HANDLERS
    
    private void OnMemberTargetChanged(Transform target)
    {
        if (shareTargets && target != null && !isInCombat)
        {
            // A member found a target, share it with the group
            groupTarget = target;
            isInCombat = true;
            
            foreach (var member in groupMembers)
            {
                if (member != null)
                {
                    member.ForceTarget(target);
                }
            }
        }
    }
    
    private void OnMemberStateChanged(CoreEnemy.EnemyState state)
    {
        // Could implement group state synchronization here
        // For example, if one member goes to dead state, others might become more aggressive
    }
    
    #endregion
    
    #region UNIFIED PUBLIC API
    
    public void AddMember(CoreEnemy newMember)
    {
        if (newMember != null && !groupMembers.Contains(newMember))
        {
            groupMembers.Add(newMember);
            CalculateFormationOffsets();
            SetupGroupMember(newMember, groupMembers.Count - 1);
        }
    }
    
    public void RemoveMember(CoreEnemy member)
    {
        if (groupMembers.Contains(member))
        {
            // Disconnect events
            member.OnTargetChanged -= OnMemberTargetChanged;
            member.OnStateChanged -= OnMemberStateChanged;
            
            groupMembers.Remove(member);
            CalculateFormationOffsets();
        }
    }
    
    public void ChangeFormation(FormationType newFormation)
    {
        formation = newFormation;
        CalculateFormationOffsets();
    }
    
    public void SetPatrol(PatrolType type, Transform[] waypoints = null, float radius = 8f)
    {
        patrolType = type;
        patrolWaypoints = waypoints;
        randomRadius = radius;
        
        // Update all members
        foreach (var member in groupMembers)
        {
            if (member != null)
            {
                SetupGroupMember(member, groupMembers.IndexOf(member));
            }
        }
    }
    
    public bool IsGroupInCombat() => isInCombat;
    public Transform GetGroupTarget() => groupTarget;
    public int GetMemberCount() => groupMembers.Count;
    public List<CoreEnemy> GetActiveMembers() => groupMembers.FindAll(m => m != null && m.IsAlive);
    
    #endregion
    
    #region CONTEXT MENU HELPERS
    
    [ContextMenu("??? Initialize Group")]
    public void InitializeGroupFromMenu()
    {
        InitializeGroup();
        UnityEngine.Debug.Log("Group initialized from menu");
    }
    
    [ContextMenu("?? Recalculate Formation")]
    public void RecalculateFormationFromMenu()
    {
        CalculateFormationOffsets();
        UnityEngine.Debug.Log("Formation recalculated");
    }
    
    [ContextMenu("? Auto-Find Members")]
    public void AutoFindMembersFromMenu()
    {
        var nearbyEnemies = FindObjectsOfType<CoreEnemy>();
        groupMembers.Clear();
        
        foreach (var enemy in nearbyEnemies)
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) <= spacing * 4f)
            {
                groupMembers.Add(enemy);
            }
        }
        
        InitializeGroup();
        UnityEngine.Debug.Log($"Auto-found {groupMembers.Count} group members");
    }
    
    [ContextMenu("?? Show Group Stats")]
    public void ShowGroupStatsFromMenu()
    {
        UnityEngine.Debug.Log($"=== GROUP STATS ===");
        UnityEngine.Debug.Log($"Members: {groupMembers.Count}");
        UnityEngine.Debug.Log($"Formation: {formation}");
        UnityEngine.Debug.Log($"In Combat: {isInCombat}");
        UnityEngine.Debug.Log($"Target: {(groupTarget?.name ?? "None")}");
    }
    
    #endregion
    
    #region GIZMOS
    
    private void OnDrawGizmosSelected()
    {
        // Draw group detection radius
        Gizmos.color = Color.yellow;
        Vector3 center = GetGroupCenter();
        Gizmos.DrawWireSphere(center, groupDetectionRadius);
        
        // Draw formation
        if (anchor != null && formationOffsets != null)
        {
            Gizmos.color = Color.blue;
            foreach (var offset in formationOffsets)
            {
                Vector3 pos = anchor.position + offset;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
            }
        }
        
        // Draw patrol waypoints
        if (patrolWaypoints != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                if (patrolWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolWaypoints[i].position, 1f);
                    
                    // Draw connections
                    if (i < patrolWaypoints.Length - 1 && patrolWaypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[i + 1].position);
                    }
                    else if (patrolMode == CoreEnemy.PatrolMode.Loop && patrolWaypoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[0].position);
                    }
                }
            }
        }
        
        // Draw random patrol radius
        if (patrolType == PatrolType.RandomAroundAnchor && anchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchor.position, randomRadius);
        }
    }
    
    #endregion
}