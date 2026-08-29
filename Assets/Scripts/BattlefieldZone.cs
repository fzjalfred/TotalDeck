using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Marks the player/enemy deployment zones with a visible dividing line
    /// and zone labels during the Planning phase.
    /// </summary>
    public class BattlefieldZone : MonoBehaviour
    {
        [Header("Zone Division")]
        public Transform zoneDivider;
        public float zoneLineWidth = 0.2f;
        public float zoneLineLength = 40f;
        public Color zoneLineColor = new Color(1f, 1f, 1f, 0.4f);

        [Header("Labels")]
        public GameObject playerZoneLabel;
        public GameObject enemyZoneLabel;

        private LineRenderer lineRenderer;

        void Start()
        {
            CreateZoneDivider();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            }

            UpdateVisibility(GamePhase.Planning);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }
        }

        void CreateZoneDivider()
        {
            GameObject lineObj = new GameObject("ZoneDivider");
            lineObj.transform.SetParent(transform);
            lineObj.transform.position = Vector3.zero;
            lineObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = zoneLineWidth;
            lineRenderer.endWidth = zoneLineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = zoneLineColor;
            lineRenderer.endColor = zoneLineColor;

            lineRenderer.SetPosition(0, new Vector3(-zoneLineLength * 0.5f, 0.1f, 0f));
            lineRenderer.SetPosition(1, new Vector3(zoneLineLength * 0.5f, 0.1f, 0f));

            // No texture assigned — Tile mode would render tiling artifacts
            zoneDivider = lineObj.transform;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            UpdateVisibility(phase);
        }

        void UpdateVisibility(GamePhase phase)
        {
            bool show = phase == GamePhase.Planning;
            if (zoneDivider != null)
                zoneDivider.gameObject.SetActive(show);
            if (playerZoneLabel != null)
                playerZoneLabel.SetActive(show);
            if (enemyZoneLabel != null)
                enemyZoneLabel.SetActive(show);
        }
    }
}
