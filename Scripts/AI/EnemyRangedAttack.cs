using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    private float cooldownMultiplier = 1f;

    // Thi?t l?p h? s? cooldown (d�ng cho Elite/Frenzy)
    public void SetCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = multiplier;
    }

    // This class is for ranged attacks.

    // C� th? m? r?ng th�m logic t?n c�ng t?m xa ? ?�y
}