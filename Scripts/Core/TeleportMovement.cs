using UnityEngine;
using System.Collections;

/// <summary>
/// 🚀 TELEPORT MOVEMENT - Special Movement cho Boss
/// Cho phép boss teleport đến vị trí chiến thuật hoặc trốn thoát
/// </summary>
public class TeleportMovement : SpecialMovementBase
{
    [Header("🚀 TELEPORT SETTINGS")]
    [SerializeField] private float teleportRange = 10f;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    [SerializeField] private int maxTeleportAttempts = 5;
    [SerializeField] private bool teleportBehindPlayer = true;
    [SerializeField] private float behindPlayerDistance = 3f;
    
    [Header("🎨 EFFECTS")]
    [SerializeField] private GameObject teleportStartEffect;
    [SerializeField] private GameObject teleportEndEffect;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private float effectDuration = 0.5f;
    
    private bool isTeleporting = false;
    private Transform playerTarget;
    private Enemy enemy;
    private EnemyAIController aiController;
    
    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>();
        aiController = GetComponent<EnemyAIController>();
        
        // Set default values
        if (string.IsNullOrEmpty(movementName))
            movementName = "Teleport";
    }
    
    public override bool CanActivate()
    {
        if (!base.CanActivate()) return false;
        if (isTeleporting) return false;
        
        return OnCanActivate();
    }
    
    public override void Activate()
    {
        if (!CanActivate()) return;
        
        OnActivate();
        
        // Set cooldown
        _cooldownEndTime = Time.time + cooldownDuration;
        _activationEndTime = Time.time + duration;
        _isActive = true;
    }
    
    public override void Deactivate()
    {
        if (!_isActive) return;
        
        _isActive = false;
        
        OnDeactivate();
    }
    
    private Transform GetValidTarget()
    {
        // Ưu tiên lấy từ AIController
        if (aiController != null && aiController.playerTarget != null)
        {
            return aiController.playerTarget;
        }
        
        // Fallback: lấy từ Enemy
        if (enemy != null && enemy.GetCurrentTarget() != null && enemy.GetCurrentTarget().CompareTag("Player"))
        {
            return enemy.GetCurrentTarget();
        }
        
        // Last resort: tìm player gần nhất
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= teleportRange)
            {
                return player.transform;
            }
        }
        
        return null;
    }
    
    private IEnumerator ExecuteTeleport()
    {
        isTeleporting = true;
        
        // Play start effect
        if (teleportStartEffect != null)
        {
            Instantiate(teleportStartEffect, transform.position, transform.rotation);
        }
        
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, transform.position);
        }
        
        // Disable enemy temporarily
        var collider = GetComponent<Collider>();
        var renderer = GetComponent<SpriteRenderer>();
        
        if (collider != null) collider.enabled = false;
        if (renderer != null) renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0.3f);
        
        // Wait for effect
        yield return new WaitForSeconds(effectDuration);
        
        // Calculate teleport position
        Vector3 teleportPosition = CalculateTeleportPosition();
        
        // Execute teleport
        transform.position = teleportPosition;
        
        // Play end effect
        if (teleportEndEffect != null)
        {
            Instantiate(teleportEndEffect, transform.position, transform.rotation);
        }
        
        // Re-enable enemy
        if (collider != null) collider.enabled = true;
        if (renderer != null) renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
        
        isTeleporting = false;
        
        // Auto deactivate if enabled
        if (autoDeactivate)
        {
            yield return new WaitForSeconds(duration - effectDuration);
            Deactivate();
        }
        
        Debug.Log($"[TeleportMovement] {gameObject.name} teleported to {teleportPosition}");
    }
    
    private Vector3 CalculateTeleportPosition()
    {
        Vector3 targetPosition = transform.position;
        
        if (playerTarget == null)
        {
            return targetPosition;
        }
        
        // Strategy 1: Teleport behind player
        if (teleportBehindPlayer)
        {
            Vector3 playerPosition = playerTarget.position;
            Vector3 playerForward = playerTarget.forward;
            Vector3 behindPlayer = playerPosition - playerForward * behindPlayerDistance;
            
            if (IsValidTeleportPosition(behindPlayer))
            {
                return behindPlayer;
            }
        }
        
        // Strategy 2: Find random positions around player
        for (int attempt = 0; attempt < maxTeleportAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * teleportRange;
            Vector3 candidatePosition = playerTarget.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            if (IsValidTeleportPosition(candidatePosition))
            {
                return candidatePosition;
            }
        }
        
        // Strategy 3: Teleport to safe distance from player
        Vector3 directionAway = (transform.position - playerTarget.position).normalized;
        Vector3 safePosition = playerTarget.position + directionAway * teleportRange * 0.7f;
        
        if (IsValidTeleportPosition(safePosition))
        {
            return safePosition;
        }
        
        // Fallback: stay in place
        return transform.position;
    }
    
    private bool IsValidTeleportPosition(Vector3 position)
    {
        // Check if position is on NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            position = hit.position;
        }
        else
        {
            return false;
        }
        
        // Check for obstacles
        Collider[] obstacles = Physics.OverlapSphere(position, 1f, obstacleLayerMask);
        if (obstacles.Length > 0)
        {
            return false;
        }
        
        // Check distance from player (shouldn't be too close)
        if (playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(position, playerTarget.position);
            if (distanceToPlayer < 2f) // Too close to player
            {
                return false;
            }
        }
        
        return true;
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Auto-deactivate when duration expires
        if (_isActive && autoDeactivate && Time.time >= _activationEndTime)
        {
            Deactivate();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw teleport range
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, teleportRange);
        
        // Draw behind player position if applicable
        if (playerTarget != null && teleportBehindPlayer)
        {
            Vector3 behindPos = playerTarget.position - playerTarget.forward * behindPlayerDistance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(behindPos, 0.5f);
            Gizmos.DrawLine(playerTarget.position, behindPos);
        }
        
        // Draw cooldown status
        if (Application.isPlaying)
        {
            Gizmos.color = CanActivate() ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }
    
    [ContextMenu("🚀 Test Teleport")]
    public void TestTeleport()
    {
        if (CanActivate())
        {
            Activate();
        }
        else
        {
            Debug.Log($"[TeleportMovement] Cannot teleport - Cooldown: {CooldownRemaining:F1}s, Active: {IsActive}");
        }
    }
    
    protected override bool OnCanActivate()
    {
        // Cần có target để teleport
        playerTarget = GetValidTarget();
        return playerTarget != null;
    }
    
    protected override void OnActivate()
    {
        Debug.Log($"[TeleportMovement] {gameObject.name} activating teleport");
        StartCoroutine(ExecuteTeleport());
    }
    
    protected override void OnDeactivate()
    {
        isTeleporting = false;
        Debug.Log($"[TeleportMovement] {gameObject.name} teleport deactivated");
    }
}