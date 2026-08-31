using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Type-dispatched card play helper. Routes by cardType and hands off to
    /// the existing PlayerState / CardManager pipelines. Spell targeting for
    /// future placeholder spells (Inferno / Frost) goes through TODO paths
    /// that can be filled in without touching PlayerState.
    /// </summary>
    public static class CardPlay
    {
        /// <summary>Human player entry point: play a card at a world position.</summary>
        public static bool Play(PlayerState player, CardData card, Vector3 worldPos)
        {
            if (player == null || card == null) return false;
            switch (card.cardType)
            {
                case CardType.Unit:
                    return player.PlayUnitCard(card, worldPos);
                case CardType.Spell:
                    return player.PlaySpellCard(card);
                default:
                    return false;
            }
        }
    }
}
