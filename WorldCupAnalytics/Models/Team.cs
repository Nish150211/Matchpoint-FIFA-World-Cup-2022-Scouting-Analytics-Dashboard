namespace WorldCupAnalytics.Models
{
    public class Team
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? GroupLetter { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
}