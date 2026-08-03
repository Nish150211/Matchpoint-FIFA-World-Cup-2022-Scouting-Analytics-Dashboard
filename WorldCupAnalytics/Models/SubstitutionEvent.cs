namespace WorldCupAnalytics.Models
{
    public class SubstitutionEvent
    {
        public string EventId { get; set; } = string.Empty;
        public long MatchId { get; set; }
        public int? Period { get; set; }
        public int? Minute { get; set; }
        public int? TeamId { get; set; }

        public int? PlayerOffId { get; set; }
        public string? PlayerOffName { get; set; }
        public int? PlayerOnId { get; set; }
        public string? PlayerOnName { get; set; }

        public string? PositionName { get; set; }
        public string? OutcomeName { get; set; } // e.g. "Tactical", "Injury"

        public Match? Match { get; set; }
    }
}