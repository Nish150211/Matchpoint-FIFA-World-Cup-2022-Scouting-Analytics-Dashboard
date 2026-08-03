namespace WorldCupAnalytics.Models
{
    public class ShotEvent
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

        public decimal? StatsbombXg { get; set; }
        public string? OutcomeName { get; set; }
        public string? BodyPartName { get; set; }
        public string? ShotTypeName { get; set; }
        public string? TechniqueName { get; set; }
        public bool UnderPressure { get; set; }

        public Match? Match { get; set; }
        public Team? Team { get; set; }
        public Player? Player { get; set; }

        public bool IsGoal => OutcomeName == "Goal";
    }
}