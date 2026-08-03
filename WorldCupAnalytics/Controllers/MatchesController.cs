using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Data;
using WorldCupAnalytics.Models;
using WorldCupAnalytics.Models.ViewModels;

namespace WorldCupAnalytics.Controllers
{
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MatchesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Matches/Detail?id=3869685
        public async Task<IActionResult> Detail(long id)
        {
            ViewData["ActiveTab"] = "teams"; // closest existing tab — this page is reached via Teams

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == id);
            if (match == null || match.HomeScore == null || match.AwayScore == null) return NotFound();

            ViewData["Title"] = $"{match.HomeTeamName} vs {match.AwayTeamName}";

            var homeShots = await _db.ShotEvents
                .Where(s => s.MatchId == id && s.TeamId == match.HomeTeamId && s.Period != 5)
                .ToListAsync();
            var awayShots = await _db.ShotEvents
                .Where(s => s.MatchId == id && s.TeamId == match.AwayTeamId && s.Period != 5)
                .ToListAsync();

            var homePasses = await _db.PassEvents.Where(p => p.MatchId == id && p.TeamId == match.HomeTeamId).ToListAsync();
            var awayPasses = await _db.PassEvents.Where(p => p.MatchId == id && p.TeamId == match.AwayTeamId).ToListAsync();

            const string homeColor = "#2E7D50", awayColor = "#1F6E80";

            var vm = new MatchDetailViewModel
            {
                MatchId = match.MatchId,
                MatchDate = match.MatchDate,
                Stage = match.CompetitionStage ?? "-",
                StadiumName = match.StadiumName,
                StadiumCountry = match.StadiumCountry,
                RefereeName = match.RefereeName,

                HomeTeamId = match.HomeTeamId,
                HomeTeamName = match.HomeTeamName ?? "",
                HomeScore = match.HomeScore.Value,
                HomeColorHex = homeColor,

                AwayTeamId = match.AwayTeamId,
                AwayTeamName = match.AwayTeamName ?? "",
                AwayScore = match.AwayScore.Value,
                AwayColorHex = awayColor,

                // Home team's shots plotted as recorded; away team's shots
                // MIRRORED (both x and y flipped) so on one shared full
                // pitch, each team's shots cluster near the goal they were
                // actually attacking — otherwise both teams' shots would
                // overlap near the same end, since StatsBomb always
                // records each event relative to that team's own
                // attacking direction.
                HomeShotMap = BuildMatchShotMap(homeShots, mirror: false, colorGoal: "#B8842A", colorTeam: homeColor),
                AwayShotMap = BuildMatchShotMap(awayShots, mirror: true, colorGoal: "#B8842A", colorTeam: awayColor),

                HomeShots = homeShots.Count,
                HomeShotsOnTarget = homeShots.Count(s => s.OutcomeName == "Goal" || s.OutcomeName == "Saved" || s.OutcomeName == "Saved to Post"),
                HomeXg = Math.Round(homeShots.Sum(s => s.StatsbombXg ?? 0), 2),
                HomePassAccuracyPct = homePasses.Count > 0 ? Math.Round(100m * homePasses.Count(p => p.IsComplete) / homePasses.Count, 0) : 0,

                AwayShots = awayShots.Count,
                AwayShotsOnTarget = awayShots.Count(s => s.OutcomeName == "Goal" || s.OutcomeName == "Saved" || s.OutcomeName == "Saved to Post"),
                AwayXg = Math.Round(awayShots.Sum(s => s.StatsbombXg ?? 0), 2),
                AwayPassAccuracyPct = awayPasses.Count > 0 ? Math.Round(100m * awayPasses.Count(p => p.IsComplete) / awayPasses.Count, 0) : 0
            };

            // Goal timeline — both teams' goals, chronological
            var allGoals = homeShots.Where(s => s.OutcomeName == "Goal").Select(s => (Shot: s, Team: match.HomeTeamName ?? "", Color: homeColor))
                .Concat(awayShots.Where(s => s.OutcomeName == "Goal").Select(s => (Shot: s, Team: match.AwayTeamName ?? "", Color: awayColor)))
                .OrderBy(x => x.Shot.Period).ThenBy(x => x.Shot.Minute)
                .ToList();

            vm.GoalTimeline = allGoals.Select(g => new GoalEventViewModel
            {
                Minute = g.Shot.Minute ?? 0,
                PlayerId = g.Shot.PlayerId,
                PlayerName = g.Shot.PlayerName ?? "Unknown",
                TeamName = g.Team,
                TeamColorHex = g.Color,
                Technique = g.Shot.TechniqueName ?? "",
                ShotType = g.Shot.ShotTypeName ?? "Open Play",
                Xg = g.Shot.StatsbombXg.HasValue ? Math.Round(g.Shot.StatsbombXg.Value, 2) : (decimal?)null
            }).ToList();

