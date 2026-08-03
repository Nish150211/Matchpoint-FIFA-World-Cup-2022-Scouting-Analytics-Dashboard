namespace WorldCupAnalytics.Models
{
    public class PossessionSequence
    {
        // Composite primary key (MatchId, PossessionSeq) — configured in ApplicationDbContext
        public long MatchId { get; set; }
        public int PossessionSeq { get; set; }
        public int TeamId { get; set; }
        public int StartMinute { get; set; }
        public int StartSecond { get; set; }

        public Match? Match { get; set; }
    }
}