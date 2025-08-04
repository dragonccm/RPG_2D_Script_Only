using UnityEngine;
using System.Collections;

/// <summary>
/// ? DASH ATTACK MOVEMENT - Special Movement cho Boss
/// Cho ph�p boss lao nhanh v? ph�a

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