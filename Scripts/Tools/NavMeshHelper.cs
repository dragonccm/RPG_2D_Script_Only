using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Helper script để kiểm tra và setup NavMesh cho scene
/// </summary>
public class NavMeshHelper : MonoBehaviour
{
    [Header("NavMesh Check Settings")]
    [Tooltip("Khoảng cách tối đa để tìm vị trí NavMesh")]
    public float maxSampleDistance = 20f;
    
    [Tooltip("Hiển thị debug info")]
    public bool showDebugInfo = true;
    
    [Header("Auto Setup")]
    [Tooltip("Tự động setup NavMesh cho tất cả enemy trong scene")]
    public bool autoSetupEnemies = true;
    
    private void Start()
    {
        if (autoSetupEnemies)
        {
            SetupAllEnemies();
        }
    }
    
    /// <summary>
    /// Setup NavMesh cho tất cả enemy trong scene
    /// </summary>
    [ContextMenu("Setup All Enemies")]
    public void SetupAllEnemies()
    {
        var enemies = FindObjectsOfType<Enemy>();
        int successCount = 0;
        int failCount = 0;
        
        foreach (var enemy in enemies)
        {
            if (SetupEnemyNavMesh(enemy))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }
        
        Debug.Log($"NavMesh Setup Complete: {successCount} thành công, {failCount} thất bại");
    }
    
    /// <summary>
    /// Setup NavMesh cho một enemy cụ thể
    /// </summary>
    public bool SetupEnemyNavMesh(Enemy enemy)
    {
        if (enemy == null) return false;
        
        var navAgent = enemy.GetComponent<NavMeshAgent>();
        if (navAgent == null) return true; // Không có NavAgent thì không cần setup
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(enemy.transform.position, out hit, maxSampleDistance, NavMesh.AllAreas))
        {
            enemy.transform.position = hit.position;
            if (showDebugInfo)
            {
                Debug.Log($"Enemy {enemy.name} đã được đặt tại vị trí NavMesh: {hit.position}");
            }
            return true;
        }
        else
        {
            Debug.LogError($"Không thể tìm thấy vị trí NavMesh hợp lệ cho {enemy.name}!");
            return false;
        }
    }
    
    /// <summary>
    /// Kiểm tra NavMesh có được bake chưa
    /// </summary>
    [ContextMenu("Check NavMesh Status")]
    public void CheckNavMeshStatus()
    {
        if (NavMesh.CalculateTriangulation().vertices.Length > 0)
        {
            Debug.Log("NavMesh đã được bake thành công!");
        }
        else
        {
            Debug.LogError("NavMesh chưa được bake! Vui lòng bake NavMesh trong Window > AI > Navigation");
        }
    }
    
    /// <summary>
    /// Tìm vị trí NavMesh gần nhất
    /// </summary>
    public static Vector3 FindNearestNavMeshPosition(Vector3 position, float maxDistance = 20f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position; // Fallback về vị trí gốc
    }
    
    /// <summary>
    /// Kiểm tra vị trí có trên NavMesh không
    /// </summary>
    public static bool IsPositionOnNavMesh(Vector3 position, float tolerance = 0.1f)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, tolerance, NavMesh.AllAreas);
    }
} 