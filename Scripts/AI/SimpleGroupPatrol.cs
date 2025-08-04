using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ?? SIMPLE GROUP PATROL SETUP
/// Script ??n gi?n ?? setup ??i hình tu?n tra cho h? th?ng m?i
/// Ch? c?n kéo enemy vào và thi?t l?p patrol
/// </summary>
public class SimpleGroupPatrol : MonoBehaviour
{
    [Header("?? PATROL SETUP")]
    [Tooltip("Anchor point - ?i?m trung tâm c?a nhóm")]
    public Transform anchor;
    
    [Tooltip("Patrol waypoints - các ?i?m tu?n tra")]
    public Transform[] patrolWaypoints;
    
    [Tooltip("Patrol mode")]
    public CoreEnemy.PatrolMode patrolMode = CoreEnemy.PatrolMode.Loop;
    
    [Tooltip("Random patrol radius (n?u dùng RandomAroundAnchor)")]
    public float randomRadius = 8f;
    
    [Header("?? ENEMY GROUP")]
    [Tooltip("Kéo các enemy vào ?ây")]
    public List<CoreEnemy> groupMembers = new List<CoreEnemy>();
    
    [Header("?? FORMATION")]
    [Tooltip("Kho?ng cách gi?a các thành viên")]
    public float spacing = 3f;
    
    [Tooltip("Formation type")]
    public FormationType formation = FormationType.Line;
    
    public enum FormationType
    {
        Line,       // Hàng ngang
        Column,     // Hàng d?c  
        Circle,     // Vòng tròn
        V,          // Hình ch? V
        Random      // Ng?u nhiên
    }
    
    private void Start()
    {
        SetupGroupPatrol();
    }
    
    [ContextMenu("?? Setup Group Patrol")]
    public void SetupGroupPatrol()
    {
        if (groupMembers.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[SimpleGroupPatrol] Không có enemy nào trong nhóm!");
            return;
        }
        
        // Setup anchor n?u ch?a có
        if (anchor == null)
        {
            anchor = transform;
        }
        
        UnityEngine.Debug.Log($"[SimpleGroupPatrol] Setup patrol cho {groupMembers.Count} enemies");
        
        // Setup t?ng enemy
        for (int i = 0; i < groupMembers.Count; i++)
        {
            var enemy = groupMembers[i];
            if (enemy == null) continue;
            
            SetupIndividualEnemy(enemy, i);
        }
        
        // Arrange formation
        ArrangeFormation();
    }
    
    private void SetupIndividualEnemy(CoreEnemy enemy, int index)
    {
        // Set anchor
        enemy.anchor = anchor;
        
        // Set patrol mode and waypoints
        enemy.patrolMode = patrolMode;
        
        if (patrolMode == CoreEnemy.PatrolMode.RandomAroundAnchor)
        {
            enemy.randomRadius = randomRadius;
            enemy.patrolPoints = new Transform[0]; // Clear waypoints
        }
        else if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            enemy.patrolPoints = patrolWaypoints;
        }
        
