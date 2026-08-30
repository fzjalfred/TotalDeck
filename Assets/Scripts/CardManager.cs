using System;
using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Human player's hand UI bridge. Card ownership and economy live in
    /// GameManager.Player (PlayerState); this class only mirrors the hand
    /// for the UI and forwards draw/play actions.
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }

        [Header("Card Pool")]
        public CardData[] cardPool;

        [Header("Initial Hand")]
        public CardData[] startingHand;

        // ── Runtime State (mirrors GameManager.Player.Hand) ──
        public List<CardData> Hand => GameManager.Instance.Player.Hand;
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

        /// <summary>
        /// Called by GameManager after it populates both starting hands.
        /// </summary>
        public void NotifyHandChanged()
        {
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Draw a card for the player. Cost scaling lives in PlayerState.
        /// </summary>
        public bool DrawCard()
        {
            bool ok = GameManager.Instance.Player.DrawCard();
            if (ok) OnHandChanged?.Invoke();
            return ok;
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
                if (!GameManager.Instance.Player.CanAfford(card.playCost))
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

            var player = GameManager.Instance.Player;

            if (SelectedCard.cardType == CardType.Unit)
            {
                if (!GameManager.Instance.IsInDeployZone(worldPos, Team.Player))
                    return false;
                if (!player.PlayUnitCard(SelectedCard, worldPos))
                    return false;
            }
            else if (SelectedCard.cardType == CardType.Spell)
            {
                // For the player, spells need a clicked target like before
                Regiment target = FindRegimentAt(worldPos, Team.Player);
                if (target == null) return false;
                if (!SpendAndCast(player, SelectedCard, target))
                    return false;
            }

            SelectedCard = null;
            OnHandChanged?.Invoke();
            return true;
        }

        bool SpendAndCast(PlayerState player, CardData spell, Regiment target)
        {
            if (!player.CanAfford(spell.playCost)) return false;
            player.Spend(spell.playCost);

            if (spell.healAmount > 0) target.ModifySoldiers(spell.healAmount);
            if (spell.damageAmount > 0) target.DamageAllSoldiers(spell.damageAmount);
            if (spell.attackBuff != 0f || spell.hpBuff != 0f || spell.speedBuff != 0f)
                target.ApplyBuff(spell.attackBuff, spell.hpBuff, spell.speedBuff);

            player.Hand.Remove(spell);
            return true;
        }

        /// <summary>
        /// Find the closest regiment of the given team within selection radius.
        /// </summary>
        Regiment FindRegimentAt(Vector3 worldPos, Team team)
        {
            Regiment closest = null;
            float minDist = GameConfig.SelectRadius;
            foreach (var reg in GameManager.Instance.RegimentsOf(team))
            {
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
