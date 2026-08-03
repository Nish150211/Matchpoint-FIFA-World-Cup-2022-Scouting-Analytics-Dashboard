namespace WorldCupAnalytics.Models.ViewModels
{
    // One player's average position on the pitch for a match — based on
    // ALL their located events (not just passes), for a more complete
    // picture of where they actually operated than a pass-only average.
    public class FormationNodeViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? JerseyNumber { get; set; }
        public double SvgX { get; set; }
        public double SvgY { get; set; }
    }

    public class SubstitutionRowViewModel
    {
        public int Minute { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TeamColorHex { get; set; } = string.Empty;
        public int? PlayerOffId { get; set; }
        public string PlayerOffName { get; set; } = string.Empty;
        public int? PlayerOnId { get; set; }
        public string PlayerOnName { get; set; } = string.Empty;
        public string? Reason { get; set; } // "Tactical" or "Injury"
    }
}