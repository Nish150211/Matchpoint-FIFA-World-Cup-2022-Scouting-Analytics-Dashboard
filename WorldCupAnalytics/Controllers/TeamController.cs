using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;
using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private static readonly string[] TeamColors = { "#2E7D50", "#1F6E80" };

        public TeamsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Teams/Compare?teamA=Argentina&teamB=France
        // Defaults to the 2022 Final matchup if nothing is specified.
        public async Task<IActionResult> Compare(string? teamA, string? teamB)
        {
            ViewData["Title"] = "Team Comparison";
            ViewData["ActiveTab"] = "teams";

            var allTeams = await _db.Teams.OrderBy(t => t.TeamName).ToListAsync();

            string nameA = teamA ?? "Argentina";
            string nameB = teamB ?? "France";

            var teamAEntity = allTeams.FirstOrDefault(t => t.TeamName == nameA) ?? allTeams.First();
            var teamBEntity = allTeams.FirstOrDefault(t => t.TeamName == nameB) ?? allTeams.Skip(1).First();

            var summaryA = await BuildTeamSummaryAsync(teamAEntity.TeamId, teamAEntity.TeamName, TeamColors[0]);
            var summaryB = await BuildTeamSummaryAsync(teamBEntity.TeamId, teamBEntity.TeamName, TeamColors[1]);

            // Shared y-axis scale for the xG chart — see note on
            // TeamsCompareViewModel about why this differs from the
            // mockup's per-team independent scaling.
            var allMatchXg = summaryA.Matches.Select(m => m.Xg).Concat(summaryB.Matches.Select(m => m.Xg));
            var sharedMaxXg = allMatchXg.DefaultIfEmpty(0).Max();
            if (sharedMaxXg <= 0) sharedMaxXg = 1;

            AssignChartCoordinates(summaryA.Matches, sharedMaxXg);
            AssignChartCoordinates(summaryB.Matches, sharedMaxXg);

            summaryA.LinePoints = string.Join(" ", summaryA.Matches.Select(m =>
                $"{m.ChartX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{m.ChartY.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
            summaryB.LinePoints = string.Join(" ", summaryB.Matches.Select(m =>
                $"{m.ChartX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{m.ChartY.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            // X-axis labels come from whichever team played more matches
            // (the one that went further in the tournament)
            var longerRun = summaryA.Matches.Count >= summaryB.Matches.Count ? summaryA.Matches : summaryB.Matches;

            var vm = new TeamsComparePageViewModel
            {
                TeamA = summaryA,
                TeamB = summaryB,
                AllTeams = allTeams.Select(t => (t.TeamId, t.TeamName)).ToList(),
                XAxisLabels = longerRun.Select(m => (m.ChartX, m.StageAbbrev)).ToList()
            };

            return View(vm);
        }

        private async Task<TeamSummaryViewModel> BuildTeamSummaryAsync(int teamId, string teamName, string colorHex)
        {
            var matches = await _db.Matches
                .Where(m => (m.HomeTeamId == teamId || m.AwayTeamId == teamId) && m.HomeScore != null && m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var summary = new TeamSummaryViewModel
            {
                TeamId = teamId,
                TeamName = teamName,
                ColorHex = colorHex
            };

            int totalShots = 0, totalSot = 0, totalPasses = 0, totalCompletedPasses = 0;
            decimal totalXg = 0;

            foreach (var match in matches)
            {
                bool isHome = match.HomeTeamId == teamId;
                string opponent = isHome ? match.AwayTeamName ?? "" : match.HomeTeamName ?? "";
                int goalsFor = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
                int goalsAgainst = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;

                var matchShots = await _db.ShotEvents
                    .Where(s => s.MatchId == match.MatchId && s.TeamId == teamId && s.Period != 5)
                    .ToListAsync();

                int shots = matchShots.Count;
                int sot = matchShots.Count(s => s.OutcomeName == "Goal" || s.OutcomeName == "Saved" || s.OutcomeName == "Saved to Post");
                decimal xg = matchShots.Sum(s => s.StatsbombXg ?? 0);

                var matchPasses = await _db.PassEvents
                    .Where(p => p.MatchId == match.MatchId && p.TeamId == teamId)
                    .ToListAsync();

                int passes = matchPasses.Count;
                int completedPasses = matchPasses.Count(p => p.IsComplete);

                totalShots += shots;
                totalSot += sot;
                totalXg += xg;
                totalPasses += passes;
                totalCompletedPasses += completedPasses;

                summary.Matches.Add(new TeamMatchStatViewModel
                {
                    MatchId = match.MatchId,
                    MatchDate = match.MatchDate,
                    Stage = match.CompetitionStage ?? "-",
                    StageAbbrev = (match.CompetitionStage ?? "-").Length >= 2 ? match.CompetitionStage!.Substring(0, 2).ToUpper() : "-",
                    Opponent = opponent,
                    IsHome = isHome,
                    GoalsFor = goalsFor,
                    GoalsAgainst = goalsAgainst,
                    Xg = Math.Round(xg, 2),
                    Shots = shots,
                    ShotsOnTarget = sot,
                    ShotsOnTargetPct = shots > 0 ? Math.Round(100m * sot / shots, 0) : 0,
                    Result = goalsFor > goalsAgainst ? "W" : goalsFor < goalsAgainst ? "L" : "D"
                });
            }

            summary.MatchesPlayed = matches.Count;
            summary.GoalsFor = summary.Matches.Sum(m => m.GoalsFor);
            summary.GoalsAgainst = summary.Matches.Sum(m => m.GoalsAgainst);
            summary.TotalXg = Math.Round(totalXg, 2);
            summary.TotalShots = totalShots;
            summary.ShotsOnTargetPct = totalShots > 0 ? Math.Round(100m * totalSot / totalShots, 0) : 0;
            summary.PassAccuracyPct = totalPasses > 0 ? Math.Round(100m * totalCompletedPasses / totalPasses, 0) : 0;

            return summary;
        }

        // Same line-chart geometry approach as the radar/bars elsewhere:
        // x position spaces matches evenly across the chart width regardless
        // of how many there are; y position is scaled against the SHARED
        // max xG across both teams (not each team's own max).
        private void AssignChartCoordinates(List<TeamMatchStatViewModel> matches, decimal sharedMaxXg)
        {
            const double xStart = 60, xEnd = 660, yTop = 20, yBottom = 190;
            int n = matches.Count;

            for (int i = 0; i < n; i++)
            {
                double x = n <= 1 ? xStart : xStart + i * ((xEnd - xStart) / (n - 1));
                double fraction = (double)(matches[i].Xg / sharedMaxXg);
                double y = yBottom - fraction * (yBottom - yTop);
                matches[i].ChartX = Math.Round(x, 1);
                matches[i].ChartY = Math.Round(y, 1);
            }
        }
    }
}