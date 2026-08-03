namespace WorldCupAnalytics.Models.ViewModels
{
    public class PlayerSearchResultDto
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int Assists { get; set; }
        public decimal Xg { get; set; }
        public int Shots { get; set; }
    }
}