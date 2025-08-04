using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controller quản lý các loại di chuyển đặc biệt (Special Movement) cho Player.
/// Lắng nghe phím C để kích hoạt Special Movement.
/// </summary>
public class PlayerSpecialMovementController : MonoBehaviour
{
    [Header("Special Movement Settings")]
    [Tooltip("Phím để kích hoạt Special Movement")]
    public KeyCode activationKey = KeyCode.C;
    
    [Tooltip("Chế độ kích hoạt Special Movement")]
    public ActivationMode activationMode = ActivationMode.FirstAvailable;
    
    [Tooltip("Hiển thị thông tin cooldown trên UI")]
    public bool showCooldownInfo = true;
    
    [Header("Special Movement Components")]
    [Tooltip("Danh sách các Special Movement được gắn trên Player")]
    public List<ISpecialMovement> specialMovements = new List<ISpecialMovement>();
    
    private PlayerController playerController;
    private bool isProcessingInput = false;
    
    public enum ActivationMode
    {
        FirstAvailable,    // Kích hoạt Special Movement đầu tiên có thể
        Random,            // Kích hoạt ngẫu nhiên một Special Movement
        Cycle,             // Luân phiên qua các Special Movement
        All                // Kích hoạt tất cả Special Movement có thể
    }
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        CollectSpecialMovements();
    }
    
    void Update()
    {
        if (isProcessingInput) return;
        
        // Kiểm tra input kích hoạt Special Movement
        if (Input.GetKeyDown(activationKey))
        {
            ActivateSpecialMovement();
        }
    }
    
    /// <summary>
    /// Thu thập tất cả Special Movement components trên GameObject
    /// </summary>
    private void CollectSpecialMovements()
    {
        specialMovements.Clear();
        
        // Tìm tất cả components implement ISpecialMovement
        var components = GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component is ISpecialMovement specialMovement)
            {
                specialMovements.Add(specialMovement);
            }
        }
        
        // Sắp xếp theo thứ tự ưu tiên (có thể customize sau)
        specialMovements = specialMovements.OrderBy(x => x.MovementName).ToList();
    }
    
    /// <summary>
    /// Kích hoạt Special Movement theo chế độ đã chọn
    /// </summary>
    private void ActivateSpecialMovement()
    {
        if (specialMovements.Count == 0) return;
        
        isProcessingInput = true;
        
        switch (activationMode)
        {
            case ActivationMode.FirstAvailable:
                ActivateFirstAvailable();
                break;
                
            case ActivationMode.Random:
                ActivateRandom();
                break;
                
            case ActivationMode.Cycle:
                ActivateNextInCycle();
                break;
                
            case ActivationMode.All:
                ActivateAllAvailable();
                break;
        }
        
        isProcessingInput = false;
    }
    
    /// <summary>
    /// Kích hoạt Special Movement đầu tiên có thể
    /// </summary>
    private void ActivateFirstAvailable()
    {
        foreach (var movement in specialMovements)
        {
            if (movement.CanActivate())
            {
                movement.Activate();
                break;
            }
        }
    }
    
    /// <summary>
    /// Kích hoạt Special Movement ngẫu nhiên
    /// </summary>
    private void ActivateRandom()
    {
        var availableMovements = specialMovements.Where(x => x.CanActivate()).ToList();
        if (availableMovements.Count > 0)
        {
            int randomIndex = Random.Range(0, availableMovements.Count);
            availableMovements[randomIndex].Activate();
        }
    }
    
    /// <summary>
    /// Kích hoạt Special Movement tiếp theo trong chu kỳ
    /// </summary>
    private void ActivateNextInCycle()
    {
        // Tìm Special Movement tiếp theo có thể kích hoạt
        for (int i = 0; i < specialMovements.Count; i++)
        {
            if (specialMovements[i].CanActivate())
            {
                specialMovements[i].Activate();
                break;
            }
        }
    }
    
    /// <summary>
    /// Kích hoạt tất cả Special Movement có thể
    /// </summary>
    private void ActivateAllAvailable()
    {
        foreach (var movement in specialMovements)
        {
            if (movement.CanActivate())
            {
                movement.Activate();
            }
        }
    }
    
    /// <summary>
    /// Lấy thông tin cooldown của tất cả Special Movement
    /// </summary>
    public List<string> GetCooldownInfo()
    {
        List<string> info = new List<string>();
        
        foreach (var movement in specialMovements)
        {
            if (movement.CooldownRemaining > 0)
            {
                info.Add($"{movement.MovementName}: {movement.CooldownRemaining:F1}s");
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// Kiểm tra xem có Special Movement nào đang active không
    /// </summary>
    public bool HasActiveSpecialMovement()
    {
        return specialMovements.Any(x => x.IsActive);
    }
    
    /// <summary>
    /// Lấy Special Movement đang active
    /// </summary>
    public ISpecialMovement GetActiveSpecialMovement()
    {
        return specialMovements.FirstOrDefault(x => x.IsActive);
    }
    
    /// <summary>
    /// Refresh danh sách Special Movement (gọi khi thêm/xóa components)
    /// </summary>
    public void RefreshSpecialMovements()
    {
        CollectSpecialMovements();
    }
} 