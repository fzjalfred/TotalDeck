using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// Game phases: Planning (card play, economy) and Combat (RTS combat).
    /// </summary>
    public enum GamePhase
    {
        Planning,
        Combat
    }

    /// <summary>
    /// Card types: Unit (deploy regiment) and Spell (instant effect on regiment).
    /// </summary>
    public enum CardType
    {
        Unit,
        Spell
    }

    /// <summary>
    /// Team affiliation for regiments and soldiers.
    /// </summary>
    public enum Team
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Global configuration constants matching the original HTML demo.
    /// </summary>
    public static class GameConfig
    {
        public const float PlanningTime = 20f;
        public const float CombatTime = 60f;
        public const int RegimentSize = 50;
        public const int RegimentCols = 10;
        public const float SoldierSpacing = 1.2f;
        public const float SoldierRadius = 0.35f;
        public const float SelectRadius = 6.5f;
        public const float RegimentSpeed = 4.5f;
        public const float SoldierHP = 20f;
        public const float SoldierAttack = 4f;
        public const float AttackCooldownMin = 0.8f;
        public const float AttackCooldownMax = 1.2f;
        public const float AttackRange = 2.2f;
        public const float EngageRadius = 6f;

        // Hard collision: any two soldiers (friend or foe) never overlap
        public const float SoldierDiameter = SoldierRadius * 2f;

        // Economy
        public const int BaseIncome = 100;
        public const int KillBounty = 2;
        public const int UpkeepPerRegiment = 15;
        public const int InitialDrawCost = 50;
        public const int DrawCostIncrement = 50;
        public const int StartingTreasury = 250;

        // Healing spell
        public const int HealAmount = 15;
    }
}
