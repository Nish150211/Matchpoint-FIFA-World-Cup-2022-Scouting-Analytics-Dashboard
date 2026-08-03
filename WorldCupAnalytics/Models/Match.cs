namespace WorldCupAnalytics.Models
{
    public class Match
    {
        public long MatchId { get; set; }
        public DateOnly MatchDate { get; set; }
        public TimeOnly? KickoffTime { get; set; }
        public string? CompetitionStage { get; set; }
        public string? StadiumName { get; set; }
        public string? StadiumCountry { get; set; }

        public int HomeTeamId { get; set; }
        public string? HomeTeamName { get; set; }
        public int AwayTeamId { get; set; }
        public string? AwayTeamName { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public string? RefereeName { get; set; }
        public string? RefereeCountry { get; set; }

        public Team? HomeTeam { get; set; }
        public Team? AwayTeam { get; set; }
        public ICollection<MatchLineup> Lineups { get; set; } = new List<MatchLineup>();
        public ICollection<ShotEvent> Shots { get; set; } = new List<ShotEvent>();
        public ICollection<PassEvent> Passes { get; set; } = new List<PassEvent>();

        public string Scoreline => $"{HomeTeamName} {HomeScore} - {AwayScore} {AwayTeamName}";
    }
}