            vm.HomeLineup = await GetLineupAsync(id, match.HomeTeamId);
            vm.AwayLineup = await GetLineupAsync(id, match.AwayTeamId);

            vm.HomePassNetwork = await BuildPassNetworkAsync(id, match.HomeTeamId);
            vm.AwayPassNetwork = await BuildPassNetworkAsync(id, match.AwayTeamId);

            vm.HomeFormation = await BuildFormationMapAsync(id, match.HomeTeamId);
            vm.AwayFormation = await BuildFormationMapAsync(id, match.AwayTeamId);

            vm.Substitutions = await GetSubstitutionsAsync(id, match.HomeTeamId, match.HomeTeamName ?? "", homeColor, match.AwayTeamId, match.AwayTeamName ?? "", awayColor);

            // Extra time (periods 3/4) extends the chart's x-axis to 120
            // minutes; a normal 90-minute match keeps the tighter scale.
            bool wentToExtraTime = homeShots.Any(s => s.Period >= 3) || awayShots.Any(s => s.Period >= 3);
            vm.XgChartMaxMinutes = wentToExtraTime ? 120 : 90;

            var allMatchXg = homeShots.Sum(s => s.StatsbombXg ?? 0) + awayShots.Sum(s => s.StatsbombXg ?? 0);
            decimal sharedMaxXg = Math.Max(homeShots.Sum(s => s.StatsbombXg ?? 0), awayShots.Sum(s => s.StatsbombXg ?? 0));
            if (sharedMaxXg <= 0) sharedMaxXg = 1;

            vm.HomeXgRace = BuildXgRaceSeries(homeShots, homeColor, match.HomeTeamName ?? "", sharedMaxXg, vm.XgChartMaxMinutes);
            vm.AwayXgRace = BuildXgRaceSeries(awayShots, awayColor, match.AwayTeamName ?? "", sharedMaxXg, vm.XgChartMaxMinutes);

            vm.Possession = await BuildPossessionChartAsync(id, match.HomeTeamId, vm.XgChartMaxMinutes);

