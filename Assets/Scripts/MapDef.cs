using System;
using UnityEngine;
using System.Collections.Generic;

namespace TotalDeck
{
    /// <summary>
    /// A named map with fixed spawn slots. The map owns geometry constants
    /// (ground size, hill); slots are just positions — any side can be
    /// assigned to any slot at game start.
    /// </summary>
    [Serializable]
    public class MapDef
    {
        public string mapName;
        public float groundSize = 100f;   // square ground edge length
        public Vector3 hillCenter = Vector3.zero;
        public float hillRadius = GameConfig.HillRadius;
        public Vector3[] spawnPoints;     // world positions; count = max players

        /// <summary>
        /// The built-in 1v1 map: two spawn slots on opposite sides of the
        /// doubled 100x100 battlefield.
        /// </summary>
        public static MapDef Duel()
        {
            return new MapDef
            {
                mapName = "1v1 对决",
                groundSize = 100f,
                hillCenter = Vector3.zero,
                hillRadius = GameConfig.HillRadius,
                spawnPoints = new[]
                {
                    new Vector3(0f, 0f, 30f),    // slot 0: player's default
                    new Vector3(0f, 0f, -30f),   // slot 1: enemy's default
                }
            };
        }

        /// <summary>
        /// A second 1v1 map: side spawns (east/west) with a tighter hill.
        /// </summary>
        public static MapDef Flank()
        {
            return new MapDef
            {
                mapName = "1v1 侧袭",
                groundSize = 100f,
                hillCenter = Vector3.zero,
                hillRadius = 12f,
                spawnPoints = new[]
                {
                    new Vector3(-35f, 0f, 0f),  // slot 0: west
                    new Vector3(35f, 0f, 0f),   // slot 1: east
                }
            };
        }

        /// <summary>All maps offered in the menu.</summary>
        public static MapDef[] Available()
        {
            return new[] { Duel(), Flank() };
        }
    }
}
