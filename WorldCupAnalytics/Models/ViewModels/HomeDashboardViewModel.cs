namespace WorldCupAnalytics.Models.ViewModels
{
    // One row in a leaderboard list (top scorers, top assists, etc.)
    public class LeaderRowViewModel
    {
        public int Rank { get; set; }
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty; // e.g. "8 G", "0.62"
    }

    // One row in the "over/underperforming xG" bar chart
    public class FinishingRowViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public decimal GoalsMinusXg { get; set; }
        public double BarWidthPct { get; set; }
    }

    public class HomeDashboardViewModel
    {
        // Tournament Snapshot cards
        public int TotalGoals { get; set; }
        public int TotalShots { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalPasses { get; set; }

        // Player Spotlight (top scorer)
        public int SpotlightPlayerId { get; set; }
        public string SpotlightName { get; set; } = string.Empty;
        public string SpotlightTeam { get; set; } = string.Empty;
        public string SpotlightPosition { get; set; } = string.Empty;
        public int SpotlightGoals { get; set; }
        public int SpotlightAssists { get; set; }
        public decimal SpotlightXg { get; set; }
        public decimal? SpotlightPassAccPct { get; set; }

        // Leaderboards
        public List<LeaderRowViewModel> TopScorers { get; set; } = new();
        public List<LeaderRowViewModel> TopAssists { get; set; } = new();
        public List<LeaderRowViewModel> TopXg { get; set; } = new();

        // Finishing over/underperformance (min. 5 shots)
        public List<FinishingRowViewModel> FinishingRows { get; set; } = new();
    }
}