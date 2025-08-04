using UnityEngine;
using System.Collections;

/// <summary>
/// ? DASH ATTACK MOVEMENT - Special Movement cho Boss
/// Cho phép boss lao nhanh v? phía

public class DashAttackMovement : MonoBehaviour
{
    public float dashSpeed;
    public float dashDuration;
    public int finalDamage;

    private void PerformDashAttack()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            GameObject enemy = hit.collider.gameObject;

            // Trigger enemy events
            if (enemy != null)
            {
                enemy.GetComponent<Enemy>().TriggerDealDamageEvent(gameObject, finalDamage);
            }
        }
    }
}