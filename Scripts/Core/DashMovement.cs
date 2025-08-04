using UnityEngine;

/// <summary>
/// Di chuyển đặc biệt: Dash - di chuyển nhanh về phía trước
/// </summary>
public class DashMovement : SpecialMovementBase
{
    [Header("Dash Settings")]
    [Tooltip("Tốc độ dash (đơn vị/giây)")]
    public float dashSpeed = 15f;
    
    [Tooltip("Khoảng cách tối đa có thể dash")]
    public float maxDashDistance = 8f;
    
    [Tooltip("Có thể dash qua vật cản không")]
    public bool canDashThroughObstacles = false;
    
    [Tooltip("Layer mask cho vật cản khi dash")]
    public LayerMask obstacleLayerMask = -1;
    
    private Rigidbody2D rb;
    private Vector2 dashDirection;
    private Vector3 originalPosition;
    private bool isDashing = false;
    
    protected override void OnAwake()
    {
        rb = GetComponent<Rigidbody2D>();
        movementName = "Dash";
    }
    
    protected override bool OnCanActivate()
    {
        // Kiểm tra có Rigidbody2D không
        if (rb == null) return false;
        
        // Lấy hướng di chuyển từ input hoặc hướng nhìn
        dashDirection = GetDashDirection();
        if (dashDirection.sqrMagnitude < 0.1f) return false;
        
        // Kiểm tra có thể dash đến vị trí đích không
        Vector3 targetPosition = transform.position + (Vector3)(dashDirection.normalized * maxDashDistance);
        
        if (!canDashThroughObstacles)
        {
            // Kiểm tra có vật cản không
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection.normalized, maxDashDistance, obstacleLayerMask);
            if (hit.collider != null)
            {
                targetPosition = hit.point - (Vector2)(dashDirection.normalized * 0.5f);
            }
        }
        
        return true;
    }
    
    protected override void OnActivate()
    {
        originalPosition = transform.position;
        isDashing = true;
        
        // Tắt gravity và collision tạm thời nếu cần
        if (canDashThroughObstacles)
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
        
        // Bắt đầu dash
        StartCoroutine(DashCoroutine());
    }
    
    protected override void OnDeactivate()
    {
        isDashing = false;
        
        // Khôi phục collision
        if (canDashThroughObstacles)
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
        }
        
        // Dừng velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    private System.Collections.IEnumerator DashCoroutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + (Vector3)(dashDirection.normalized * maxDashDistance);
        
        // Kiểm tra và điều chỉnh target position nếu có vật cản
        if (!canDashThroughObstacles)
        {
            RaycastHit2D hit = Physics2D.Raycast(startPos, dashDirection.normalized, maxDashDistance, obstacleLayerMask);
            if (hit.collider != null)
            {
                targetPos = hit.point - (Vector2)(dashDirection.normalized * 0.5f);
            }
        }
        
        float dashTime = Vector3.Distance(startPos, targetPos) / dashSpeed;
        float elapsed = 0f;
        
        while (elapsed < dashTime && isDashing)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashTime;
            
            // Sử dụng curve để tạo hiệu ứng dash mượt mà
            float curveT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, targetPos, curveT);
            
            yield return null;
        }
        
        // Đảm bảo đến đúng vị trí cuối
        if (isDashing)
        {
            transform.position = targetPos;
        }
    }
    
    private Vector2 GetDashDirection()
    {
        // Ưu tiên input từ player
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            return new Vector2(horizontal, vertical).normalized;
        }
        
        // Nếu không có input, dash theo hướng nhìn
        var playerController = GetComponent<PlayerController>();
        if (playerController != null && playerController.movement.sqrMagnitude > 0.1f)
        {
            return playerController.movement.normalized;
        }
        
        // Fallback: dash về phía trước (theo scale.x)
        return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
    }
} 