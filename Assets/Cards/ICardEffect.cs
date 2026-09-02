using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Strategy interface for card resolution. The engine (PlayerState /
    /// CardManager / AIController) never inspects WHICH card it is holding —
    /// it resolves the card's effect through CardEffectResolver and calls
    /// these methods. Adding a card = one entry in the resolver + this file.
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// Legal-target check, before any payment. <paramref name="position"/>
        /// is the clicked world position (deploy cards), <paramref name="clickedTarget"/>
        /// the clicked regiment (targeted spells) — either may be unused
        /// depending on the card. Returns false and the engine charges nothing.
        /// </summary>
        bool CanPlay(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget);

        /// <summary>
        /// Apply the card's world effect. Phase gating, payment and hand
        /// removal are the engine's job and happen before this runs.
        /// </summary>
        void Execute(CardData card, PlayerState caster, Vector3 position, Regiment clickedTarget);
    }
}
