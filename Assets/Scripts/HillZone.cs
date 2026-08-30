using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// King of the Hill zone: a large circle at map center. Counts each
    /// side's soldiers inside every frame, awards one point every
    /// HillScoreInterval seconds to the side with the most soldiers inside.
    /// First side to reach HillScoreToWin wins the game.
    /// </summary>
    public class HillZone : MonoBehaviour
    {
        public static HillZone Instance { get; private set; }

        [Header("Settings")]
        public Vector3 center = Vector3.zero;
        public float radius = GameConfig.HillRadius;

        [Header("State (read-only)")]
        public int PlayerScore;
        public int EnemyScore;
        public int PlayerInHill;
        public int EnemyInHill;

        float scoreTimer = GameConfig.HillScoreInterval;
        bool gameEnded;

        /// <summary>Progress of the current scoring window, 0..1 (1 = about to settle).</summary>
        public float ScoreProgress => 1f - Mathf.Clamp01(scoreTimer / GameConfig.HillScoreInterval);

        /// <summary>Raised when a side reaches the winning score. Null arg = draw impossible here.</summary>
        public event System.Action<Team> OnVictory;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            // Planning empties the bar: the next scoring cycle starts fresh
            // when Combat resumes
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Planning)
                scoreTimer = GameConfig.HillScoreInterval; // bar empties, stays paused
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        void Update()
        {
            if (gameEnded || GameManager.Instance == null) return;

            // Keep live counts for the UI, but the scoring window only runs
            // during Combat — Planning freezes the bar
            CountSoldiersInHill();

            if (GameManager.Instance.CurrentPhase == GamePhase.Combat)
                TickScoring();
        }

        void CountSoldiersInHill()
        {
            int p = 0, e = 0;
            float sqrRadius = radius * radius;

            foreach (var reg in GameManager.Instance.AllRegiments)
            {
                if (reg == null || reg.AliveCount == 0) continue;
                foreach (var s in reg.Soldiers)
                {
                    if (s == null || !s.gameObject.activeSelf) continue;
                    float sqr = (s.transform.position - center).sqrMagnitude;
                    if (sqr > sqrRadius) continue;
                    if (s.Team == Team.Player) p++;
                    else e++;
                }
            }

            PlayerInHill = p;
            EnemyInHill = e;
        }

        void TickScoring()
        {
            scoreTimer -= Time.deltaTime;
            if (scoreTimer > 0f) return;
            scoreTimer += GameConfig.HillScoreInterval;

            // Majority owner scores; empty hill or a tie scores nobody
            if (PlayerInHill > EnemyInHill) PlayerScore++;
            else if (EnemyInHill > PlayerInHill) EnemyScore++;

            if (PlayerScore >= GameConfig.HillScoreToWin) EndGame(Team.Player);
            else if (EnemyScore >= GameConfig.HillScoreToWin) EndGame(Team.Enemy);
        }

        void EndGame(Team winner)
        {
            if (gameEnded) return;
            gameEnded = true;
            Debug.Log($"[HillZone] VICTORY — {winner} wins {PlayerScore}:{EnemyScore}!");
            OnVictory?.Invoke(winner);
            GameManager.Instance?.EndGame(winner); // freeze field + results screen
        }

        /// <summary>
        /// Reset scores for a fresh game (called by GameManager.StartNewGame).
        /// </summary>
        public void ResetScores()
        {
            PlayerScore = 0;
            EnemyScore = 0;
            PlayerInHill = 0;
            EnemyInHill = 0;
            scoreTimer = GameConfig.HillScoreInterval;
            gameEnded = false;
        }

        public bool IsInHill(Vector3 worldPos)
        {
            return (worldPos - center).sqrMagnitude <= radius * radius;
        }

        // ── Visualization ─────────────────────────────────

        /// <summary>
        /// Rebuild ring + disc visuals at the current center/radius
        /// (called when a map is applied).
        /// </summary>
        public void RebuildVisual()
        {
            // Clear old visuals
            var stale = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                stale.Add(child.gameObject);
            foreach (var go in stale)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            CreateVisual();
        }

        /// <summary>
        /// Build the ring visual at runtime: a flat circle of line segments
        /// plus a translucent disc on the ground, centered on the hill.
        /// </summary>
        public void CreateVisual()
        {
            transform.position = center;

            // Ring outline
            GameObject ring = new GameObject("HillRing");
            ring.transform.SetParent(transform, false);
            LineRenderer lr = ring.AddComponent<LineRenderer>();
            const int segments = 72;
            lr.positionCount = segments + 1;
            lr.loop = true;
            lr.widthMultiplier = 0.35f;
            lr.useWorldSpace = false;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            Color ringColor = new Color(1f, 0.85f, 0.2f, 0.9f); // gold
            lr.startColor = ringColor;
            lr.endColor = ringColor;
            for (int i = 0; i <= segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.08f, Mathf.Sin(a) * radius));
            }

            // Translucent disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "HillDisc";
            var col = disc.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col);
            else if (col != null) DestroyImmediate(col);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            disc.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3); // transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            Color c = new Color(1f, 0.85f, 0.2f, 0.18f);
            mat.color = c;
            disc.GetComponent<Renderer>().material = mat;
        }
    }
}