            return View(vm);
        }

        // Turns possession_sequences (which team had the ball, and when
        // each sequence started) into an overall % split and a running
        // cumulative-to-date line showing how that % evolved through the
        // match. Sequence duration = time until the NEXT sequence starts —
        // the standard way to convert "who started this passage of play"
        // into actual time-controlled.
        private async Task<PossessionChartViewModel> BuildPossessionChartAsync(long matchId, int homeTeamId, int maxMinutes)
        {
            var sequences = await _db.PossessionSequences
                .Where(p => p.MatchId == matchId)
                .OrderBy(p => p.StartMinute).ThenBy(p => p.StartSecond)
                .ToListAsync();

            const double xStart = 40, xEnd = 700;
            double XForMinute(double minute) => xStart + Math.Min(minute, maxMinutes) / (double)maxMinutes * (xEnd - xStart);

            double homeSeconds = 0, awaySeconds = 0;
            var runningPoints = new List<(double X, double Y)>();
            var tooltipPoints = new List<PossessionPointViewModel>();

            for (int i = 0; i < sequences.Count; i++)
            {
                double startTotalSeconds = sequences[i].StartMinute * 60 + sequences[i].StartSecond;
                double endTotalSeconds = i + 1 < sequences.Count
                    ? sequences[i + 1].StartMinute * 60 + sequences[i + 1].StartSecond
                    : startTotalSeconds; // last sequence contributes ~0 extra, negligible for the chart

                double duration = Math.Max(0, endTotalSeconds - startTotalSeconds);

                if (sequences[i].TeamId == homeTeamId) homeSeconds += duration;
                else awaySeconds += duration;

                double totalSoFar = homeSeconds + awaySeconds;
                double homePctSoFar = totalSoFar > 0 ? 100.0 * homeSeconds / totalSoFar : 50.0;

                double xMinute = endTotalSeconds / 60.0;
                double y = 190 - (homePctSoFar / 100.0) * 170; // 190 = 0%, 20 = 100%, matches other charts' y-range
                double svgX = XForMinute(xMinute);

                runningPoints.Add((svgX, y));
                tooltipPoints.Add(new PossessionPointViewModel
                {
                    SvgX = Math.Round(svgX, 1),
                    SvgY = Math.Round(y, 1),
                    Minute = (int)Math.Round(xMinute),
                    HomePctAtPoint = Math.Round((decimal)homePctSoFar, 1),
                    AwayPctAtPoint = Math.Round(100m - (decimal)homePctSoFar, 1)
                });
            }

            string polyline = string.Join(" ", runningPoints.Select(p =>
                $"{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            decimal finalTotal = (decimal)(homeSeconds + awaySeconds);
            decimal homePct = finalTotal > 0 ? Math.Round(100m * (decimal)homeSeconds / finalTotal, 1) : 50m;

            return new PossessionChartViewModel
            {
                HomePossessionPct = homePct,
                AwayPossessionPct = Math.Round(100m - homePct, 1),
                HomeRunningPctPolyline = polyline,
                Points = tooltipPoints
            };
        }

        // Builds the step-shaped cumulative xG line for one team, plus
        // marker points at each actual goal. The line jumps at the exact
        // minute of each shot and stays flat between shots — a smooth
        // diagonal would wrongly imply xG accrues gradually over time.
        // Both teams share the same y-axis scale (sharedMaxXg) so line
        // height is genuinely comparable between them.
        private XgRaceSeriesViewModel BuildXgRaceSeries(List<ShotEvent> shots, string colorHex, string teamName, decimal sharedMaxXg, int maxMinutes)
        {
            const double xStart = 40, xEnd = 700, yTop = 20, yBottom = 190;

            double XForMinute(int minute) => xStart + Math.Min(minute, maxMinutes) / (double)maxMinutes * (xEnd - xStart);
            double YForXg(decimal xg) => yBottom - (double)(xg / sharedMaxXg) * (yBottom - yTop);

            var ordered = shots.Where(s => s.Minute.HasValue).OrderBy(s => s.Period).ThenBy(s => s.Minute).ThenBy(s => s.Second).ToList();

            var points = new List<(double X, double Y)> { (xStart, yBottom) };
            decimal cumulative = 0;
            var markers = new List<XgRaceMarkerViewModel>();
            var allShotMarkers = new List<XgRaceMarkerViewModel>();

            foreach (var shot in ordered)
            {
                int minute = shot.Minute!.Value;
                double xBefore = XForMinute(minute);
                points.Add((xBefore, YForXg(cumulative))); // flat up to this minute

                decimal shotXg = Math.Round(shot.StatsbombXg ?? 0, 2);
                cumulative += shot.StatsbombXg ?? 0;
                points.Add((xBefore, YForXg(cumulative))); // vertical jump

                var marker = new XgRaceMarkerViewModel
                {
                    SvgX = Math.Round(xBefore, 1),
                    SvgY = Math.Round(YForXg(cumulative), 1),
                    PlayerName = shot.PlayerName ?? "Unknown",
                    Minute = minute,
                    CumulativeXg = Math.Round(cumulative, 2),
                    OutcomeName = shot.OutcomeName ?? "Attempt",
                    ShotXg = shotXg
                };
                allShotMarkers.Add(marker);

                if (shot.OutcomeName == "Goal")
                    markers.Add(marker);
            }
            points.Add((XForMinute(maxMinutes), YForXg(cumulative))); // extend flat line to full time

            string polyline = string.Join(" ", points.Select(p =>
                $"{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            return new XgRaceSeriesViewModel
            {
                TeamName = teamName,
                ColorHex = colorHex,
                StepPolylinePoints = polyline,
                GoalMarkers = markers,
                AllShotMarkers = allShotMarkers,
                FinalXg = Math.Round(cumulative, 2)
            };
        }

        // Average position for each starting-XI player, based on ALL their
        // located events in this match (passes, carries, pressures,
        // duels, etc.) — not just passes, for a fuller picture of where
        // they actually spent the match than a pass-only average gives.
        private async Task<List<FormationNodeViewModel>> BuildFormationMapAsync(long matchId, int teamId)
        {
            var startingXi = await _db.MatchLineups
                .Where(l => l.MatchId == matchId && l.TeamId == teamId && l.IsStartingXi)
                .ToListAsync();

            var starterIds = startingXi.Select(l => l.PlayerId).ToList();
            if (starterIds.Count == 0) return new List<FormationNodeViewModel>();

            var players = await _db.Players.Where(p => starterIds.Contains(p.PlayerId)).ToListAsync();
            var playerById = players.ToDictionary(p => p.PlayerId);

            var allLocations = await _db.EventLocations
                .Where(e => e.MatchId == matchId && e.PlayerId != null && starterIds.Contains(e.PlayerId.Value))
                .Select(e => new { e.PlayerId, e.LocX, e.LocY })
                .ToListAsync();

            var nodes = new List<FormationNodeViewModel>();
            foreach (var lineup in startingXi)
            {
                var playerEvents = allLocations.Where(e => e.PlayerId == lineup.PlayerId).ToList();
                double avgX = playerEvents.Count > 0 ? (double)playerEvents.Average(e => e.LocX) : 60;
                double avgY = playerEvents.Count > 0 ? (double)playerEvents.Average(e => e.LocY) : 40;

                nodes.Add(new FormationNodeViewModel
                {
                    PlayerId = lineup.PlayerId,
                    DisplayName = playerById.TryGetValue(lineup.PlayerId, out var pl) ? pl.DisplayName : "Unknown",
                    Position = lineup.PositionName ?? "-",
                    JerseyNumber = lineup.JerseyNumber,
                    SvgX = Math.Round(10 + (avgX / 120.0) * 400, 1),
                    SvgY = Math.Round(10 + (avgY / 80.0) * 280, 1)
                });
            }
            return nodes;
        }

        private async Task<List<SubstitutionRowViewModel>> GetSubstitutionsAsync(long matchId, int homeTeamId, string homeTeamName, string homeColor, int awayTeamId, string awayTeamName, string awayColor)
        {
            var subs = await _db.SubstitutionEvents
                .Where(s => s.MatchId == matchId)
                .OrderBy(s => s.Period).ThenBy(s => s.Minute)
                .ToListAsync();

            return subs.Select(s => new SubstitutionRowViewModel
            {
                Minute = s.Minute ?? 0,
                TeamName = s.TeamId == homeTeamId ? homeTeamName : awayTeamName,
                TeamColorHex = s.TeamId == homeTeamId ? homeColor : awayColor,
                PlayerOffId = s.PlayerOffId,
                PlayerOffName = s.PlayerOffName ?? "Unknown",
                PlayerOnId = s.PlayerOnId,
                PlayerOnName = s.PlayerOnName ?? "Unknown",
                Reason = s.OutcomeName
            }).ToList();
        }

        // Builds a pass network for one team in one match: starting XI
        // positioned at their average pass-origin location, connected by
        // lines weighted by how many completed passes each pair combined
        // for (both directions merged into one line).
        private async Task<PassNetworkViewModel> BuildPassNetworkAsync(long matchId, int teamId)
        {
            var startingXi = await _db.MatchLineups
                .Where(l => l.MatchId == matchId && l.TeamId == teamId && l.IsStartingXi)
                .ToListAsync();
            var starterIds = startingXi.Select(l => l.PlayerId).ToHashSet();

            if (starterIds.Count == 0) return new PassNetworkViewModel();

            var players = await _db.Players.Where(p => starterIds.Contains(p.PlayerId)).ToListAsync();
            var playerById = players.ToDictionary(p => p.PlayerId);

            var teamPasses = await _db.PassEvents
                .Where(p => p.MatchId == matchId && p.TeamId == teamId && p.PlayerId != null)
                .ToListAsync();

            var network = new PassNetworkViewModel();
            var totalPassesByPlayer = new Dictionary<int, int>();
            int maxTotalPasses = 1;

            // Nodes: average position from this player's own pass attempts
            // in this match (where they made the pass from, not where it landed)
            foreach (var playerId in starterIds)
            {
                var ownPasses = teamPasses.Where(p => p.PlayerId == playerId).ToList();
                int total = ownPasses.Count;
                totalPassesByPlayer[playerId] = total;
                maxTotalPasses = Math.Max(maxTotalPasses, total);

                var withLoc = ownPasses.Where(p => p.LocX.HasValue && p.LocY.HasValue).ToList();
                double avgX = withLoc.Count > 0 ? (double)withLoc.Average(p => p.LocX!.Value) : 60;
                double avgY = withLoc.Count > 0 ? (double)withLoc.Average(p => p.LocY!.Value) : 40;

                double svgX = 10 + (avgX / 120.0) * 400;
                double svgY = 10 + (avgY / 80.0) * 280;

                network.Nodes.Add(new PassNetworkNodeViewModel
                {
                    PlayerId = playerId,
                    DisplayName = playerById.TryGetValue(playerId, out var pl) ? pl.DisplayName : "Unknown",
                    Position = startingXi.FirstOrDefault(l => l.PlayerId == playerId)?.PositionName ?? "-",
                    SvgX = Math.Round(svgX, 1),
                    SvgY = Math.Round(svgY, 1),
                    TotalPasses = total
                });
            }

            // Scale node radius 10-24 based on relative involvement
            foreach (var node in network.Nodes)
                node.Radius = Math.Round(10 + 14.0 * node.TotalPasses / maxTotalPasses, 1);

            // Edges: completed passes between two starting-XI teammates,
            // both directions combined into one pair count
            var pairCounts = new Dictionary<(int, int), int>();
            foreach (var pass in teamPasses)
            {
                if (!pass.IsComplete || pass.PlayerId == null || pass.RecipientId == null) continue;
                int a = pass.PlayerId.Value, b = pass.RecipientId.Value;
                if (!starterIds.Contains(a) || !starterIds.Contains(b) || a == b) continue;

                var key = a < b ? (a, b) : (b, a);
                pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
            }

            // Only draw pairs with a meaningful number of combinations —
            // keeps the diagram readable instead of a tangle of faint lines
            const int minPassesToShow = 3;
            int maxPairCount = pairCounts.Count > 0 ? pairCounts.Values.Max() : 1;

            var nodeById = network.Nodes.ToDictionary(n => n.PlayerId);
            foreach (var (pair, count) in pairCounts)
            {
                if (count < minPassesToShow) continue;
                if (!nodeById.TryGetValue(pair.Item1, out var nodeA) || !nodeById.TryGetValue(pair.Item2, out var nodeB)) continue;

                network.Edges.Add(new PassNetworkEdgeViewModel
                {
                    X1 = nodeA.SvgX,
                    Y1 = nodeA.SvgY,
                    X2 = nodeB.SvgX,
                    Y2 = nodeB.SvgY,
                    CombinedPassCount = count,
                    StrokeWidth = Math.Round(1 + 6.0 * count / maxPairCount, 1)
                });
            }

            return network;
        }

        private List<ShotDotViewModel> BuildMatchShotMap(List<ShotEvent> shots, bool mirror, string colorGoal, string colorTeam)
        {
            var dots = new List<ShotDotViewModel>();
            foreach (var shot in shots)
            {
                if (shot.LocX == null || shot.LocY == null) continue;

                double rawX = (double)shot.LocX.Value;
                double rawY = (double)shot.LocY.Value;
                double x = mirror ? 120 - rawX : rawX;
                double y = mirror ? 80 - rawY : rawY;

                double svgX = 10 + (x / 120.0) * 400;
                double svgY = 10 + (y / 80.0) * 280;

                bool isGoal = shot.OutcomeName == "Goal";

                dots.Add(new ShotDotViewModel
                {
                    SvgX = Math.Round(svgX, 1),
                    SvgY = Math.Round(svgY, 1),
                    Radius = isGoal ? 8 : 5,
                    Color = isGoal ? colorGoal : colorTeam,
                    OutcomeName = shot.OutcomeName ?? "Attempt",
                    Minute = shot.Minute,
                    Xg = shot.StatsbombXg.HasValue ? Math.Round(shot.StatsbombXg.Value, 2) : (decimal?)null,
                    MatchLabel = shot.PlayerName ?? ""
                });
            }
            // Goals last, same reasoning as the player shot map, then
            // spread any near-identical positions apart so duplicates
            // (e.g. two goals from the same spot) both stay visible.
            var ordered = dots.OrderBy(d => d.OutcomeName == "Goal" ? 1 : 0).ToList();
            return WorldCupAnalytics.Helpers.ChartMathHelper.DeoverlapDots(ordered);
        }

        private async Task<List<LineupRowViewModel>> GetLineupAsync(long matchId, int teamId)
        {
            var lineups = await _db.MatchLineups
                .Where(l => l.MatchId == matchId && l.TeamId == teamId)
                .ToListAsync();

            var playerIds = lineups.Select(l => l.PlayerId).ToList();
            var players = await _db.Players.Where(p => playerIds.Contains(p.PlayerId)).ToListAsync();
            var playerById = players.ToDictionary(p => p.PlayerId);

            return lineups
                .OrderByDescending(l => l.IsStartingXi)
                .ThenBy(l => l.JerseyNumber ?? 99)
                .Select(l => new LineupRowViewModel
                {
                    PlayerId = l.PlayerId,
                    PlayerName = playerById.TryGetValue(l.PlayerId, out var p) ? p.DisplayName : "Unknown",
                    JerseyNumber = l.JerseyNumber,
                    Position = l.PositionName ?? "-",
                    IsStartingXi = l.IsStartingXi
                })
                .ToList();
        }
    }
}