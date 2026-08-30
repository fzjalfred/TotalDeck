using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TotalDeck
{
    /// <summary>
    /// Manages all UI: phase/timer display, treasury & economy info,
    /// draw card button, hand cards rendering, and skip-to-combat button.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Phase & Timer")]
        public Text phaseText;
        public Text timerText;
        public Image phaseBadge;

        [Header("Economy")]
        public Text treasuryText;
        public Text incomeText;
        public Text upkeepText;
        public Text balanceText;

        [Header("Draw Card")]
        public Button drawButton;
        public Text drawCostText;

        [Header("Combat Controls")]
        public Button skipButton;

        [Header("Hand")]
        public Transform handContainer;
        public GameObject cardPrefab;

        [Header("Bottom Panel")]
        public GameObject bottomPanel;

        [Header("Colors")]
        public Color planningColor = new Color(0.17f, 0.36f, 0.56f);
        public Color combatColor = new Color(0.56f, 0.17f, 0.17f);
        public Color positiveColor = new Color(0f, 1f, 0.67f);
        public Color negativeColor = new Color(1f, 0.42f, 0.42f);

        private List<GameObject> cardUIPool = new List<GameObject>();

        void Start()
        {
            // Wire up buttons
            if (drawButton != null)
                drawButton.onClick.AddListener(OnDrawCardClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);

            // Subscribe to events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnEconomyChanged += UpdateEconomyUI;
            }

            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnHandChanged += RebuildHandWrapper;
                CardManager.Instance.OnCardSelected += OnCardSelected;
            }

            UpdateAllUI();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnEconomyChanged -= UpdateEconomyUI;
            }
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnHandChanged -= RebuildHandWrapper;
                CardManager.Instance.OnCardSelected -= OnCardSelected;
            }
        }

        void Update()
        {
            // Update timer every frame
            if (timerText != null && GameManager.Instance != null)
            {
                timerText.text = Mathf.CeilToInt(GameManager.Instance.PhaseTimer) + "s";
            }
        }

        void OnPhaseChanged(GamePhase phase)
        {
            UpdateAllUI();
        }

        void UpdateAllUI()
        {
            UpdatePhaseUI();
            UpdateEconomyUI();
            RebuildHand(CardManager.Instance != null ? CardManager.Instance.SelectedCard : null);
            UpdatePanelVisibility();
        }

        void UpdatePhaseUI()
        {
            if (GameManager.Instance == null) return;

            GamePhase phase = GameManager.Instance.CurrentPhase;

            if (phaseText != null)
            {
                if (phase == GamePhase.Planning)
                    phaseText.text = "Planning Phase (Turn " + GameManager.Instance.TurnCount + ")";
                else
                    phaseText.text = "Combat Phase (Earning Bounty...)";
            }

            if (phaseBadge != null)
            {
                phaseBadge.color = phase == GamePhase.Planning ? planningColor : combatColor;
            }

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(phase == GamePhase.Planning);
                skipButton.interactable = phase == GamePhase.Planning;
            }
        }

        void UpdateEconomyUI()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            if (treasuryText != null)
                treasuryText.text = "$" + gm.Treasury;

            if (incomeText != null)
                incomeText.text = "$" + gm.TotalIncome;

            if (upkeepText != null)
                upkeepText.text = gm.TotalUpkeep.ToString();

            if (balanceText != null)
            {
                int net = gm.NetBalance;
                if (net >= 0)
                {
                    balanceText.text = "Balance: +$" + net;
                    balanceText.color = positiveColor;
                }
                else
                {
                    balanceText.text = "Deficit: -$" + Mathf.Abs(net);
                    balanceText.color = negativeColor;
                }
            }

            // Draw button
            if (drawCostText != null)
                drawCostText.text = "$" + gm.CurrentDrawCost;

            if (drawButton != null)
            {
                bool canDraw = gm.CurrentPhase == GamePhase.Planning && gm.Treasury >= gm.CurrentDrawCost;
                drawButton.interactable = canDraw;
            }
        }

        void UpdatePanelVisibility()
        {
            if (bottomPanel == null || GameManager.Instance == null) return;
            bottomPanel.SetActive(GameManager.Instance.CurrentPhase == GamePhase.Planning);
        }

        void RebuildHandWrapper()
        {
            RebuildHand(CardManager.Instance != null ? CardManager.Instance.SelectedCard : null);
        }

        void RebuildHand(CardData selectedCard)
        {
            if (handContainer == null || cardPrefab == null) return;
            if (CardManager.Instance == null) return;

            // Clear existing
            foreach (var obj in cardUIPool)
            {
                if (obj != null) Destroy(obj);
            }
            cardUIPool.Clear();

            foreach (var card in CardManager.Instance.Hand)
            {
                GameObject cardObj = Instantiate(cardPrefab, handContainer);
                cardObj.SetActive(true);

                // Set card visuals
                CardUIElement cardUI = cardObj.GetComponent<CardUIElement>();
                if (cardUI == null)
                    cardUI = cardObj.AddComponent<CardUIElement>();

                cardUI.Setup(card, card == CardManager.Instance.SelectedCard);

                // Wire click event
                Button cardBtn = cardObj.GetComponent<Button>();
                if (cardBtn == null)
                    cardBtn = cardObj.AddComponent<Button>();
                CardData captured = card;
                cardBtn.onClick.AddListener(() => OnCardClicked(captured));

                cardUIPool.Add(cardObj);
            }

            UpdateEconomyUI();
        }

        void OnCardClicked(CardData card)
        {
            if (CardManager.Instance != null)
                CardManager.Instance.SelectCard(card);
        }

        void OnCardSelected(CardData card)
        {
            RebuildHand(card);
        }

        void OnDrawCardClicked()
        {
            if (CardManager.Instance != null)
                CardManager.Instance.DrawCard();
        }

        void OnSkipClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SwitchPhase();
        }
    }
}
