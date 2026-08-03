namespace WorldCupAnalytics.Models
{
    public class MatchLineup
    {
        public long MatchId { get; set; }
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public int? JerseyNumber { get; set; }
        public string? PositionName { get; set; }
        public bool IsStartingXi { get; set; }

        public Match? Match { get; set; }
        public Player? Player { get; set; }
        public Team? Team { get; set; }
    }
}