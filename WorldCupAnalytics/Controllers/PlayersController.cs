using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;
using WorldCupAnalytics.Models;
using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private static readonly string[] ComparisonColors = { "#2E7D50", "#1F6E80", "#B8842A", "#6C5FBE" };

        public PlayersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<PlayerSearchResultDto>());

            var matches = await _db.Players
                .Where(p => p.PlayerName.Contains(q) || (p.PlayerNickname != null && p.PlayerNickname.Contains(q)))
                .Take(8)
                .ToListAsync();

            var results = new List<PlayerSearchResultDto>();
            foreach (var player in matches)
            {
                var playerShots = await _db.ShotEvents
                    .Where(s => s.PlayerId == player.PlayerId && s.Period != 5)
                    .ToListAsync();
                var goals = playerShots.Count(s => s.OutcomeName == "Goal");
                var xg = Math.Round(playerShots.Sum(s => s.StatsbombXg ?? 0), 2);
                var assists = await _db.PassEvents
                    .CountAsync(p => p.PlayerId == player.PlayerId && p.IsGoalAssist);
                var position = await GetMostCommonPositionAsync(player.PlayerId);

                results.Add(new PlayerSearchResultDto
                {
                    PlayerId = player.PlayerId,
                    DisplayName = player.DisplayName,
                    TeamName = player.TeamName ?? "",
                    Position = position,
                    Goals = goals,
                    Assists = assists,
                    Xg = xg,
                    Shots = playerShots.Count
                });
            }
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> Browse(string? team, string? group, string? pos, string? q)
        {
            var query = _db.Players.AsQueryable();
            if (!string.IsNullOrWhiteSpace(team)) query = query.Where(p => p.TeamName == team);
            if (!string.IsNullOrWhiteSpace(group)) query = query.Where(p => p.Team != null && p.Team.GroupLetter == group);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.PlayerName.Contains(q) || (p.PlayerNickname != null && p.PlayerNickname.Contains(q)));

            var players = await query.Take(200).ToListAsync();
            var results = new List<PlayerSearchResultDto>();

            foreach (var player in players)
            {
                var position = await GetMostCommonPositionAsync(player.PlayerId);
                if (!string.IsNullOrWhiteSpace(pos) && position != pos) continue;

                var goals = await _db.ShotEvents
                    .CountAsync(s => s.PlayerId == player.PlayerId && s.OutcomeName == "Goal" && s.Period != 5);
                var assists = await _db.PassEvents
                    .CountAsync(p => p.PlayerId == player.PlayerId && p.IsGoalAssist);

                results.Add(new PlayerSearchResultDto
                {
                    PlayerId = player.PlayerId,
                    DisplayName = player.DisplayName,
                    TeamName = player.TeamName ?? "",
                    Position = position,
                    Goals = goals,
                    Assists = assists
                });

                if (results.Count >= 40) break;
            }
            return Json(results);
        }

        public async Task<IActionResult> Compare(string? ids)
        {
            ViewData["Title"] = "Player Comparison";
            ViewData["ActiveTab"] = "compare";

            var idList = (ids ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .Distinct()
                .Take(4)
                .ToList();

            var vm = new ComparePageViewModel { SelectedIdsCsv = string.Join(",", idList) };

            if (idList.Any())
            {
                var players = await _db.Players.Where(p => idList.Contains(p.PlayerId)).ToListAsync();
                var rows = new List<ComparePlayerViewModel>();

                foreach (var player in players)
                {
                    var starts = await _db.MatchLineups
                        .CountAsync(l => l.PlayerId == player.PlayerId && l.IsStartingXi);

                    var shots = await _db.ShotEvents
                        .Where(s => s.PlayerId == player.PlayerId && s.Period != 5)
                        .ToListAsync();

                    var goals = shots.Count(s => s.OutcomeName == "Goal");
                    var xg = shots.Sum(s => s.StatsbombXg ?? 0);
                    var sot = shots.Count(s => s.OutcomeName == "Goal" || s.OutcomeName == "Saved" || s.OutcomeName == "Saved to Post");

                    var passes = await _db.PassEvents
                        .Where(p => p.PlayerId == player.PlayerId)
                        .ToListAsync();

                    var assists = passes.Count(p => p.IsGoalAssist);
                    var totalPasses = passes.Count;
                    var completedPasses = passes.Count(p => p.IsComplete);
                    var passAcc = totalPasses > 0 ? Math.Round((decimal)completedPasses / totalPasses * 100, 1) : 0;

                    var touchesCount = await _db.EventLocations.CountAsync(e => e.PlayerId == player.PlayerId);
                    var defStats = await GetDefensiveStatsAsync(player.PlayerId);
                    var duelDribbleStats = await GetDuelDribbleStatsAsync(player.PlayerId);

                    rows.Add(new ComparePlayerViewModel
                    {
                        PlayerId = player.PlayerId,
                        DisplayName = player.DisplayName,
                        TeamName = player.TeamName ?? "",
                        Position = await GetMostCommonPositionAsync(player.PlayerId),
                        Starts = starts,
                        Goals = goals,
                        Assists = assists,
                        Xg = Math.Round(xg, 2),
                        Shots = shots.Count,
                        ShotsOnTarget = sot,
                        PassAccuracyPct = passAcc,
                        TouchesCount = touchesCount,
                        Form = await GetLastFiveFormAsync(player.PlayerId, player.TeamId),
                        ShotMap = await BuildShotMapAsync(shots),
                        HeatMap = await BuildHeatMapAsync(player.PlayerId),
                        Pressures = defStats.Pressures,
                        Interceptions = defStats.Interceptions,
                        Clearances = defStats.Clearances,
                        Blocks = defStats.Blocks,
                        FoulsCommitted = defStats.FoulsCommitted,
                        FoulsWon = defStats.FoulsWon,
                        GroundDuelsWon = duelDribbleStats.GroundDuelsWon,
                        GroundDuelsTotal = duelDribbleStats.GroundDuelsTotal,
                        GroundDuelWinPct = duelDribbleStats.GroundDuelWinPct,
                        AerialDuelsLost = duelDribbleStats.AerialDuelsLost,
                        DribblesCompleted = duelDribbleStats.DribblesCompleted,
                        DribblesAttempted = duelDribbleStats.DribblesAttempted,
                        DribbleSuccessPct = duelDribbleStats.DribbleSuccessPct
                    });
                }

                var ordered = idList
                    .Select(id => rows.FirstOrDefault(r => r.PlayerId == id))
                    .Where(r => r != null)
                    .Select(r => r!)
                    .ToList();

                for (int i = 0; i < ordered.Count; i++)
                    ordered[i].ColorHex = ComparisonColors[i % ComparisonColors.Length];

                vm.Players = ordered;
                vm.RadarSeries = BuildRadarSeries(ordered);
            }

            vm.AllTeamNames = await _db.Teams.OrderBy(t => t.TeamName).Select(t => t.TeamName).ToListAsync();
            vm.AllGroupLetters = await _db.Teams
                .Where(t => t.GroupLetter != null)
                .Select(t => t.GroupLetter!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return View(vm);
        }

        // GET /Players/Profile?id=5503
        // Defaults to Messi if no id is given.
        public async Task<IActionResult> Profile(int? id)
        {
            ViewData["ActiveTab"] = "player";

            var player = id.HasValue
                ? await _db.Players.Include(p => p.Team).FirstOrDefaultAsync(p => p.PlayerId == id.Value)
                : await _db.Players.Include(p => p.Team)
                    .FirstOrDefaultAsync(p => p.PlayerName.Contains("Lionel Messi") || (p.PlayerNickname != null && p.PlayerNickname.Contains("Lionel Messi")));

            if (player == null) return NotFound();

            ViewData["Title"] = player.DisplayName;

            var shots = await _db.ShotEvents
                .Where(s => s.PlayerId == player.PlayerId && s.Period != 5)
                .ToListAsync();

            var passes = await _db.PassEvents
                .Where(p => p.PlayerId == player.PlayerId)
                .ToListAsync();

            var starts = await _db.MatchLineups
                .CountAsync(l => l.PlayerId == player.PlayerId && l.IsStartingXi);

            var totalPasses = passes.Count;
            var completedPasses = passes.Count(p => p.IsComplete);

            var vm = new PlayerProfileViewModel
            {
                PlayerId = player.PlayerId,
                DisplayName = player.DisplayName,
                TeamName = player.TeamName ?? "",
                GroupLetter = player.Team?.GroupLetter,
                JerseyNumber = player.JerseyNumber,
                Position = await GetMostCommonPositionAsync(player.PlayerId),

                Goals = shots.Count(s => s.OutcomeName == "Goal"),
                Xg = Math.Round(shots.Sum(s => s.StatsbombXg ?? 0), 2),
                Shots = shots.Count,
                ShotsOnTarget = shots.Count(s => s.OutcomeName == "Goal" || s.OutcomeName == "Saved" || s.OutcomeName == "Saved to Post"),
                Assists = passes.Count(p => p.IsGoalAssist),
                Starts = starts,
                TotalPasses = totalPasses,
                PassAccuracyPct = totalPasses > 0 ? Math.Round((decimal)completedPasses / totalPasses * 100, 1) : (decimal?)null,

                ShotMap = await BuildShotMapAsync(shots),
                PassMap = await BuildPassMapAsync(passes),
                HeatMap = await BuildHeatMapAsync(player.PlayerId),
                TouchesCount = await _db.EventLocations.CountAsync(e => e.PlayerId == player.PlayerId),

                MatchLog = await GetMatchLogAsync(player.PlayerId, player.TeamId),
                Form = await GetTeamFormAsync(player.TeamId)
            };

            var profileDefStats = await GetDefensiveStatsAsync(player.PlayerId);
            vm.Pressures = profileDefStats.Pressures;
            vm.Interceptions = profileDefStats.Interceptions;
            vm.Clearances = profileDefStats.Clearances;
            vm.Blocks = profileDefStats.Blocks;
            vm.FoulsCommitted = profileDefStats.FoulsCommitted;
            vm.FoulsWon = profileDefStats.FoulsWon;

            var profileDuelDribbleStats = await GetDuelDribbleStatsAsync(player.PlayerId);
            vm.GroundDuelsWon = profileDuelDribbleStats.GroundDuelsWon;
            vm.GroundDuelsTotal = profileDuelDribbleStats.GroundDuelsTotal;
            vm.GroundDuelWinPct = profileDuelDribbleStats.GroundDuelWinPct;
            vm.AerialDuelsLost = profileDuelDribbleStats.AerialDuelsLost;
            vm.DribblesCompleted = profileDuelDribbleStats.DribblesCompleted;
            vm.DribblesAttempted = profileDuelDribbleStats.DribblesAttempted;
            vm.DribbleSuccessPct = profileDuelDribbleStats.DribbleSuccessPct;

            return View(vm);
        }

        // Counts of each defensive/work-rate event type for this player,
        // pulled from event_locations.event_type. These are raw involvement
        // counts, NOT success rates — e.g. "Duel" doesn't distinguish won
        // vs lost in our extracted data, unlike shots/passes which do carry
        // a full outcome. Framed as workrate in the UI, not efficiency.
        private async Task<(int Pressures, int Interceptions, int Clearances, int Blocks, int FoulsCommitted, int FoulsWon)> GetDefensiveStatsAsync(int playerId)
        {
            var counts = await _db.EventLocations
                .Where(e => e.PlayerId == playerId)
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToListAsync();

            int Get(string type) => counts.FirstOrDefault(c => c.EventType == type)?.Count ?? 0;

            return (
                Pressures: Get("Pressure"),
                Interceptions: Get("Interception"),
                Clearances: Get("Clearance"),
                Blocks: Get("Block"),
                FoulsCommitted: Get("Foul Committed"),
                FoulsWon: Get("Foul Won")
            );
        }

        // Ground duels (tackles) have a real won/lost outcome; aerial duels
        // are losses-only in our data (StatsBomb doesn't tag the winning
        // side of an aerial as a separate Duel event), so they're reported
        // as a count, not blended into a misleading win percentage.
        private async Task<(int GroundDuelsWon, int GroundDuelsTotal, decimal GroundDuelWinPct, int AerialDuelsLost, int DribblesCompleted, int DribblesAttempted, decimal DribbleSuccessPct)> GetDuelDribbleStatsAsync(int playerId)
        {
            var duels = await _db.DuelEvents.Where(d => d.PlayerId == playerId).ToListAsync();
            var groundDuels = duels.Where(d => d.DuelType == "Tackle").ToList();
            int groundWon = groundDuels.Count(d => d.IsWon == true);
            int groundTotal = groundDuels.Count;
            decimal groundPct = groundTotal > 0 ? Math.Round(100m * groundWon / groundTotal, 0) : 0;
            int aerialLost = duels.Count(d => d.DuelType == "Aerial Lost");

            var dribbles = await _db.DribbleEvents.Where(d => d.PlayerId == playerId).ToListAsync();
            int dribbleComplete = dribbles.Count(d => d.IsComplete == true);
            int dribbleTotal = dribbles.Count;
            decimal dribblePct = dribbleTotal > 0 ? Math.Round(100m * dribbleComplete / dribbleTotal, 0) : 0;

            return (groundWon, groundTotal, groundPct, aerialLost, dribbleComplete, dribbleTotal, dribblePct);
        }

        // Converts each pass's real start/end pitch coordinates into SVG
        // pixel space, and looks up which match it happened in for the
        // tooltip. Green = completed, gold = incomplete, violet = an
        // assist (a completed pass that led directly to a goal) — colored
        // separately since that's a more significant event than a routine
        // completed pass.
        private async Task<List<PassLineViewModel>> BuildPassMapAsync(List<PassEvent> passes)
        {
            var matchIds = passes.Select(p => p.MatchId).Distinct().ToList();
            var matches = await _db.Matches.Where(m => matchIds.Contains(m.MatchId)).ToListAsync();
            var matchById = matches.ToDictionary(m => m.MatchId);

            var lines = new List<PassLineViewModel>();
            foreach (var pass in passes)
            {
                if (pass.LocX == null || pass.LocY == null || pass.EndLocX == null || pass.EndLocY == null) continue;

                double x1 = 10 + ((double)pass.LocX.Value / 120.0) * 400;
                double y1 = 10 + ((double)pass.LocY.Value / 80.0) * 280;
                double x2 = 10 + ((double)pass.EndLocX.Value / 120.0) * 400;
                double y2 = 10 + ((double)pass.EndLocY.Value / 80.0) * 280;

                string color = pass.IsGoalAssist ? "#6C5FBE" : (pass.IsComplete ? "#2E7D50" : "#B8842A");

                string matchLabel = "";
                if (matchById.TryGetValue(pass.MatchId, out var match))
                {
                    bool isHome = match.HomeTeamId == pass.TeamId;
                    string opponent = isHome ? match.AwayTeamName ?? "" : match.HomeTeamName ?? "";
                    matchLabel = $"vs {opponent} ({match.CompetitionStage}, {match.MatchDate:d MMM})";
                }

                string statusText = pass.IsGoalAssist
                    ? $"ASSIST to {pass.RecipientName ?? "teammate"}"
                    : pass.IsComplete
                        ? $"Complete to {pass.RecipientName ?? "teammate"} ({pass.PassHeightName})"
                        : $"{pass.OutcomeName ?? "Incomplete"} ({pass.PassHeightName})";

                string tooltip = $"{matchLabel} \u2014 {(pass.Minute.HasValue ? $"{pass.Minute}' " : "")}{statusText}";

                lines.Add(new PassLineViewModel
                {
                    SvgX1 = Math.Round(x1, 1),
                    SvgY1 = Math.Round(y1, 1),
                    SvgX2 = Math.Round(x2, 1),
                    SvgY2 = Math.Round(y2, 1),
                    IsComplete = pass.IsComplete,
                    IsGoalAssist = pass.IsGoalAssist,
                    Color = color,
                    MatchLabel = matchLabel,
                    TooltipText = tooltip
                });
            }
            return lines;
        }

        private async Task<string> GetMostCommonPositionAsync(int playerId)
        {
            var position = await _db.MatchLineups
                .Where(l => l.PlayerId == playerId && l.PositionName != null)
                .GroupBy(l => l.PositionName)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();
            return position ?? "-";
        }

        private async Task<List<string>> GetLastFiveFormAsync(int playerId, int teamId)
        {
            var startedMatchIds = await _db.MatchLineups
                .Where(l => l.PlayerId == playerId && l.IsStartingXi)
                .Select(l => l.MatchId)
                .ToListAsync();

            var matches = await _db.Matches
                .Where(m => startedMatchIds.Contains(m.MatchId))
                .OrderByDescending(m => m.MatchDate)
                .Take(5)
                .ToListAsync();

            var results = new List<string>();
            foreach (var match in matches.OrderBy(m => m.MatchDate))
            {
                if (match.HomeScore == null || match.AwayScore == null) continue;
                bool isHome = match.HomeTeamId == teamId;
                int teamScore = isHome ? match.HomeScore.Value : match.AwayScore.Value;
                int oppScore = isHome ? match.AwayScore.Value : match.HomeScore.Value;
                results.Add(teamScore > oppScore ? "W" : teamScore < oppScore ? "L" : "D");
            }
            return results;
        }

        // The team's last 5 tournament results overall — NOT conditioned on
        // whether this specific player featured, since "team form" is about
        // the squad's current run, which matters to a scout regardless of
        // one individual's involvement.
        private async Task<List<string>> GetTeamFormAsync(int teamId)
        {
            var matches = await _db.Matches
                .Where(m => (m.HomeTeamId == teamId || m.AwayTeamId == teamId) && m.HomeScore != null && m.AwayScore != null)
                .OrderByDescending(m => m.MatchDate)
                .Take(5)
                .ToListAsync();

            var results = new List<string>();
            foreach (var match in matches.OrderBy(m => m.MatchDate))
            {
                bool isHome = match.HomeTeamId == teamId;
                int teamScore = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
                int oppScore = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;
                results.Add(teamScore > oppScore ? "W" : teamScore < oppScore ? "L" : "D");
            }
            return results;
        }

        // Every match this player appeared in (lineup entry exists, starter
        // or sub), oldest to newest, with their personal goals/assists/xG/
        // shots for that specific match.
        private async Task<List<PlayerMatchLogRowViewModel>> GetMatchLogAsync(int playerId, int teamId)
        {
            var matchIds = await _db.MatchLineups
                .Where(l => l.PlayerId == playerId)
                .Select(l => l.MatchId)
                .ToListAsync();

            var matches = await _db.Matches
                .Where(m => matchIds.Contains(m.MatchId))
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var rows = new List<PlayerMatchLogRowViewModel>();
            foreach (var match in matches)
            {
                bool isHome = match.HomeTeamId == teamId;
                string opponent = isHome ? match.AwayTeamName ?? "" : match.HomeTeamName ?? "";

                var matchShots = await _db.ShotEvents
                    .Where(s => s.MatchId == match.MatchId && s.PlayerId == playerId && s.Period != 5)
                    .ToListAsync();

                var matchAssists = await _db.PassEvents
                    .CountAsync(p => p.MatchId == match.MatchId && p.PlayerId == playerId && p.IsGoalAssist);

                rows.Add(new PlayerMatchLogRowViewModel
                {
                    MatchDate = match.MatchDate,
                    Stage = match.CompetitionStage ?? "-",
                    Opponent = opponent,
                    Goals = matchShots.Count(s => s.OutcomeName == "Goal"),
                    Assists = matchAssists,
                    Xg = Math.Round(matchShots.Sum(s => s.StatsbombXg ?? 0), 2),
                    Shots = matchShots.Count
                });
            }
            return rows;
        }

        // Converts real pitch coordinates into SVG pixel space, and now
        // also looks up which match each shot happened in (opponent, stage,
        // date) for the tooltip — hence this is async, unlike before.
        private async Task<List<ShotDotViewModel>> BuildShotMapAsync(List<ShotEvent> shots)
        {
            var matchIds = shots.Select(s => s.MatchId).Distinct().ToList();
            var matches = await _db.Matches
                .Where(m => matchIds.Contains(m.MatchId))
                .ToListAsync();
            var matchById = matches.ToDictionary(m => m.MatchId);

            var dots = new List<ShotDotViewModel>();
            foreach (var shot in shots)
            {
                if (shot.LocX == null || shot.LocY == null) continue;
                double svgX = 10 + ((double)shot.LocX.Value / 120.0) * 400;
                double svgY = 10 + ((double)shot.LocY.Value / 80.0) * 280;

                string color; double radius;
                if (shot.OutcomeName == "Goal") { color = "#B8842A"; radius = 8; }
                else if (shot.OutcomeName == "Saved" || shot.OutcomeName == "Saved to Post") { color = "#2E7D50"; radius = 6; }
                else { color = "rgba(20,24,28,0.25)"; radius = 6; }

                string matchLabel = "";
                if (matchById.TryGetValue(shot.MatchId, out var match))
                {
                    bool isHome = match.HomeTeamId == shot.TeamId;
                    string opponent = isHome ? match.AwayTeamName ?? "" : match.HomeTeamName ?? "";
                    matchLabel = $"vs {opponent} ({match.CompetitionStage}, {match.MatchDate:d MMM})";
                }

                dots.Add(new ShotDotViewModel
                {
                    SvgX = Math.Round(svgX, 1),
                    SvgY = Math.Round(svgY, 1),
                    Radius = radius,
                    Color = color,
                    OutcomeName = shot.OutcomeName ?? "Attempt",
                    Minute = shot.Minute,
                    Xg = shot.StatsbombXg.HasValue ? Math.Round(shot.StatsbombXg.Value, 2) : (decimal?)null,
                    MatchLabel = matchLabel
                });
            }

            // Draw goals LAST so they always render on top — otherwise a
            // saved/missed shot at nearly the same pixel position can
            // visually cover a goal dot underneath it.
            var ordered = dots.OrderBy(d => d.OutcomeName == "Goal" ? 1 : 0).ToList();

            // Spread out any shots landing at (near-)identical positions —
            // e.g. two goals from the same spot — so both stay visible
            // instead of one perfectly hiding the other.
            return WorldCupAnalytics.Helpers.ChartMathHelper.DeoverlapDots(ordered);
        }

        private async Task<List<HeatCellViewModel>> BuildHeatMapAsync(int playerId)
        {
            var locations = await _db.EventLocations
                .Where(e => e.PlayerId == playerId)
                .Select(e => new { e.LocX, e.LocY })
                .ToListAsync();

            const int cols = 8, rows = 6;
            var counts = new int[cols, rows];
            foreach (var loc in locations)
            {
                int col = (int)Math.Min(cols - 1, (double)loc.LocX / 120.0 * cols);
                int row = (int)Math.Min(rows - 1, (double)loc.LocY / 80.0 * rows);
                if (col < 0 || row < 0) continue;
                counts[col, row]++;
            }

            int maxCount = 0;
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                    maxCount = Math.Max(maxCount, counts[c, r]);

            var cells = new List<HeatCellViewModel>();
            double cellW = 400.0 / cols, cellH = 280.0 / rows;

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    double density = maxCount > 0 ? (double)counts[c, r] / maxCount : 0;
                    cells.Add(new HeatCellViewModel
                    {
                        SvgX = Math.Round(10 + c * cellW, 1),
                        SvgY = Math.Round(10 + r * cellH, 1),
                        Width = Math.Round(cellW, 1),
                        Height = Math.Round(cellH, 1),
                        Opacity = Math.Round(0.05 + density * 0.9, 2),
                        TouchCount = counts[c, r]
                    });
                }
            }
            return cells;
        }

        private List<RadarSeriesViewModel> BuildRadarSeries(List<ComparePlayerViewModel> players)
        {
            const double cx = 190, cy = 170, r = 140;
            var axes = new (string Label, Func<ComparePlayerViewModel, double> Value, Func<ComparePlayerViewModel, string> Display)[]
            {
                ("GOALS",     p => p.Goals,               p => p.Goals.ToString()),
                ("xG",        p => (double)p.Xg,          p => p.Xg.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("ASSISTS",   p => p.Assists,              p => p.Assists.ToString()),
                ("SHOTS",     p => p.Shots,                p => p.Shots.ToString()),
                ("PASS ACC.", p => (double)p.PassAccuracyPct, p => p.PassAccuracyPct.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%"),
                ("TOUCHES",   p => p.TouchesCount,         p => p.TouchesCount.ToString())
            };

            var maxes = axes.Select(a => players.Select(a.Value).DefaultIfEmpty(0).Max() is var m && m > 0 ? m : 1).ToArray();

            (double x, double y) RadarPoint(double angleDeg, double pct)
            {
                double rad = Math.PI / 180.0 * angleDeg;
                double dist = r * (pct / 100.0);
                return (cx + dist * Math.Sin(rad), cy - dist * Math.Cos(rad));
            }

            double NormPct(double val, double max) => max <= 0 ? 0 : Math.Max(4, Math.Min(100, Math.Round(100 * val / max)));

            var series = new List<RadarSeriesViewModel>();
            foreach (var p in players)
            {
                var pts = new List<RadarAxisPointViewModel>();
                var polyCoords = new List<string>();

                for (int i = 0; i < axes.Length; i++)
                {
                    double raw = axes[i].Value(p);
                    double pct = NormPct(raw, maxes[i]);
                    var (x, y) = RadarPoint(i * (360.0 / axes.Length), pct);

                    pts.Add(new RadarAxisPointViewModel
                    {
                        X = Math.Round(x, 1),
                        Y = Math.Round(y, 1),
                        AxisLabel = axes[i].Label,
                        RawValueDisplay = axes[i].Display(p),
                        Percentile = pct
                    });
                    polyCoords.Add($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                }

                series.Add(new RadarSeriesViewModel
                {
                    PlayerName = p.DisplayName,
                    ColorHex = p.ColorHex,
                    PolygonPoints = string.Join(" ", polyCoords),
                    Points = pts
                });
            }
            return series;
        }
    }
}