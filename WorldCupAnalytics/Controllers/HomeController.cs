using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;
using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /  and  GET /Home/Index
        // The login screen — entry point of the app.
        public IActionResult Index()
        {
            return View();
        }

        // GET /Home/Dashboard
        // The authenticated landing page: tournament snapshot, player
        // spotlight, and three leaderboards computed across all 829 players.
        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Home";
            ViewData["ActiveTab"] = "home";

            // Aggregate shot stats per player, done in SQL via GroupBy.
            // Period 5 = penalty shootout — excluded, since shootout goals
            // aren't counted in official tournament goal/xG stats (e.g.
            // Golden Boot standings exclude them; StatsBomb's xG model also
            // isn't meaningful for the shootout context).
            var shotAgg = await _db.ShotEvents
                .Where(s => s.PlayerId != null && s.Period != 5)
                .GroupBy(s => s.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key!.Value,
                    Goals = g.Count(x => x.OutcomeName == "Goal"),
                    Shots = g.Count(),
                    Xg = g.Sum(x => x.StatsbombXg ?? 0)
                })
                .ToListAsync();

            // Aggregate pass stats per player, same approach.
            var passAgg = await _db.PassEvents
                .Where(p => p.PlayerId != null)
                .GroupBy(p => p.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key!.Value,
                    Assists = g.Count(x => x.IsGoalAssist),
                    TotalPasses = g.Count(),
                    CompletedPasses = g.Count(x => x.OutcomeName == null || x.OutcomeName == "Complete")
                })
                .ToListAsync();

            var players = await _db.Players.ToListAsync();

            // Combine into one per-player stats record, in memory (fast —
            // this is joining three already-small in-memory lists, not
            // hitting the database again).
            var combined = players.Select(p =>
            {
                var shots = shotAgg.FirstOrDefault(s => s.PlayerId == p.PlayerId);
                var passes = passAgg.FirstOrDefault(pa => pa.PlayerId == p.PlayerId);

                int goals = shots?.Goals ?? 0;
                int shotCount = shots?.Shots ?? 0;
                decimal xg = shots != null ? Math.Round(shots.Xg, 2) : 0;
                int assists = passes?.Assists ?? 0;
                int totalPasses = passes?.TotalPasses ?? 0;
                int completedPasses = passes?.CompletedPasses ?? 0;
                decimal? passAcc = totalPasses > 0
                    ? Math.Round((decimal)completedPasses / totalPasses * 100, 1)
                    : (decimal?)null;

                return new
                {
                    Player = p,
                    Goals = goals,
                    Shots = shotCount,
                    Xg = xg,
                    Assists = assists,
                    PassAccPct = passAcc
                };
            }).ToList();

            var vm = new HomeDashboardViewModel
            {
                TotalPlayers = players.Count,
                TotalShots = combined.Sum(c => c.Shots),
                TotalGoals = combined.Sum(c => c.Goals),
                TotalPasses = passAgg.Sum(p => p.TotalPasses)
            };

            var topScorers = combined.OrderByDescending(c => c.Goals).ThenByDescending(c => c.Xg).Take(8).ToList();
            vm.TopScorers = topScorers.Select((c, i) => new LeaderRowViewModel
            {
                Rank = i + 1,
                PlayerId = c.Player.PlayerId,
                DisplayName = c.Player.DisplayName,
                TeamName = c.Player.TeamName ?? "",
                DisplayValue = $"{c.Goals} G"
            }).ToList();

            var topAssists = combined.OrderByDescending(c => c.Assists).ThenByDescending(c => c.Goals).Take(8).ToList();
            vm.TopAssists = topAssists.Select((c, i) => new LeaderRowViewModel
            {
                Rank = i + 1,
                PlayerId = c.Player.PlayerId,
                DisplayName = c.Player.DisplayName,
                TeamName = c.Player.TeamName ?? "",
                DisplayValue = $"{c.Assists} A"
            }).ToList();

            var topXg = combined.OrderByDescending(c => c.Xg).Take(8).ToList();
            vm.TopXg = topXg.Select((c, i) => new LeaderRowViewModel
            {
                Rank = i + 1,
                PlayerId = c.Player.PlayerId,
                DisplayName = c.Player.DisplayName,
                TeamName = c.Player.TeamName ?? "",
                DisplayValue = c.Xg.ToString("0.00")
            }).ToList();

            var spotlight = topScorers.FirstOrDefault();
            if (spotlight != null)
            {
                vm.SpotlightPlayerId = spotlight.Player.PlayerId;
                vm.SpotlightName = spotlight.Player.DisplayName;
                vm.SpotlightTeam = spotlight.Player.TeamName ?? "";
                vm.SpotlightGoals = spotlight.Goals;
                vm.SpotlightAssists = spotlight.Assists;
                vm.SpotlightXg = spotlight.Xg;
                vm.SpotlightPassAccPct = spotlight.PassAccPct;

                vm.SpotlightPosition = await _db.MatchLineups
                    .Where(l => l.PlayerId == spotlight.Player.PlayerId && l.PositionName != null)
                    .GroupBy(l => l.PositionName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync() ?? "-";
            }

            var finishing = combined
                .Where(c => c.Shots >= 5)
                .OrderByDescending(c => c.Goals - c.Xg)
                .Take(6)
                .ToList();

            vm.FinishingRows = finishing.Select(c =>
            {
                decimal diff = c.Goals - c.Xg;
                double pct = Math.Min(100, (double)Math.Abs(diff) / 6.0 * 100);
                return new FinishingRowViewModel
                {
                    PlayerId = c.Player.PlayerId,
                    DisplayName = c.Player.DisplayName,
                    TeamName = c.Player.TeamName ?? "",
                    GoalsMinusXg = Math.Round(diff, 2),
                    BarWidthPct = pct
                };
            }).ToList();

            return View(vm);
        }
    }
}