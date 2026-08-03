namespace WorldCupAnalytics.Models
{
    public class DribbleEvent
    {
        public string EventId { get; set; } = string.Empty;
        public long MatchId { get; set; }
        public int? Period { get; set; }
        public int? Minute { get; set; }
        public int? TeamId { get; set; }
        public int? PlayerId { get; set; }
        public string? PositionName { get; set; }

        public string? OutcomeName { get; set; } // "Complete" or "Incomplete"
        public bool? IsComplete { get; set; }

        public decimal? LocX { get; set; }
        public decimal? LocY { get; set; }

        public Match? Match { get; set; }
        public Player? Player { get; set; }
    }
}