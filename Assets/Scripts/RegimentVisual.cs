using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Visual feedback for regiments: soldier highlight on selection,
    /// move path visualization, health bar, and troop count.
    /// </summary>
    [RequireComponent(typeof(Regiment))]
    public class RegimentVisual : MonoBehaviour
    {
        [Header("Path Visualization")]
        public Color pathColor = new Color(0f, 1f, 0.67f, 0.6f);
        public Color pathEndColor = new Color(0f, 1f, 0.67f, 1f);

        [Header("Health Bar")]
        public Transform healthBarFill;
        public Transform healthBarBackground;
        public TextMesh troopCountText;

        private Regiment regiment;
        private bool wasSelected;
        private LineRenderer pathLine;
        private GameObject moveTargetMarker;

        void Start()
        {
            regiment = GetComponent<Regiment>();
            CreatePathLine();
            CreateMoveTargetMarker();
            if (healthBarFill == null)
                CreateHealthBar();
        }

        void Update()
        {
            if (regiment == null) return;

            bool selected = regiment.IsSelected();
            if (selected != wasSelected)
            {
                UpdateSoldierHighlight(selected);
                wasSelected = selected;
            }

            UpdateHealthBar();
            UpdatePathVisualization(selected);

            if (troopCountText != null)
                troopCountText.text = regiment.AliveCount + "/" + GameConfig.RegimentSize;
        }

        void UpdateSoldierHighlight(bool selected)
        {
            foreach (var s in regiment.Soldiers)
            {
                if (s == null || !s.gameObject.activeSelf) continue;
                s.SetHighlight(selected);
            }
        }

        void UpdateHealthBar()
        {
            if (healthBarFill == null) return;
            float ratio = (float)regiment.AliveCount / GameConfig.RegimentSize;
            healthBarFill.localScale = new Vector3(ratio, 1f, 1f);

            Vector3 barPos = transform.position + Vector3.up * 3.5f;
            if (healthBarBackground != null)
                healthBarBackground.position = barPos;
            // Keep fill 0.05 toward the camera (+Z) so it never z-fights the BG
            healthBarFill.position = barPos + new Vector3(-(1f - ratio) * 0.5f, 0f, 0.05f);
        }

        void UpdatePathVisualization(bool selected)
        {
            if (pathLine == null) return;

            Vector3[] path = regiment.GetMovePath();
            bool showPath = selected && path.Length >= 2;

            if (!showPath)
            {
                pathLine.enabled = false;
                if (moveTargetMarker != null)
                    moveTargetMarker.SetActive(false);
                return;
            }

            pathLine.enabled = true;
            pathLine.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 pos = path[i];
                pos.y = 0.1f;
                pathLine.SetPosition(i, pos);
            }

            // Show marker at final destination
            Vector3 dest = path[path.Length - 1];
            dest.y = 0.2f;
            if (moveTargetMarker != null)
            {
                moveTargetMarker.SetActive(true);
                moveTargetMarker.transform.position = dest;
            }
        }

        void CreatePathLine()
        {
            GameObject lineObj = new GameObject("PathLine");
            lineObj.transform.SetParent(transform);
            pathLine = lineObj.AddComponent<LineRenderer>();
            pathLine.positionCount = 0;
            pathLine.widthMultiplier = 0.15f;
            pathLine.useWorldSpace = true;
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = pathColor;
            pathLine.endColor = pathEndColor;
            pathLine.enabled = false;
        }

        void CreateMoveTargetMarker()
        {
            moveTargetMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moveTargetMarker.name = "MoveTarget";
            moveTargetMarker.transform.localScale = Vector3.one * 0.5f;
            moveTargetMarker.GetComponent<Renderer>().material.color = pathEndColor;
            Destroy(moveTargetMarker.GetComponent<Collider>());
            moveTargetMarker.SetActive(false);
        }

        void CreateHealthBar()
        {
            // Background — Sprites/Default honors alpha; Standard would render opaque black
            GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgObj.name = "HealthBarBG";
            Destroy(bgObj.GetComponent<Collider>());
            bgObj.transform.SetParent(transform);
            bgObj.transform.localScale = new Vector3(1.2f, 0.12f, 1f);
            Material bgMat = new Material(Shader.Find("Sprites/Default"));
            bgMat.color = new Color(0f, 0f, 0f, 0.6f);
            bgObj.GetComponent<Renderer>().material = bgMat;
            healthBarBackground = bgObj.transform;

            // Fill — drawn 0.05 in front of the BG (toward camera at +Z)
            GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillObj.name = "HealthBarFill";
            Destroy(fillObj.GetComponent<Collider>());
            fillObj.transform.SetParent(bgObj.transform);
            fillObj.transform.localScale = new Vector3(1f, 1f, 0.1f);
            Material fillMat = new Material(Shader.Find("Sprites/Default"));
            fillMat.color = regiment.Team == Team.Player
                ? new Color(0.13f, 0.72f, 0.81f)
                : new Color(0.98f, 0.32f, 0.32f);
            fillObj.GetComponent<Renderer>().material = fillMat;
            healthBarFill = fillObj.transform;
        }
    }
}
