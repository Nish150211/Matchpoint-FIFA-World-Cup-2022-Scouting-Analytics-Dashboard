using Microsoft.EntityFrameworkCore;
using WorldCupAnalytics.Models;

namespace WorldCupAnalytics.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<MatchLineup> MatchLineups => Set<MatchLineup>();
        public DbSet<ShotEvent> ShotEvents => Set<ShotEvent>();
        public DbSet<PassEvent> PassEvents => Set<PassEvent>();
        public DbSet<EventLocation> EventLocations => Set<EventLocation>();
        public DbSet<DuelEvent> DuelEvents => Set<DuelEvent>();
        public DbSet<DribbleEvent> DribbleEvents => Set<DribbleEvent>();
        public DbSet<SubstitutionEvent> SubstitutionEvents => Set<SubstitutionEvent>();
        public DbSet<PossessionSequence> PossessionSequences => Set<PossessionSequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>(e =>
            {
                e.ToTable("teams");
                e.HasKey(x => x.TeamId);
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.TeamName).HasColumnName("team_name");
                e.Property(x => x.GroupLetter).HasColumnName("group_letter");
            });

            modelBuilder.Entity<Player>(e =>
            {
                e.ToTable("players");
                e.HasKey(x => x.PlayerId);
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PlayerName).HasColumnName("player_name");
                e.Property(x => x.PlayerNickname).HasColumnName("player_nickname");
                e.Property(x => x.JerseyNumber).HasColumnName("jersey_number");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.TeamName).HasColumnName("team_name");

                e.HasOne(x => x.Team)
                 .WithMany(t => t.Players)
                 .HasForeignKey(x => x.TeamId);
            });

            modelBuilder.Entity<Match>(e =>
            {
                e.ToTable("matches");
                e.HasKey(x => x.MatchId);
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.MatchDate).HasColumnName("match_date");
                e.Property(x => x.KickoffTime).HasColumnName("kickoff_time");
                e.Property(x => x.CompetitionStage).HasColumnName("competition_stage");
                e.Property(x => x.StadiumName).HasColumnName("stadium_name");
                e.Property(x => x.StadiumCountry).HasColumnName("stadium_country");
                e.Property(x => x.HomeTeamId).HasColumnName("home_team_id");
                e.Property(x => x.HomeTeamName).HasColumnName("home_team_name");
                e.Property(x => x.AwayTeamId).HasColumnName("away_team_id");
                e.Property(x => x.AwayTeamName).HasColumnName("away_team_name");
                e.Property(x => x.HomeScore).HasColumnName("home_score");
                e.Property(x => x.AwayScore).HasColumnName("away_score");
                e.Property(x => x.RefereeName).HasColumnName("referee_name");
                e.Property(x => x.RefereeCountry).HasColumnName("referee_country");

                e.HasOne(x => x.HomeTeam)
                 .WithMany(t => t.HomeMatches)
                 .HasForeignKey(x => x.HomeTeamId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.AwayTeam)
                 .WithMany(t => t.AwayMatches)
                 .HasForeignKey(x => x.AwayTeamId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MatchLineup>(e =>
            {
                e.ToTable("match_lineups");
                e.HasKey(x => new { x.MatchId, x.PlayerId });
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.JerseyNumber).HasColumnName("jersey_number");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.IsStartingXi).HasColumnName("is_starting_xi");

                e.HasOne(x => x.Match)
                 .WithMany(m => m.Lineups)
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Player)
                 .WithMany(p => p.Lineups)
                 .HasForeignKey(x => x.PlayerId);

                e.HasOne(x => x.Team)
                 .WithMany()
                 .HasForeignKey(x => x.TeamId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ShotEvent>(e =>
            {
                e.ToTable("shot_events");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.Second).HasColumnName("second");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.TeamName).HasColumnName("team_name");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PlayerName).HasColumnName("player_name");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.LocX).HasColumnName("loc_x");
                e.Property(x => x.LocY).HasColumnName("loc_y");
                e.Property(x => x.EndLocX).HasColumnName("end_loc_x");
                e.Property(x => x.EndLocY).HasColumnName("end_loc_y");
                e.Property(x => x.StatsbombXg).HasColumnName("statsbomb_xg");
                e.Property(x => x.OutcomeName).HasColumnName("outcome_name");
                e.Property(x => x.BodyPartName).HasColumnName("body_part_name");
                e.Property(x => x.ShotTypeName).HasColumnName("shot_type_name");
                e.Property(x => x.TechniqueName).HasColumnName("technique_name");
                e.Property(x => x.UnderPressure).HasColumnName("under_pressure");

                e.HasOne(x => x.Match)
                 .WithMany(m => m.Shots)
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Team)
                 .WithMany()
                 .HasForeignKey(x => x.TeamId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Player)
                 .WithMany(p => p.Shots)
                 .HasForeignKey(x => x.PlayerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PassEvent>(e =>
            {
                e.ToTable("pass_events");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.Second).HasColumnName("second");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.TeamName).HasColumnName("team_name");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PlayerName).HasColumnName("player_name");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.LocX).HasColumnName("loc_x");
                e.Property(x => x.LocY).HasColumnName("loc_y");
                e.Property(x => x.EndLocX).HasColumnName("end_loc_x");
                e.Property(x => x.EndLocY).HasColumnName("end_loc_y");
                e.Property(x => x.RecipientId).HasColumnName("recipient_id");
                e.Property(x => x.RecipientName).HasColumnName("recipient_name");
                e.Property(x => x.PassLength).HasColumnName("pass_length");
                e.Property(x => x.PassHeightName).HasColumnName("pass_height_name");
                e.Property(x => x.OutcomeName).HasColumnName("outcome_name");
                e.Property(x => x.BodyPartName).HasColumnName("body_part_name");
                e.Property(x => x.UnderPressure).HasColumnName("under_pressure");
                e.Property(x => x.IsShotAssist).HasColumnName("is_shot_assist");
                e.Property(x => x.IsGoalAssist).HasColumnName("is_goal_assist");

                e.HasOne(x => x.Match)
                 .WithMany(m => m.Passes)
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Team)
                 .WithMany()
                 .HasForeignKey(x => x.TeamId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Player)
                 .WithMany(p => p.Passes)
                 .HasForeignKey(x => x.PlayerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventLocation>(e =>
            {
                e.ToTable("event_locations");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.EventType).HasColumnName("event_type");
                e.Property(x => x.LocX).HasColumnName("loc_x");
                e.Property(x => x.LocY).HasColumnName("loc_y");

                e.HasOne(x => x.Match)
                 .WithMany()
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Player)
                 .WithMany()
                 .HasForeignKey(x => x.PlayerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- DuelEvent (new) ----------
            modelBuilder.Entity<DuelEvent>(e =>
            {
                e.ToTable("duel_events");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.DuelType).HasColumnName("duel_type");
                e.Property(x => x.OutcomeName).HasColumnName("outcome_name");
                e.Property(x => x.IsWon).HasColumnName("is_won");
                e.Property(x => x.LocX).HasColumnName("loc_x");
                e.Property(x => x.LocY).HasColumnName("loc_y");

                e.HasOne(x => x.Match)
                 .WithMany()
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Player)
                 .WithMany()
                 .HasForeignKey(x => x.PlayerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- DribbleEvent (new) ----------
            modelBuilder.Entity<DribbleEvent>(e =>
            {
                e.ToTable("dribble_events");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.PlayerId).HasColumnName("player_id");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.OutcomeName).HasColumnName("outcome_name");
                e.Property(x => x.IsComplete).HasColumnName("is_complete");
                e.Property(x => x.LocX).HasColumnName("loc_x");
                e.Property(x => x.LocY).HasColumnName("loc_y");

                e.HasOne(x => x.Match)
                 .WithMany()
                 .HasForeignKey(x => x.MatchId);

                e.HasOne(x => x.Player)
                 .WithMany()
                 .HasForeignKey(x => x.PlayerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
            // ---------- SubstitutionEvent (new) ----------
            modelBuilder.Entity<SubstitutionEvent>(e =>
            {
                e.ToTable("substitution_events");
                e.HasKey(x => x.EventId);
                e.Property(x => x.EventId).HasColumnName("event_id");
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.Period).HasColumnName("period");
                e.Property(x => x.Minute).HasColumnName("minute");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.PlayerOffId).HasColumnName("player_off_id");
                e.Property(x => x.PlayerOffName).HasColumnName("player_off_name");
                e.Property(x => x.PlayerOnId).HasColumnName("player_on_id");
                e.Property(x => x.PlayerOnName).HasColumnName("player_on_name");
                e.Property(x => x.PositionName).HasColumnName("position_name");
                e.Property(x => x.OutcomeName).HasColumnName("outcome_name");

                e.HasOne(x => x.Match)
                 .WithMany()
                 .HasForeignKey(x => x.MatchId);
            });

            // ---------- PossessionSequence (new, composite key) ----------
            modelBuilder.Entity<PossessionSequence>(e =>
            {
                e.ToTable("possession_sequences");
                e.HasKey(x => new { x.MatchId, x.PossessionSeq });
                e.Property(x => x.MatchId).HasColumnName("match_id");
                e.Property(x => x.PossessionSeq).HasColumnName("possession_seq");
                e.Property(x => x.TeamId).HasColumnName("team_id");
                e.Property(x => x.StartMinute).HasColumnName("start_minute");
                e.Property(x => x.StartSecond).HasColumnName("start_second");

                e.HasOne(x => x.Match)
                 .WithMany()
                 .HasForeignKey(x => x.MatchId);
            });
        }
    }
}