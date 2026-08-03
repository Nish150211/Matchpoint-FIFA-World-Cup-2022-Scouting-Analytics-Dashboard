namespace WorldCupAnalytics.Models.ViewModels
{
    // One dot on a shot map, already positioned in SVG pixel space
    // (not raw pitch coordinates — whichever controller builds this list
    // does that conversion once, so views never need to do pitch math).
    public class ShotDotViewModel
    {
        public double SvgX { get; set; }
        public double SvgY { get; set; }
        public double Radius { get; set; }
        public string Color { get; set; } = string.Empty;
        public string OutcomeName { get; set; } = string.Empty;

        // Extra detail shown in the hover tooltip
        public int? Minute { get; set; }
        public decimal? Xg { get; set; }
        public string MatchLabel { get; set; } = string.Empty; // e.g. "vs France (Final, 18 Dec)"
        public string TooltipText =>
            $"{MatchLabel}{(MatchLabel.Length > 0 ? " \u2014 " : "")}{(Minute.HasValue ? $"{Minute}' " : "")}{OutcomeName}{(Xg.HasValue ? $" (xG {Xg.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})" : "")}";
    }

    // One pass drawn as a line from origin to destination, already
    // converted to SVG pixel space.
    public class PassLineViewModel
    {
        public double SvgX1 { get; set; }
        public double SvgY1 { get; set; }
        public double SvgX2 { get; set; }
        public double SvgY2 { get; set; }
        public bool IsComplete { get; set; }
        public bool IsGoalAssist { get; set; }
        public string Color { get; set; } = string.Empty;
        public string MatchLabel { get; set; } = string.Empty;
        public string TooltipText { get; set; } = string.Empty;
    }

    // One cell of a heat map grid, already positioned + sized in SVG pixel
    // space, with a pre-computed fill opacity based on touch density.
    public class HeatCellViewModel
    {
        public double SvgX { get; set; }
        public double SvgY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; }
        public int TouchCount { get; set; }
    }
}