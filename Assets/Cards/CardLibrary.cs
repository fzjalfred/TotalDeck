using System.Collections.Generic;
using UnityEngine;

namespace TotalDeck.Cards
{
    /// <summary>
    /// Card Library — the single authoritative view of every card in the game.
    /// Order matters: the three Troop (unit) cards come first, then the three
    /// Spell cards. Implemented cards (Footman, Heal) have their behaviour
    /// wired through PlayerState/CardManager; the rest are TODO placeholders.
    /// </summary>
    public static class CardLibrary
    {
        public class CardEntry
        {
            public string name;
            public CardType type;
            public string assetPath;
            public string description;
            public bool implemented;
        }

        // ── Troop (unit) cards ─────────────────────────────
        public static readonly CardEntry Footman = new CardEntry
        {
            name = "Footman", type = CardType.Unit,
            assetPath = "Assets/Cards/FootmanCard.asset",
            description = "Deploy a 50-man footman regiment",
            implemented = true
        };
        public static readonly CardEntry Archer = new CardEntry
        {
            name = "Archer", type = CardType.Unit,
            assetPath = "Assets/Cards/ArcherCard.asset",
            description = "Ranged troop",
            implemented = false
        };
        public static readonly CardEntry Knight = new CardEntry
        {
            name = "Knight", type = CardType.Unit,
            assetPath = "Assets/Cards/KnightCard.asset",
            description = "Melee cavalry troop",
            implemented = false
        };

        // ── Spell cards ────────────────────────────────────
        public static readonly CardEntry Heal = new CardEntry
        {
            name = "Heal", type = CardType.Spell,
            assetPath = "Assets/Cards/HealCard.asset",
            description = "Restore +15 soldiers to the most wounded friendly regiment",
            implemented = true
        };
        public static readonly CardEntry Inferno = new CardEntry
        {
            name = "Inferno", type = CardType.Spell,
            assetPath = "Assets/Cards/InfernoCard.asset",
            description = "Area damage spell",
            implemented = false
        };
        public static readonly CardEntry Frost = new CardEntry
        {
            name = "Frost", type = CardType.Spell,
            assetPath = "Assets/Cards/FrostCard.asset",
            description = "Slow / freeze debuff spell",
            implemented = false
        };

        /// <summary>All cards in display order: Footman, Archer, Knight, Heal, Inferno, Frost.</summary>
        public static IReadOnlyList<CardEntry> All => new[] { Footman, Archer, Knight, Heal, Inferno, Frost };
        public static IReadOnlyList<CardEntry> Troops => new[] { Footman, Archer, Knight };
        public static IReadOnlyList<CardEntry> Spells => new[] { Heal, Inferno, Frost };
    }
}
