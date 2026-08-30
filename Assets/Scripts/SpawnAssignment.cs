using System;
using UnityEngine;

namespace TotalDeck
{
    /// <summary>
    /// A side's deployment assignment: which map slot it occupies.
    /// Player defaults to slot 0; the AI takes any closed slot chosen
    /// in the menu dropdown.
    /// </summary>
    [Serializable]
    public class SpawnAssignment
    {
        public Team team;
        public int slotIndex;

        public SpawnAssignment(Team team, int slotIndex)
        {
            this.team = team;
            this.slotIndex = slotIndex;
        }
    }
}
