using UnityEngine;
using System;

/// <summary>
/// Interface cơ bản cho tất cả các loại di chuyển đặc biệt (Special Movement).
/// Mọi script di chuyển đặc biệt đều phải implement interface này.
/// </summary>
public interface ISpecialMovement
{
    /// <summary>
    /// Tên của loại di chuyển đặc biệt
    /// </summary>
    string MovementName { get; }
    
    /// <summary>
    /// Trạng thái hiện tại của di chuyển đặc biệt
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Kiểm tra xem có thể kích hoạt di chuyển đặc biệt không
    /// </summary>
    bool CanActivate();
    
    /// <summary>
    /// Kích hoạt di chuyển đặc biệt
    /// </summary>
    void Activate();
    
    /// <summary>
    /// Tắt di chuyển đặc biệt
    /// </summary>
    void Deactivate();
    
    /// <summary>
    /// Thời gian cooldown còn lại (0 = có thể kích hoạt)
    /// </summary>
    float CooldownRemaining { get; }
}

/// <summary>
/// Abstract class cung cấp implementation cơ bản cho Special Movement.
/// Các script di chuyển đặc biệt cụ thể sẽ kế thừa từ đây.
/// </summary>
public abstract class SpecialMovementBase : MonoBehaviour, ISpecialMovement
{
    [Header("Special Movement Settings")]
    [Tooltip("Tên của loại di chuyển đặc biệt")]
    public string movementName = "Special Movement";
    
    [Tooltip("Thời gian cooldown giữa các lần kích hoạt (giây)")]
    public float cooldownDuration = 5f;
    
    [Tooltip("Thời gian di chuyển đặc biệt kéo dài (giây)")]
    public float duration = 2f;
    
    [Tooltip("Có thể kích hoạt khi đang trong trạng thái khác không")]
    public bool canActivateWhileBusy = false;
    
    [Tooltip("Tự động tắt sau khi hết thời gian")]
    public bool autoDeactivate = true;
    
    protected bool _isActive = false;
    protected float _cooldownEndTime = 0f;
    protected float _activationEndTime = 0f;
    
    // ISpecialMovement implementation
    public string MovementName => movementName;
    public bool IsActive => _isActive;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndTime - Time.time);
    
    public virtual bool CanActivate()
    {
        if (_isActive) return false;
        if (Time.time < _cooldownEndTime) return false;
        
        // Kiểm tra xem có đang bận không (nếu không cho phép kích hoạt khi bận)
        if (!canActivateWhileBusy)
        {
            var playerController = GetComponent<PlayerController>();
            if (playerController != null && playerController.IsBusy())
                return false;
        }
        
        return OnCanActivate();
    }
    
    public virtual void Activate()
    {
        if (!CanActivate()) return;
        
        _isActive = true;
        _activationEndTime = Time.time + duration;
        _cooldownEndTime = Time.time + cooldownDuration;
        
        OnActivate();
        
        if (autoDeactivate)
        {
            Invoke(nameof(Deactivate), duration);
        }
    }
    
    public virtual void Deactivate()
    {
        if (!_isActive) return;
        
        _isActive = false;
        OnDeactivate();
    }
    
    protected virtual void Update()
    {
        if (_isActive && autoDeactivate && Time.time >= _activationEndTime)
        {
            Deactivate();
        }
    }
    
    // Abstract methods cho các lớp con implement
    protected abstract bool OnCanActivate();
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    
    /// <summary>
    /// Method để các lớp con override nếu cần khởi tạo thêm
    /// </summary>
    protected virtual void OnAwake() { }
    
    protected virtual void Awake()
    {
        // Initialize components if needed
        OnAwake();
    }
} 