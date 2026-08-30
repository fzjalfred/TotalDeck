namespace TotalDeck
{
    /// <summary>
    /// Per-side combat statistics: kills and losses. One instance per team,
    /// owned by GameManager alongside PlayerState.
    /// </summary>
    public class SideStats
    {
        public Team Team { get; }
        public int Kills { get; private set; }
        public int Losses { get; private set; }

        public SideStats(Team team)
        {
            Team = team;
        }

        public void AddKill() => Kills++;
        public void AddLoss() => Losses++;

        public void Reset()
        {
            Kills = 0;
            Losses = 0;
        }
    }
}
