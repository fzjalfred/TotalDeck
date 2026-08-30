using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Obsolete: starting deployments moved into GameManager.StartNewGame /
    /// DeployStartingRegiments, driven by MapDef spawn slots and the menu's
    /// spawn assignment. Kept as a no-op so old scene objects don't error.
    /// </summary>
    public class BattleInitializer : MonoBehaviour
    {
        [Header("Obsolete — deployments are map-driven now")]
        public Vector3 playerStartPos = new Vector3(0f, 0f, 30f);
        public Vector3 enemyStartPos = new Vector3(0f, 0f, -30f);

        [Header("Enemy Spawn")]
        public Vector3 enemySpawnPos = new Vector3(0f, 0f, -40f);
    }
}
