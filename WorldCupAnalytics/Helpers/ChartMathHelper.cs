using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Helpers
{
    public static class ChartMathHelper
    {
        // If two or more shots land at (or very near) the same pixel
        // position, drawing them as identical circles means one completely
        // hides the other — so a player who scored twice from the same
        // spot would visually look like only one goal happened. This
        // spreads near-duplicate points into a small circle around their
        // shared center so every one of them stays individually visible.
        public static List<ShotDotViewModel> DeoverlapDots(List<ShotDotViewModel> dots, double bucketSizePx = 4.0)
        {
            var groups = dots.GroupBy(d => (
                Math.Round(d.SvgX / bucketSizePx), Math.Round(d.SvgY / bucketSizePx)
            ));

            foreach (var group in groups)
            {
                var list = group.ToList();
                if (list.Count <= 1) continue;

                double cx = list.Average(d => d.SvgX);
                double cy = list.Average(d => d.SvgY);
                double spreadRadius = 6 + list.Max(d => d.Radius);

                for (int i = 0; i < list.Count; i++)
                {
                    double angle = 2 * Math.PI * i / list.Count;
                    list[i].SvgX = Math.Round(cx + spreadRadius * Math.Cos(angle), 1);
                    list[i].SvgY = Math.Round(cy + spreadRadius * Math.Sin(angle), 1);
                }
            }

            return dots;
        }
    }
}