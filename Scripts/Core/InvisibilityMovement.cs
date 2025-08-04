using UnityEngine;

/// <summary>
/// Di chuyển đặc biệt: Invisibility - tàng hình trong một khoảng thời gian
/// </summary>
public class InvisibilityMovement : SpecialMovementBase
{
    [Header("Invisibility Settings")]
    [Tooltip("Độ trong suốt khi tàng hình (0 = hoàn toàn trong suốt, 1 = hoàn toàn hiện)")]
    [Range(0f, 1f)]
    public float invisibilityAlpha = 0.3f;
    
    [Tooltip("Có thể di chuyển khi tàng hình không")]
    public bool canMoveWhileInvisible = true;
    
    [Tooltip("Có thể tấn công khi tàng hình không")]
    public bool canAttackWhileInvisible = false;
    
    [Tooltip("Tự động hiện lại khi tấn công")]
    public bool revealOnAttack = true;
    
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool wasInvisible = false;
    
    protected override void OnAwake()
    {
        movementName = "Invisibility";
        
        // Cache tất cả SpriteRenderer
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }
    
    protected override bool OnCanActivate()
    {
        // Kiểm tra có SpriteRenderer không
        if (spriteRenderers == null || spriteRenderers.Length == 0) return false;
        
        return true;
    }
    
    protected override void OnActivate()
    {
        wasInvisible = false;
        
        // Áp dụng hiệu ứng tàng hình
        SetInvisibility(true);
        
        // Tắt collision với enemy nếu cần
        if (gameObject.CompareTag("Player"))
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), col, true);
                }
            }
        }
    }
    
    protected override void OnDeactivate()
    {
        // Khôi phục hiển thị
        SetInvisibility(false);
        
        // Khôi phục collision
        if (gameObject.CompareTag("Player"))
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, 5f);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), col, false);
                }
            }
        }
    }
    
    private void SetInvisibility(bool invisible)
    {
        if (spriteRenderers == null) return;
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color color = originalColors[i];
                color.a = invisible ? invisibilityAlpha : originalColors[i].a;
                spriteRenderers[i].color = color;
            }
        }
        
        wasInvisible = invisible;
    }
    
    // Gọi từ bên ngoài khi player tấn công
    public void OnAttack()
    {
        if (revealOnAttack && _isActive)
        {
            Deactivate();
        }
    }
    
    // Override để kiểm tra xem có thể tấn công khi tàng hình không
    public bool CanAttackWhileInvisible()
    {
        return canAttackWhileInvisible && _isActive;
    }
    
    // Override để kiểm tra xem có thể di chuyển khi tàng hình không
    public bool CanMoveWhileInvisible()
    {
        return canMoveWhileInvisible || !_isActive;
    }
} 