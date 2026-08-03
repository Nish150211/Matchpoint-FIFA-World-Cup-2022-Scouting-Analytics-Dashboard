namespace WorldCupAnalytics.Models.ViewModels
{
    public class TeamMatchStatViewModel
    {
        public long MatchId { get; set; }
        public DateOnly MatchDate { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string StageAbbrev { get; set; } = string.Empty; // first 2 letters, e.g. "GR", "QU", "FI"
        public string Opponent { get; set; } = string.Empty;
        public bool IsHome { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public decimal Xg { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public decimal ShotsOnTargetPct { get; set; }
        public string Result { get; set; } = string.Empty; // W/D/L

        // Pre-computed SVG coordinates for this match's point on the line chart
        public double ChartX { get; set; }
        public double ChartY { get; set; }
    }

    public class TeamSummaryViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;

        public int MatchesPlayed { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public decimal TotalXg { get; set; }
        public int TotalShots { get; set; }
        public decimal ShotsOnTargetPct { get; set; }
        public decimal PassAccuracyPct { get; set; }

        public List<TeamMatchStatViewModel> Matches { get; set; } = new(); // chronological

        // Pre-built SVG <polyline points="..."> for the xG-per-match chart
        public string LinePoints { get; set; } = string.Empty;
    }

    public class TeamsComparePageViewModel
    {
        public TeamSummaryViewModel? TeamA { get; set; }
        public TeamSummaryViewModel? TeamB { get; set; }

        // For the two dropdowns
        public List<(int TeamId, string TeamName)> AllTeams { get; set; } = new();

        // X-axis stage labels for the chart — taken from whichever team
        // played more matches (the one that went further in the tournament)
        public List<(double ChartX, string Label)> XAxisLabels { get; set; } = new();
    }
}