        UnityEngine.Debug.Log($"[SimpleGroupPatrol] Setup {enemy.name} - Mode: {patrolMode}, Waypoints: {(patrolWaypoints?.Length ?? 0)}");
    }
    
    private void ArrangeFormation()
    {
        if (anchor == null || groupMembers.Count == 0) return;
        
        Vector3 anchorPos = anchor.position;
        
        for (int i = 0; i < groupMembers.Count; i++)
        {
            if (groupMembers[i] == null) continue;
            
            Vector3 offset = GetFormationOffset(i);
            Vector3 targetPos = anchorPos + offset;
            
            // Set initial position
            groupMembers[i].transform.position = targetPos;
            
            UnityEngine.Debug.Log($"[SimpleGroupPatrol] {groupMembers[i].name} positioned at offset {offset}");
        }
    }
    
    private Vector3 GetFormationOffset(int index)
    {
        int totalMembers = groupMembers.Count;
        
        return formation switch
        {
            FormationType.Line => GetLineOffset(index, totalMembers),
            FormationType.Column => GetColumnOffset(index, totalMembers),
            FormationType.Circle => GetCircleOffset(index, totalMembers),
            FormationType.V => GetVOffset(index, totalMembers),
            FormationType.Random => GetRandomOffset(),
            _ => Vector3.zero
        };
    }
    
    private Vector3 GetLineOffset(int index, int total)
    {
        float x = (index - (total - 1) / 2f) * spacing;
        return new Vector3(x, 0, 0);
    }
    
    private Vector3 GetColumnOffset(int index, int total)
    {
        float z = (index - (total - 1) / 2f) * spacing;
        return new Vector3(0, 0, z);
    }
    
    private Vector3 GetCircleOffset(int index, int total)
    {
        float angle = (360f / total) * index * Mathf.Deg2Rad;
        float radius = spacing * total / (2f * Mathf.PI);
        radius = Mathf.Max(radius, spacing); // Minimum radius
        
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;
        
        return new Vector3(x, 0, z);
    }
    
    private Vector3 GetVOffset(int index, int total)
    {
        float halfCount = (total - 1) / 2f;
        float x = (index - halfCount) * spacing;
        float z = -Mathf.Abs(index - halfCount) * spacing * 0.5f;
        
        return new Vector3(x, 0, z);
    }
    
    private Vector3 GetRandomOffset()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(spacing * 0.5f, spacing * 1.5f);
        
        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;
        
        return new Vector3(x, 0, z);
    }
    
    #region EDITOR HELPERS
    
    [ContextMenu("? Add Selected Enemies")]
    public void AddSelectedEnemies()
    {
        #if UNITY_EDITOR
        var selectedObjects = UnityEditor.Selection.gameObjects;
        
        foreach (var obj in selectedObjects)
        {
            var enemy = obj.GetComponent<CoreEnemy>();
            if (enemy != null && !groupMembers.Contains(enemy))
            {
                groupMembers.Add(enemy);
                UnityEngine.Debug.Log($"[SimpleGroupPatrol] Added {enemy.name} to group");
            }
        }
        
        SetupGroupPatrol();
        #endif
    }
    
    [ContextMenu("?? Refresh Formation")]
    public void RefreshFormation()
    {
        ArrangeFormation();
    }
    
    [ContextMenu("?? Auto-Find Enemies")]
    public void AutoFindEnemies()
    {
        var enemies = FindObjectsOfType<CoreEnemy>();
        
        groupMembers.Clear();
        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) <= spacing * 5f)
            {
                groupMembers.Add(enemy);
            }
        }
        
        UnityEngine.Debug.Log($"[SimpleGroupPatrol] Auto-found {groupMembers.Count} enemies nearby");
        SetupGroupPatrol();
    }
    
    #endregion
    
    #region GIZMOS
    
    private void OnDrawGizmosSelected()
    {
        // Draw anchor
        if (anchor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(anchor.position, 1f);
        }
        
        // Draw patrol waypoints
        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            
            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                if (patrolWaypoints[i] == null) continue;
                
                Gizmos.DrawWireSphere(patrolWaypoints[i].position, 0.5f);
                
                // Draw path connections
                if (patrolMode == CoreEnemy.PatrolMode.Loop)
                {
                    int nextIndex = (i + 1) % patrolWaypoints.Length;
                    if (patrolWaypoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[nextIndex].position);
                    }
                }
                else if (patrolMode == CoreEnemy.PatrolMode.PingPong && i < patrolWaypoints.Length - 1)
                {
                    if (patrolWaypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[i + 1].position);
                    }
                }
            }
        }
        
        // Draw random radius
        if (patrolMode == CoreEnemy.PatrolMode.RandomAroundAnchor && anchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(anchor.position, randomRadius);
        }
        
        // Draw formation
        if (anchor != null && groupMembers.Count > 0)
        {
            Gizmos.color = Color.red;
            
            for (int i = 0; i < groupMembers.Count; i++)
            {
                if (groupMembers[i] == null) continue;
                
                Vector3 offset = GetFormationOffset(i);
                Vector3 formationPos = anchor.position + offset;
                
                // Draw formation position
                Gizmos.DrawWireCube(formationPos, Vector3.one * 0.5f);
                
                // Draw line from enemy to formation position
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(groupMembers[i].transform.position, formationPos);
                Gizmos.color = Color.red;
            }
        }
    }
    
    #endregion
}