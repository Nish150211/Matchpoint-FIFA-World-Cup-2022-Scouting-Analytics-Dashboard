namespace WorldCupAnalytics.Models.ViewModels
{
    public class PlayerMatchLogRowViewModel
    {
        public DateOnly MatchDate { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string Opponent { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int Assists { get; set; }
        public decimal Xg { get; set; }
        public int Shots { get; set; }
    }

    public class PlayerProfileViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? GroupLetter { get; set; }
        public int? JerseyNumber { get; set; }

        // Tournament Output cards
        public int Goals { get; set; }
        public decimal Xg { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public int Assists { get; set; }
        public int Starts { get; set; }
        public int TotalPasses { get; set; }
        public decimal? PassAccuracyPct { get; set; }

        public List<ShotDotViewModel> ShotMap { get; set; } = new();
        public List<PassLineViewModel> PassMap { get; set; } = new();
        public List<HeatCellViewModel> HeatMap { get; set; } = new();
        public int TouchesCount { get; set; }

        public List<PlayerMatchLogRowViewModel> MatchLog { get; set; } = new();

        // Defensive & work-rate stats — see note in ComparePlayerViewModel
        public int Pressures { get; set; }
        public int Interceptions { get; set; }
        public int Clearances { get; set; }
        public int Blocks { get; set; }
        public int FoulsCommitted { get; set; }
        public int FoulsWon { get; set; }

        public int GroundDuelsWon { get; set; }
        public int GroundDuelsTotal { get; set; }
        public decimal GroundDuelWinPct { get; set; }
        public int AerialDuelsLost { get; set; }

        public int DribblesCompleted { get; set; }
        public int DribblesAttempted { get; set; }
        public decimal DribbleSuccessPct { get; set; }

        // Team's last 5 results (not just this player's — matches the
        // mockup's "team form" framing)
        public List<string> Form { get; set; } = new();
    }
}