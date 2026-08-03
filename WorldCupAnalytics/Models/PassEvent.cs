namespace WorldCupAnalytics.Models
{
    public class PassEvent
    {
        public string EventId { get; set; } = string.Empty;
        public long MatchId { get; set; }
        public int? Period { get; set; }
        public int? Minute { get; set; }
        public int? Second { get; set; }

        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public int? PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public string? PositionName { get; set; }

        public decimal? LocX { get; set; }
        public decimal? LocY { get; set; }
        public decimal? EndLocX { get; set; }
        public decimal? EndLocY { get; set; }

        public int? RecipientId { get; set; }
        public string? RecipientName { get; set; }
        public decimal? PassLength { get; set; }
        public string? PassHeightName { get; set; }

        public string? OutcomeName { get; set; }
        public string? BodyPartName { get; set; }
        public bool UnderPressure { get; set; }
        public bool IsShotAssist { get; set; }
        public bool IsGoalAssist { get; set; }

        public Match? Match { get; set; }
        public Team? Team { get; set; }
        public Player? Player { get; set; }

        public bool IsComplete => string.IsNullOrEmpty(OutcomeName) || OutcomeName == "Complete";
    }
}