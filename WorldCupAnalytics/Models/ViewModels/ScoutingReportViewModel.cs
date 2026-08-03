namespace WorldCupAnalytics.Models.ViewModels
{
    public class ScoutingReportViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public int Goals { get; set; }
        public int Assists { get; set; }
        public int Shots { get; set; }
        public int Starts { get; set; }
        public decimal Xg { get; set; }
        public decimal? PassAccuracyPct { get; set; }

        public string Verdict { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Considerations { get; set; } = new();
        public string AnalystNotes { get; set; } = string.Empty;

        // For the player-switcher dropdown
        public List<(int PlayerId, string DisplayName, string TeamName)> AllPlayers { get; set; } = new();
    }
}