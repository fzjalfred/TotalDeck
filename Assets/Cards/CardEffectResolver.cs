using System.Collections.Generic;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Maps card assets to their strategy objects (keyed by the stable
    /// CardData.cardID set on each asset). The engine resolves ANY card
    /// through here and talks only to ICardEffect — it never needs to know
    /// which card it is settling. To add a card: create the asset, write its
    /// ICardEffect class, add one entry below.
    /// </summary>
    public static class CardEffectResolver
    {
        static readonly Dictionary<int, ICardEffect> byCardId = new Dictionary<int, ICardEffect>
        {
            { 1, new FootmanCard() },   // Troop  — implemented
            { 2, new ArcherCard() },    // Troop  — placeholder
            { 3, new KnightCard() },    // Troop  — placeholder
            { 4, new HealCard() },      // Spell  — implemented
            { 5, new InfernoCard() },   // Spell  — placeholder
            { 6, new FrostCard() },     // Spell  — placeholder
        };

        /// <summary>Null when the card has no registered effect (unplayable).</summary>
        public static ICardEffect Resolve(CardData card)
            => card != null && byCardId.TryGetValue(card.cardID, out var effect) ? effect : null;
    }
}
