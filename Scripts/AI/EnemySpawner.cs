using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemy Spawner - updated for new unified system
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Prefab kẻ địch cần spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Thời gian giữa các lần spawn")]
    public float spawnInterval = 5f;

    [Tooltip("Số lượng kẻ địch tối đa được phép tồn tại từ spawner này")]
    public int maxEnemies = 5;

    [Tooltip("Các điểm spawn")]
    public Transform[] spawnPoints;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnEnemiesRoutine());
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Clean up destroyed enemies
            spawnedEnemies.RemoveAll(e => e == null);

            if (spawnedEnemies.Count < maxEnemies)
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    continue;
                }

                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);

                // The new system auto-handles everything via CoreEnemy
                // Just ensure the enemy has the required components
                EnsureEnemyComponents(enemy);
            }
        }
    }

    private void EnsureEnemyComponents(GameObject enemy)
    {
        // Ensure essential components exist
        if (enemy.GetComponent<Character>() == null)
            enemy.AddComponent<Character>();

        if (enemy.GetComponent<CoreEnemy>() == null)
            enemy.AddComponent<CoreEnemy>();

        if (enemy.GetComponent<EnemyType>() == null)
            enemy.AddComponent<EnemyType>();

        if (enemy.GetComponent<EnemySkillManager>() == null)
            enemy.AddComponent<EnemySkillManager>();
    }
}
