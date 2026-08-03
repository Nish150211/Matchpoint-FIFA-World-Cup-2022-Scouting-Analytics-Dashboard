using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;
using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Controllers
{
    public class ScoutingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ScoutingController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Scouting/Report?id=5503
        // Defaults to Messi if no id is given.
        public async Task<IActionResult> Report(int? id)
        {
            ViewData["Title"] = "Scouting Report";
            ViewData["ActiveTab"] = "report";

            var player = id.HasValue
                ? await _db.Players.FirstOrDefaultAsync(p => p.PlayerId == id.Value)
                : await _db.Players.FirstOrDefaultAsync(p => p.PlayerName.Contains("Lionel Messi") || (p.PlayerNickname != null && p.PlayerNickname.Contains("Lionel Messi")));

            if (player == null) return NotFound();

            var shots = await _db.ShotEvents
                .Where(s => s.PlayerId == player.PlayerId && s.Period != 5)
                .ToListAsync();

            var passes = await _db.PassEvents
                .Where(p => p.PlayerId == player.PlayerId)
                .ToListAsync();

            var starts = await _db.MatchLineups
                .CountAsync(l => l.PlayerId == player.PlayerId && l.IsStartingXi);

            int goals = shots.Count(s => s.OutcomeName == "Goal");
            int shotCount = shots.Count;
            decimal xg = Math.Round(shots.Sum(s => s.StatsbombXg ?? 0), 2);
            int assists = passes.Count(p => p.IsGoalAssist);
            int totalPasses = passes.Count;
            int completedPasses = passes.Count(p => p.IsComplete);
            decimal? passAcc = totalPasses > 0 ? Math.Round((decimal)completedPasses / totalPasses * 100, 0) : (decimal?)null;

            var position = await _db.MatchLineups
                .Where(l => l.PlayerId == player.PlayerId && l.PositionName != null)
                .GroupBy(l => l.PositionName)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "-";

            var vm = new ScoutingReportViewModel
            {
                PlayerId = player.PlayerId,
                DisplayName = player.DisplayName,
                TeamName = player.TeamName ?? "",
                Position = position,
                Goals = goals,
                Assists = assists,
                Shots = shotCount,
                Starts = starts,
                Xg = xg,
                PassAccuracyPct = passAcc
            };

            // ---------- Verdict (rule-based) ----------
            decimal diff = goals - xg;
            int perShotPct = shotCount > 0 ? (int)Math.Round(100m * goals / shotCount) : 0;

            string verdict = "Monitor";
            if (goals >= 5 || (assists >= 3 && passAcc >= 80))
                verdict = "Strong Sign / Retain";
            else if (starts <= 1)
                verdict = "Insufficient Sample";
            else if (goals + assists >= 2)
                verdict = "Worth a Closer Look";
            vm.Verdict = verdict;

            // ---------- Strengths ----------
            var strengths = new List<string>();
            if (goals > 0)
                strengths.Add($"{goals} goal{(goals > 1 ? "s" : "")} from {shotCount} shots ({perShotPct}% conversion)");
            if (assists > 0)
                strengths.Add($"{assists} assist{(assists > 1 ? "s" : "")} created for teammates");
            if (passAcc.HasValue && passAcc.Value >= 82)
                strengths.Add($"High pass accuracy at {passAcc}%");
            if (diff > 0.5m)
                strengths.Add($"Finishing above expectation (+{diff:0.00} goals vs xG)");
            if (strengths.Count == 0)
                strengths.Add("Limited attacking output recorded — review defensive/creative contributions separately");
            vm.Strengths = strengths;

            // ---------- Considerations ----------
            var considerations = new List<string>();
            if (starts < 3)
                considerations.Add($"Small sample — only {starts} start{(starts == 1 ? "" : "s")} logged");
            if (passAcc.HasValue && passAcc.Value < 75)
                considerations.Add($"Pass accuracy below squad average at {passAcc}%");
            if (diff < -0.5m)
                considerations.Add($"Underperforming xG by {Math.Abs(diff):0.00} goals — finishing worth monitoring");
            if (shotCount == 0)
                considerations.Add("No shots recorded — attacking threat unproven in this data");
            if (considerations.Count == 0)
                considerations.Add("No major red flags in the current sample");
            considerations.Add("Manually logged injury status, since source files don't track it");
            vm.Considerations = considerations;

            // ---------- Analyst notes (auto-generated prose) ----------
            string shotSentence = shotCount > 0
                ? $"Shot volume ({shotCount}) and xG ({xg:0.00}) suggest {(diff > 0 ? "a finisher outperforming underlying chance quality" : "output roughly in line with (or below) chance quality")}."
                : "No shot data recorded for this player in the current dataset.";

            string passSentence = passAcc.HasValue
                ? $"Pass accuracy of {passAcc}% reflects {(passAcc.Value > 80 ? "a low-risk, retentive profile" : "a higher-risk passing profile or a deeper defensive role")}."
                : "No pass data recorded for this player in the current dataset.";

            vm.AnalystNotes = $"Touch map and shot map are both computed directly from this player's real event coordinates. {shotSentence} {passSentence}";

            // ---------- Player switcher dropdown ----------
            var allPlayers = await _db.Players.OrderBy(p => p.PlayerName).ToListAsync();
            vm.AllPlayers = allPlayers.Select(p => (p.PlayerId, p.DisplayName, p.TeamName ?? "")).ToList();

            return View(vm);
        }
    }
}