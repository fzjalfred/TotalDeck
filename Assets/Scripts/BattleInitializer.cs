using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Initial battle setup. Spawns starting regiments for both player and enemy.
    /// Also creates the battlefield ground plane if none exists.
    /// </summary>
    public class BattleInitializer : MonoBehaviour
    {
        [Header("Starting Positions")]
        public Vector3 playerStartPos = new Vector3(0f, 0f, 15f);
        public Vector3 enemyStartPos = new Vector3(0f, 0f, -15f);

        [Header("Enemy Spawn")]
        public Vector3 enemySpawnPos = new Vector3(0f, 0f, -20f);

        void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[BattleInitializer] GameManager not found! Make sure it's in the scene.");
                return;
            }

            // Set the enemy spawn point on GameManager
            // We store it as a reference point for reinforcements
            GameManager.Instance.enemySpawnPoint = new GameObject("EnemySpawnPoint").transform;
            GameManager.Instance.enemySpawnPoint.position = enemySpawnPos;

            // Spawn initial regiments
            GameManager.Instance.SpawnRegiment(playerStartPos, Team.Player, 0);
            GameManager.Instance.SpawnRegiment(enemyStartPos, Team.Enemy, 0);
        }
    }
}
