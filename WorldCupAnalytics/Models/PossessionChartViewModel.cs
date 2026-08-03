namespace WorldCupAnalytics.Models.ViewModels
{
    public class PossessionPointViewModel
    {
        public double SvgX { get; set; }
        public double SvgY { get; set; }
        public int Minute { get; set; }
        public decimal HomePctAtPoint { get; set; }
        public decimal AwayPctAtPoint { get; set; }
    }

    public class PossessionChartViewModel
    {
        public decimal HomePossessionPct { get; set; }
        public decimal AwayPossessionPct { get; set; }

        // Running (cumulative-to-date) home-team possession % over the
        // course of the match, as an SVG polyline. Naturally starts
        // volatile (small sample) and settles toward the final % as the
        // match progresses — that's expected behavior, not a bug.
        public string HomeRunningPctPolyline { get; set; } = string.Empty;

        // One hoverable point per possession-sequence change, so the exact
        // reading at any moment in the match can be read via tooltip
        // instead of having to interpret the line's shape.
        public List<PossessionPointViewModel> Points { get; set; } = new();
    }
}