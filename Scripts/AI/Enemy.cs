using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System;

// Explicitly use UnityEngine.Random for Unity-specific random operations
using Random = UnityEngine.Random;

/// <summary>
/// Lớp cơ sở cho tất cả kẻ địch, quản lý việc tìm kiếm mục tiêu và di chuyển cơ bản.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Auto Targeting")]
    [Tooltip("Phạm vi phát hiện ban đầu của kẻ địch. Kẻ địch sẽ tìm kiếm mục tiêu mới trong phạm vi này.")]
    public float detectionRange = 10f;
    [Tooltip("Phạm vi truy đuổi của kẻ địch. Kẻ địch sẽ tiếp tục đuổi theo mục tiêu trong phạm vi này ngay cả khi mục tiêu đã ra khỏi detectionRange.")]
    public float chaseRange = 20f;
    [Tooltip("Layer của Player để kẻ địch có thể phát hiện.")]
    public LayerMask playerLayerMask = 1 << 6; // Layer của Player

    [SerializeField] protected Transform target; // Mục tiêu hiện tại của kẻ địch (được xác định bởi Enemy.UpdateTarget)
    [SerializeField] protected NavMeshAgent agent; // NavMeshAgent để điều khiển di chuyển

    // Cache để tối ưu performance
    protected static readonly List<Transform> tempPlayerList = new List<Transform>();
    private float nextTargetUpdateTime = 0f;
    private const float TARGET_UPDATE_INTERVAL = 0.2f; // Cập nhật mục tiêu mỗi 0.2s thay vì mỗi frame

    // --- Event & Property cho hệ thống combat, buff, elite, v.v. ---
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action<Enemy, float, float> OnDamageTaken;
    public event Action<GameObject, float> OnDealDamage;
    public event Action<int> OnPhaseChanged;

    private float _currentHealth = 100f;
    private float _maxHealth = 100f;
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _defenseMultiplier = 1f;
    private float _damageReduction = 0f;
    private bool _invulnerable = false;
    public int phaseCount { get; set; } = 1;

    private float bossRange = 15f; // Giá trị ví dụ, điều chỉnh theo nhu cầu
    private float minDistance = 5f; // Giá trị ví dụ, điều chỉnh theo nhu cầu

    public float CurrentHealth { get => _currentHealth; protected set => _currentHealth = value; }
    public float MaxHealth { get => _maxHealth; protected set => _maxHealth = value; }
    public float DamageMultiplier { get => _damageMultiplier; set => _damageMultiplier = value; }
    public float SpeedMultiplier { get => _speedMultiplier; set => _speedMultiplier = value; }
    public float AttackSpeedMultiplier { get => _attackSpeedMultiplier; set => _attackSpeedMultiplier = value; }
    public float DefenseMultiplier { get => _defenseMultiplier; set => _defenseMultiplier = value; }
    public float DamageReduction { get => _damageReduction; set => _damageReduction = value; }
    public bool Invulnerable { get => _invulnerable; set => _invulnerable = value; }
    public bool IsPlayer { get; set; }
    public bool IsDead { get; set; }

    public void SetInvulnerable(bool value) { _invulnerable = value; }
    public void SetDamageMultiplier(float value) { _damageMultiplier = value; }
    public void SetSpeedMultiplier(float value) { _speedMultiplier = value; }
    public void SetAttackSpeedMultiplier(float value) { _attackSpeedMultiplier = value; }
    public void SetDefenseMultiplier(float value) { _defenseMultiplier = value; }
    public void SetDamageReduction(float value) { _damageReduction = value; }

    public float GetPower() { return 100f * _damageMultiplier * _speedMultiplier; }
    public bool NeedsHealing() { return _currentHealth < _maxHealth; }
    public float GetHealthPercent() { return _maxHealth > 0 ? _currentHealth / _maxHealth : 0f; }
    public void Heal(float amount) { _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth); OnHealthChanged?.Invoke(_currentHealth, _maxHealth); }

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false; // Tắt cập nhật xoay tự động để điều khiển bằng script
            agent.updateUpAxis = false; // Tắt cập nhật trục Y tự động
        }
        // Kích hoạt MeleeEnemyAI ngay khi Start để đảm bảo nó hoạt động
        EnsureMeleeEnemyAIActive();
    }

    protected virtual void Update()
    {
        // Debug.Log($"[{gameObject.name}] Current Target: {target?.name ?? "None"}");

        // Chỉ cập nhật mục tiêu theo khoảng thời gian để tối ưu hiệu suất
        if (Time.time >= nextTargetUpdateTime)
        {
            UpdateTarget();
            nextTargetUpdateTime = Time.time + TARGET_UPDATE_INTERVAL;
        }

        // Logic di chuyển được gọi ở class con hoặc xử lý trực tiếp tại đây
        HandleMovement();
    }

    /// <summary>
    /// Cập nhật mục tiêu chính của kẻ địch. Đây là logic tập trung để xác định mục tiêu.
    /// </summary>
    protected virtual void UpdateTarget()
    {
        Transform bestCandidateTarget = null;
        float bestCandidateDistance = float.MaxValue;

        // 1. Ưu tiên target hiện tại nếu nó vẫn trong tầm chaseRange
        if (target != null && Vector3.Distance(transform.position, target.position) <= chaseRange)
        {
            bestCandidateTarget = target;
            bestCandidateDistance = Vector3.Distance(transform.position, target.position);
            // Debug.Log($"[Enemy-UpdateTarget] Keeping current target: {bestCandidateTarget.name} at distance {bestCandidateDistance:F2}");
        }

        // 2. Tìm tất cả players trong detectionRange
        tempPlayerList.Clear();
        var colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                tempPlayerList.Add(col.transform);
            }
        }
        // Debug.Log($"[Enemy-UpdateTarget] Found {tempPlayerList.Count} players in detection range.");

        // 3. Get closest player from detected list
        Transform closestPlayer = GetClosestTransform(tempPlayerList); // Helper method để lấy transform gần nhất
        if (closestPlayer != null)
        {
            float distToClosestPlayer = Vector3.Distance(transform.position, closestPlayer.position);
            // Debug.Log($"[Enemy-UpdateTarget] Closest Player: {closestPlayer.name} at distance {distToClosestPlayer:F2}");

            if (bestCandidateTarget == null || distToClosestPlayer < bestCandidateDistance)
            {
                bestCandidateTarget = closestPlayer;
                bestCandidateDistance = distToClosestPlayer;
                // Debug.Log($"[Enemy-UpdateTarget] New best target candidate: {bestCandidateTarget.name}");
            }
        }

        // 4. Cập nhật target chính của Enemy
        target = bestCandidateTarget;

        // 5. Nếu target hiện tại đã ra khỏi chaseRange, đặt target về null
        if (target != null && Vector3.Distance(transform.position, target.position) > chaseRange)
        {
            // Debug.Log($"[Enemy-UpdateTarget] Target {target.name} ({Vector3.Distance(transform.position, target.position):F2}m) out of chase range ({chaseRange}m). Setting target to null.");
            target = null;
        }

        // 6. Cập nhật playerTarget cho EnemyAIController và chuyển trạng thái
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.playerTarget = target; // Luôn đồng bộ playerTarget với target chính của Enemy

            if (target != null)
            {
                var attackController = GetComponent<EnemyAttackController>();
                float currentDistance = Vector3.Distance(transform.position, target.position);

                if (attackController != null && currentDistance <= attackController.AttackRange)
                {
                    // Debug.Log($"[Enemy-UpdateTarget] Target {target.name} ({currentDistance:F2}m) in attack range ({attackController.AttackRange}m). Changing to AttackState.");
                    aiController.ChangeState(aiController.attackState);
                }
                else if (currentDistance <= chaseRange)
                {
                    // Debug.Log($"[Enemy-UpdateTarget] Target {target.name} ({currentDistance:F2}m) in chase range ({chaseRange}m). Changing to ChaseState.");
                    aiController.ChangeState(aiController.chaseState);
                }
                else
                {
                    // Trường hợp này chỉ xảy ra nếu target không null nhưng đã ra ngoài chaseRange
                    // và chưa kịp được đặt về null ở bước 5.
                    // Debug.Log($"[Enemy-UpdateTarget] Target {target.name} ({currentDistance:F2}m) outside chase range ({chaseRange}m). Changing to IdleState (fallback).");
                    aiController.ChangeState(aiController.idleState);
                }
            }
            else
            {
                // Nếu không có mục tiêu, chuyển trạng thái AI sang nhàn rỗi hoặc tuần tra
                // Debug.Log($"[Enemy-UpdateTarget] No target found. Changing to IdleState.");
                aiController.ChangeState(aiController.idleState);
            }
        }
    }

    /// <summary>
    /// Phương thức trợ giúp để tìm Transform gần nhất trong một danh sách.
    /// </summary>
    protected virtual Transform GetClosestTransform(List<Transform> transforms)
    {
        if (transforms == null || transforms.Count == 0) return null;

        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var t in transforms)
        {
            if (t == null) continue; // Đảm bảo transform không null
            float distance = Vector3.Distance(transform.position, t.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = t;
            }
        }
        return closest;
    }

    protected virtual void HandleMovement()
    {
        if (agent == null)
        {
            Debug.LogWarning("NavMeshAgent is not assigned to Enemy: " + gameObject.name, this);
            return;
        }

        if (target != null)
        {
            // Di chuyển đến vị trí của mục tiêu
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else
        {
            // Nếu không có mục tiêu, dừng di chuyển
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            agent.isStopped = true;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Vẽ vùng detection (phát hiện mục tiêu ban đầu)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Màu xanh lá cây
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vẽ vùng chase (truy đuổi)
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Màu xanh dương
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Vẽ vùng attack (nếu có EnemyAttackController)
        var attackController = GetComponent<EnemyAttackController>();
        if (attackController != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Màu đỏ
            Gizmos.DrawWireSphere(transform.position, attackController.AttackRange);
        }
    }

    // --- Bổ sung method nhận sát thương và chết cho Enemy ---
    public virtual void TakeDamage(float damage)
    {
        var character = GetComponent<Character>();
        if (character != null)
        {
            character.TakeDamage(damage);
        }
        OnDamageTaken?.Invoke(this, damage, _currentHealth); // Kích hoạt sự kiện OnDamageTaken
    }

    public virtual void Die()
    {
        // Trigger hiệu ứng chết, animation, v.v.
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            // Chuyển trạng thái AI sang DeadState khi kẻ địch chết
            aiController.ChangeState(aiController.chaseState); // Sử dụng deadState đã được khởi tạo
        }
        // Có thể mở rộng: phát hiệu ứng, âm thanh, v.v.
        OnDeath?.Invoke(); // Kích hoạt sự kiện OnDeath
        Destroy(gameObject); // Hủy GameObject của kẻ địch
    }

    // Các phương thức cài đặt thuộc tính
    public void SetMaxHealthMultiplier(float m) { _maxHealth *= m; }
    public void SetExperienceMultiplier(float m) { /* TODO: Triển khai logic tăng kinh nghiệm */ }
    public void SetCurrencyMultiplier(float m) { /* TODO: Triển khai logic tăng tiền tệ */ }
    public void SetItemDropChanceMultiplier(float m) { /* TODO: Triển khai logic tăng tỷ lệ rơi đồ */ }
    public void SetNamePrefix(string prefix) { gameObject.name = prefix + gameObject.name; }
    public void SetSummoned(bool value) { /* TODO: Triển khai logic cho kẻ địch được triệu hồi */ }

    public float BossRange { get => bossRange; set => bossRange = value; }
    public float MinDistance { get => minDistance; set => minDistance = value; }

    // Đảm bảo script MeleeEnemyAI luôn active khi spawn
    protected void EnsureMeleeEnemyAIActive()
    {
        var meleeAi = GetComponent<MeleeEnemyAI>();
        if (meleeAi != null)
        {
            meleeAi.enabled = true; // Luôn kích hoạt script MeleeEnemyAI
        }
    }
}

// Interface ví dụ cho các đối tượng có thể nhận sát thương
public interface IDamageable
{
    void TakeDamage(float damage);
}

// Các lớp ví dụ khác (có thể không có trong các file bạn cung cấp, nhưng được giữ lại để tránh lỗi tham chiếu)
public class PlayerThreatManager { }
