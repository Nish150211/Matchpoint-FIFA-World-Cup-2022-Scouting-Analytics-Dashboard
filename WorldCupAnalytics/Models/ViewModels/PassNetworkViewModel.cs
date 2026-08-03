namespace WorldCupAnalytics.Models.ViewModels
{
    public class PassNetworkNodeViewModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public double SvgX { get; set; }
        public double SvgY { get; set; }
        public double Radius { get; set; }
        public int TotalPasses { get; set; }
    }

    public class PassNetworkEdgeViewModel
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public int CombinedPassCount { get; set; } // passes A->B + B->A
        public double StrokeWidth { get; set; }
    }

    public class PassNetworkViewModel
    {
        public List<PassNetworkNodeViewModel> Nodes { get; set; } = new();
        public List<PassNetworkEdgeViewModel> Edges { get; set; } = new();
    }
}