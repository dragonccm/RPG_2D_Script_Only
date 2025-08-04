using UnityEngine;

/// <summary>
/// Lớp điều khiển Animator cho kẻ địch, quản lý các trạng thái và tham số animation.
/// </summary>
public class EnemyAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Đặt tham số float cho Animator với safe check.
    /// </summary>
    /// <param name="paramName">Tên tham số.</param>
    /// <param name="value">Giá trị float.</param>
    public void SetFloat(string paramName, float value)
    {
        if (animator != null && HasParameter(paramName))
        {
            animator.SetFloat(paramName, value);
        }
        else if (animator != null)
        {
            Debug.LogWarning($"[EnemyAnimatorController] Parameter '{paramName}' does not exist in animator for {gameObject.name}");
        }
    }

    /// <summary>
    /// Đặt tham số boolean cho Animator với safe check.
    /// </summary>
    /// <param name="paramName">Tên tham số.</param>
    /// <param name="value">Giá trị boolean.</param>
    public void SetBool(string paramName, bool value)
    {
        if (animator != null && HasParameter(paramName))
        {
            animator.SetBool(paramName, value);
        }
        else if (animator != null)
        {
            Debug.LogWarning($"[EnemyAnimatorController] Parameter '{paramName}' does not exist in animator for {gameObject.name}");
        }
    }

    /// <summary>
    /// Kích hoạt trigger cho Animator với safe check.
    /// </summary>
    /// <param name="paramName">Tên trigger.</param>
    public void SetTrigger(string paramName)
    {
        if (animator != null && HasParameter(paramName))
        {
            animator.SetTrigger(paramName);
        }
        else if (animator != null)
        {
            Debug.LogWarning($"[EnemyAnimatorController] Trigger '{paramName}' does not exist in animator for {gameObject.name}");
        }
    }

    /// <summary>
    /// Kiểm tra parameter có tồn tại trong animator không
    /// </summary>
    /// <param name="paramName">Tên parameter</param>
    /// <returns>True nếu parameter tồn tại</returns>
    private bool HasParameter(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == paramName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Kích hoạt animation di chuyển với fallback parameters.
    /// </summary>
    /// <param name="speed">Tốc độ di chuyển.</param>
    public void PlayMoveAnimation(float speed)
    {
        // Thử các tên parameter phổ biến
        string[] speedParams = { "Speed", "speed", "MoveSpeed", "Velocity", "MovementSpeed" };

        foreach (string param in speedParams)
        {
            if (HasParameter(param))
            {
                SetFloat(param, speed);
                return;
            }
        }

        // Nếu không tìm thấy parameter nào, log warning
        Debug.LogWarning($"[EnemyAnimatorController] No speed parameter found in animator for {gameObject.name}. Available parameters: {GetAvailableParameters()}");
    }

    /// <summary>
    /// Kích hoạt animation tấn công với fallback triggers.
    /// </summary>
    public void PlayAttackAnimation()
    {
        string[] attackTriggers = { "Attack", "attack", "DoAttack", "TriggerAttack" };

        foreach (string trigger in attackTriggers)
        {
            if (HasParameter(trigger))
            {
                SetTrigger(trigger);
                return;
            }
        }

        Debug.LogWarning($"[EnemyAnimatorController] No attack trigger found in animator for {gameObject.name}. Available parameters: {GetAvailableParameters()}");
    }

    /// <summary>
    /// Kích hoạt animation chết với fallback triggers.
    /// </summary>
    public void PlayDeathAnimation()
    {
        string[] deathTriggers = { "Die", "Death", "die", "death", "Dead" };

        foreach (string trigger in deathTriggers)
        {
            if (HasParameter(trigger))
            {
                SetTrigger(trigger);
                return;
            }
        }

        Debug.LogWarning($"[EnemyAnimatorController] No death trigger found in animator for {gameObject.name}. Available parameters: {GetAvailableParameters()}");
    }

    /// <summary>
    /// Kích hoạt animation nhàn rỗi.
    /// </summary>
    public void PlayIdleAnimation()
    {
        PlayMoveAnimation(0f);
    }

    /// <summary>
    /// Lấy danh sách tất cả parameters có sẵn (để debug)
    /// </summary>
    /// <returns>String chứa tên các parameters</returns>
    private string GetAvailableParameters()
    {
        if (animator == null) return "No animator";

        var paramNames = new System.Collections.Generic.List<string>();
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            paramNames.Add($"{parameter.name}({parameter.type})");
        }

        return string.Join(", ", paramNames);
    }
}