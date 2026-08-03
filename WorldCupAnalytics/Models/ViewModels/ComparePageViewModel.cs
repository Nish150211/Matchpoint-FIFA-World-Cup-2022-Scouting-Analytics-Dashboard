namespace WorldCupAnalytics.Models.ViewModels
{
    public class ComparePlayerViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;

        public int Starts { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public decimal Xg { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public decimal PassAccuracyPct { get; set; }
        public int TouchesCount { get; set; }

        public List<string> Form { get; set; } = new();

        // Defensive & work-rate stats — raw counts from event_locations'
        // event_type column. Note: unlike shots/passes, these don't carry
        // a success/fail outcome in our extracted data (e.g. "Duel" doesn't
        // tell us won vs lost) — they're workrate/involvement counts, not
        // success-rate stats. Framed that way in the UI.
        public int Pressures { get; set; }
        public int Interceptions { get; set; }
        public int Clearances { get; set; }
        public int Blocks { get; set; }
        public int FoulsCommitted { get; set; }
        public int FoulsWon { get; set; }

        // Ground duels (tackles) have a real won/lost outcome; aerial
        // duels are losses-only in this dataset (see notes elsewhere) so
        // they're kept separate rather than blended into one misleading %.
        public int GroundDuelsWon { get; set; }
        public int GroundDuelsTotal { get; set; }
        public decimal GroundDuelWinPct { get; set; }
        public int AerialDuelsLost { get; set; }

        public int DribblesCompleted { get; set; }
        public int DribblesAttempted { get; set; }
        public decimal DribbleSuccessPct { get; set; }

        // Assigned by the controller based on selection order (1st, 2nd,
        // 3rd, 4th player picked) — used consistently across the table,
        // radar legend, bars, and pitch cards so each player has one color
        // throughout the whole page.
        public string ColorHex { get; set; } = "#2E7D50";

        public List<ShotDotViewModel> ShotMap { get; set; } = new();
        public List<HeatCellViewModel> HeatMap { get; set; } = new();
    }

    // One labeled point on the radar chart for one player — carries both
    // the plotted (x,y) position AND the real underlying stat value, so
    // the view can show an exact number in a tooltip, not just a shape.
    public class RadarAxisPointViewModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string AxisLabel { get; set; } = string.Empty;
        public string RawValueDisplay { get; set; } = string.Empty;
        public double Percentile { get; set; }
    }

    public class RadarSeriesViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string PolygonPoints { get; set; } = string.Empty; // "x,y x,y ..." for the <polygon>
        public List<RadarAxisPointViewModel> Points { get; set; } = new();
    }

    public class ComparePageViewModel
    {
        public List<ComparePlayerViewModel> Players { get; set; } = new();

        // Comma-separated player IDs currently selected — reflected back
        // into the page so the browse panel's checkboxes can show which
        // players are already checked without needing separate client state.
        public string SelectedIdsCsv { get; set; } = string.Empty;

        // Dropdown options for the filter rail
        public List<string> AllTeamNames { get; set; } = new();
        public List<string> AllGroupLetters { get; set; } = new();

        // One series per selected player — shape + tooltip data for the radar chart
        public List<RadarSeriesViewModel> RadarSeries { get; set; } = new();
    }
}