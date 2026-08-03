namespace WorldCupAnalytics.Models.ViewModels
{
    public class GoalEventViewModel
    {
        public int Minute { get; set; }
        public int? PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string TeamColorHex { get; set; } = string.Empty;
        public string Technique { get; set; } = string.Empty;
        public string ShotType { get; set; } = string.Empty; // "Open Play", "Penalty", "Free Kick", "Corner"
        public decimal? Xg { get; set; }
    }

    public class LineupRowViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public int? JerseyNumber { get; set; }
        public string Position { get; set; } = string.Empty;
        public bool IsStartingXi { get; set; }
    }

    public class MatchDetailViewModel
    {
        public long MatchId { get; set; }
        public DateOnly MatchDate { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string? StadiumName { get; set; }
        public string? StadiumCountry { get; set; }
        public string? RefereeName { get; set; }

        public int HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public string HomeColorHex { get; set; } = "#2E7D50";

        public int AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public int AwayScore { get; set; }
        public string AwayColorHex { get; set; } = "#1F6E80";

        // Combined shot map — both teams on one pitch. Home team's shots
        // are drawn attacking left-to-right (as recorded), away team's
        // shots are mirrored so both attack "forward" visually rather
        // than overlapping in the same direction.
        public List<ShotDotViewModel> HomeShotMap { get; set; } = new();
        public List<ShotDotViewModel> AwayShotMap { get; set; } = new();

        // Team-level match stats, same shape used on the Teams page
        public int HomeShots { get; set; }
        public int HomeShotsOnTarget { get; set; }
        public decimal HomeXg { get; set; }
        public decimal HomePassAccuracyPct { get; set; }

        public int AwayShots { get; set; }
        public int AwayShotsOnTarget { get; set; }
        public decimal AwayXg { get; set; }
        public decimal AwayPassAccuracyPct { get; set; }

        public List<GoalEventViewModel> GoalTimeline { get; set; } = new();

        public List<LineupRowViewModel> HomeLineup { get; set; } = new();
        public List<LineupRowViewModel> AwayLineup { get; set; } = new();

        public PassNetworkViewModel HomePassNetwork { get; set; } = new();
        public PassNetworkViewModel AwayPassNetwork { get; set; } = new();

        public List<FormationNodeViewModel> HomeFormation { get; set; } = new();
        public List<FormationNodeViewModel> AwayFormation { get; set; } = new();

        // Both teams' substitutions, chronological
        public List<SubstitutionRowViewModel> Substitutions { get; set; } = new();

        public XgRaceSeriesViewModel HomeXgRace { get; set; } = new();
        public XgRaceSeriesViewModel AwayXgRace { get; set; } = new();
        public int XgChartMaxMinutes { get; set; } = 90; // 120 if the match went to extra time

        public PossessionChartViewModel Possession { get; set; } = new();
    }
}