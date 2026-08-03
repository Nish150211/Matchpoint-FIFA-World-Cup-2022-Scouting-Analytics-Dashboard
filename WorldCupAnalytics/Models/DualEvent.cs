namespace WorldCupAnalytics.Models
{
    public class DuelEvent
    {
        public string EventId { get; set; } = string.Empty;
        public long MatchId { get; set; }
        public int? Period { get; set; }
        public int? Minute { get; set; }
        public int? TeamId { get; set; }
        public int? PlayerId { get; set; }
        public string? PositionName { get; set; }

        // "Tackle" or "Aerial Lost" — see notes elsewhere: aerial WINS
        // aren't tracked as a separate Duel event in StatsBomb's model,
        // so "Aerial Lost" duels are always losses by definition.
        public string? DuelType { get; set; }
        public string? OutcomeName { get; set; }
        public bool? IsWon { get; set; }

        public decimal? LocX { get; set; }
        public decimal? LocY { get; set; }

        public Match? Match { get; set; }
        public Player? Player { get; set; }

        public bool IsAerial => DuelType == "Aerial Lost";
    }
}