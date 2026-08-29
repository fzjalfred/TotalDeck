using System;
using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Manages the player's hand, card drawing with scaling cost,
    /// and playing cards (deploying units or casting spells).
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }

        [Header("Card Pool")]
        public CardData[] cardPool;

        [Header("Initial Hand")]
        public CardData[] startingHand;

        // ── Runtime State ──────────────────────────────────
        public List<CardData> Hand { get; } = new List<CardData>();
        public CardData SelectedCard { get; private set; }

        // ── Events ─────────────────────────────────────────
        public event Action OnHandChanged;
        public event Action<CardData> OnCardSelected;

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
            // Populate starting hand with runtime copies
            Hand.Clear();
            if (startingHand != null)
            {
                foreach (var card in startingHand)
                {
                    if (card != null)
                        Hand.Add(card.CreateRuntimeCopy());
                }
            }
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Draw a card from the pool. Cost scales with each draw this planning phase.
        /// </summary>
        public bool DrawCard()
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Planning)
                return false;

            int cost = GameManager.Instance.CurrentDrawCost;
            if (!GameManager.Instance.SpendTreasury(cost))
                return false;

            // Weighted random from card pool
            CardData template = PickRandomCard();
            if (template == null) return false;

            Hand.Add(template.CreateRuntimeCopy());
            GameManager.Instance.IncrementDrawCost();

            OnHandChanged?.Invoke();
            return true;
        }

        CardData PickRandomCard()
        {
            if (cardPool == null || cardPool.Length == 0) return null;
            return cardPool[UnityEngine.Random.Range(0, cardPool.Length)];
        }

        /// <summary>
        /// Select or deselect a card in hand.
        /// </summary>
        public void SelectCard(CardData card)
        {
            if (card == null)
            {
                SelectedCard = null;
            }
            else if (SelectedCard == card)
            {
                SelectedCard = null;
            }
            else
            {
                if (GameManager.Instance.Treasury < card.playCost)
                    return;
                SelectedCard = card;
            }
            OnCardSelected?.Invoke(SelectedCard);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Play the currently selected card at the given world position.
        /// Called by RTSInputController when clicking on the battlefield.
        /// </summary>
        public bool PlayCardAt(Vector3 worldPos)
        {
            if (SelectedCard == null) return false;
            if (GameManager.Instance.CurrentPhase != GamePhase.Planning) return false;
            if (!GameManager.Instance.SpendTreasury(SelectedCard.playCost)) return false;

            if (SelectedCard.cardType == CardType.Unit)
            {
                GameManager.Instance.SpawnRegiment(worldPos, Team.Player, SelectedCard.prefabIndex);
            }
            else if (SelectedCard.cardType == CardType.Spell)
            {
                Regiment target = FindRegimentAt(worldPos, Team.Player);
                if (target != null)
                {
                    ApplySpellToRegiment(SelectedCard, target);
                }
                else
                {
                    // Refund if no valid target
                    GameManager.Instance.SpendTreasury(-SelectedCard.playCost);
                    return false;
                }
            }

            Hand.Remove(SelectedCard);
            SelectedCard = null;
            OnHandChanged?.Invoke();
            return true;
        }

        void ApplySpellToRegiment(CardData spell, Regiment target)
        {
            if (spell.healAmount > 0)
            {
                target.ModifySoldiers(spell.healAmount);
            }
            if (spell.damageAmount > 0)
            {
                target.DamageAllSoldiers(spell.damageAmount);
            }
            if (spell.attackBuff != 0f || spell.hpBuff != 0f || spell.speedBuff != 0f)
            {
                target.ApplyBuff(spell.attackBuff, spell.hpBuff, spell.speedBuff);
            }
        }

        /// <summary>
        /// Find the closest regiment of the given team within selection radius.
        /// </summary>
        Regiment FindRegimentAt(Vector3 worldPos, Team team)
        {
            Regiment closest = null;
            float minDist = GameConfig.SelectRadius;
            foreach (var reg in GameManager.Instance.AllRegiments)
            {
                if (reg.Team != team || reg.AliveCount == 0) continue;
                float dist = Vector3.Distance(reg.transform.position, worldPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = reg;
                }
            }
            return closest;
        }
    }
}
