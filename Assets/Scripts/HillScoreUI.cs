using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// King of the Hill scoreboard:
    /// 【01|023】【03|023】
    ///  big number  = side's current score (2 digits, leading zero)
    ///  small number = side's soldiers inside the hill right now (3 digits)
    /// Left bracket = player (blue), right bracket = enemy (red).
    /// </summary>
    public class HillScoreUI : MonoBehaviour
    {
        public Text playerScoreText;
        public Text playerCountText;
        public Text enemyScoreText;
        public Text enemyCountText;

        [Header("Scoring Window Progress Bar")]
        public Image progressFill;

        HillZone hill;
        bool victoryShown;

        void Start()
        {
            hill = HillZone.Instance;
            if (hill != null)
                hill.OnVictory += OnVictory;
        }

        void OnDestroy()
        {
            if (hill != null)
                hill.OnVictory -= OnVictory;
        }

        void Update()
        {
            if (hill == null) return;

            if (playerScoreText != null)
                playerScoreText.text = hill.PlayerScore.ToString("00");
            if (playerCountText != null)
                playerCountText.text = hill.PlayerInHill.ToString("000");
            if (enemyScoreText != null)
                enemyScoreText.text = hill.EnemyScore.ToString("00");
            if (enemyCountText != null)
                enemyCountText.text = hill.EnemyInHill.ToString("000");

            UpdateProgressBar();
        }

        /// <summary>
        /// Fill the bar left -> right over one scoring window (10s). The fill
        /// color follows the CURRENT hill majority in real time — the bar
        /// recolors the instant a side takes the lead inside the circle.
        /// Implemented with a plain stretched rect (anchorMax.x = progress):
        /// Tuanjie does not crop Image.Type.Filled for sprite-less images,
        /// so fillAmount renders as a full-width bar — anchors always work.
        /// </summary>
        void UpdateProgressBar()
        {
            if (progressFill == null) return;

            float progress = hill.ScoreProgress;

            // Real-time ownership color
            Color owner = hill.PlayerInHill > hill.EnemyInHill
                ? new Color(0.3f, 0.67f, 0.97f)   // player blue
                : hill.EnemyInHill > hill.PlayerInHill
                    ? new Color(1f, 0.42f, 0.42f) // enemy red
                    : new Color(0.85f, 0.85f, 0.85f); // neutral
            progressFill.color = owner;

            // Physically stretch the fill rect from the left edge
            var rt = progressFill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(progress, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        void OnVictory(Team winner)
        {
            if (victoryShown || playerScoreText == null || enemyScoreText == null) return;
            victoryShown = true;
            string msg = winner == Team.Player ? "VICTORY!" : "DEFEAT";
            Color c = winner == Team.Player
                ? new Color(0f, 1f, 0.67f)
                : new Color(1f, 0.3f, 0.3f);

            // Reuse the scoreboard texts to show the result banner
            playerScoreText.text = msg;
            playerScoreText.fontSize = 40;
            playerScoreText.color = c;
            enemyScoreText.text = msg;
            enemyScoreText.fontSize = 40;
            enemyScoreText.color = c;
        }
    }
}
