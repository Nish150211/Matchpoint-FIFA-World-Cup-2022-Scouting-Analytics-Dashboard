namespace WorldCupAnalytics.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? PlayerNickname { get; set; }
        public int? JerseyNumber { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }

        public Team? Team { get; set; }
        public ICollection<MatchLineup> Lineups { get; set; } = new List<MatchLineup>();
        public ICollection<ShotEvent> Shots { get; set; } = new List<ShotEvent>();
        public ICollection<PassEvent> Passes { get; set; } = new List<PassEvent>();

        public string DisplayName => !string.IsNullOrEmpty(PlayerNickname) ? PlayerNickname! : PlayerName;
    }
}