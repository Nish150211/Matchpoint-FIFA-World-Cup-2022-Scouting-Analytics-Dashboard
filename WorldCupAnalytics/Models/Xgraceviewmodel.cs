namespace WorldCupAnalytics.Models.ViewModels
{
    public class XgRaceMarkerViewModel
    {
        public double SvgX { get; set; }
        public double SvgY { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Minute { get; set; }
        public decimal CumulativeXg { get; set; }
        public string OutcomeName { get; set; } = string.Empty;
        public decimal ShotXg { get; set; }
    }

    public class XgRaceSeriesViewModel
    {
        public string TeamName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string StepPolylinePoints { get; set; } = string.Empty; // "x,y x,y ..." forming a step/stairs shape
        public List<XgRaceMarkerViewModel> GoalMarkers { get; set; } = new();
        public List<XgRaceMarkerViewModel> AllShotMarkers { get; set; } = new(); // every shot, for full-graph hover
        public decimal FinalXg { get; set; }
    }
}