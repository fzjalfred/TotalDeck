using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Procedurally creates the soldier prefab at runtime if not assigned.
    /// A soldier is a simple capsule with Soldier component and collider.
    /// </summary>
    public class SoldierPrefabFactory : MonoBehaviour
    {
        public static GameObject CreateSoldierPrefab()
        {
            GameObject soldier = new GameObject("SoldierPrefab");

            // Capsule mesh renderer
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(soldier.transform);
            capsule.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            capsule.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // Collider for OverlapSphere detection
            CapsuleCollider collider = capsule.GetComponent<CapsuleCollider>();
            collider.radius = 0.35f;
            collider.height = 1f;

            // Soldier component
            soldier.AddComponent<Soldier>();

            // Mark as don't destroy (will be used as prefab template)
            soldier.SetActive(false);
            soldier.hideFlags = HideFlags.HideInHierarchy;

            return soldier;
        }
    }
}
