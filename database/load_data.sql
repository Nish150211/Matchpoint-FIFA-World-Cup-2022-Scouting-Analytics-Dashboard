-- ============================================================
-- Matchpoint — FIFA World Cup 2022 Analytics Dashboard
-- Complete data load script — all 11 tables, correct order.
--
-- BEFORE RUNNING:
-- 1. Run schema.sql first.
-- 2. Enable local file loading:
--      SET GLOBAL local_infile = 1;
--    ...and in Workbench: Edit > Preferences > SQL Editor >
--    check "Allow loading of local files", then restart Workbench.
--    Also add OPT_LOCAL_INFILE=1 under your connection's
--    Database > Manage Connections > Advanced > Others box.
-- 3. Replace every 'C:/path/to/...' below with the actual folder
--    where you've placed the CSVs from /data/csv.
--
-- Two files use ASCII-transliterated player names (accents stripped,
-- e.g. "Mbappé" -> "Mbappe") to avoid a MySQL Workbench encoding bug —
-- use the _ascii versions for those four specific files as noted.
-- ============================================================

USE world_cup_2022;
SET FOREIGN_KEY_CHECKS = 0;

-- ---------- 1. teams ----------
LOAD DATA LOCAL INFILE 'C:/path/to/teams.csv'
INTO TABLE teams
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(team_id, team_name, @group_letter)
SET group_letter = NULLIF(@group_letter, '');

-- ---------- 2. players (ASCII-safe names) ----------
LOAD DATA LOCAL INFILE 'C:/path/to/players_ascii.csv'
INTO TABLE players
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(player_id, player_name, @nickname, @jersey, team_id, team_name)
SET player_nickname = NULLIF(@nickname, ''),
    jersey_number = NULLIF(@jersey, '');

-- ---------- 3. matches (ASCII-safe names) ----------
LOAD DATA LOCAL INFILE 'C:/path/to/matches_ascii.csv'
INTO TABLE matches
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(match_id, match_date, @kickoff, competition_stage, stadium_name, stadium_country,
 home_team_id, home_team_name, away_team_id, away_team_name,
 @home_score, @away_score, @ref_name, @ref_country)
SET kickoff_time    = NULLIF(@kickoff, ''),
    home_score      = NULLIF(@home_score, ''),
    away_score      = NULLIF(@away_score, ''),
    referee_name    = NULLIF(@ref_name, ''),
    referee_country = NULLIF(@ref_country, '');

-- ---------- 4. match_lineups ----------
LOAD DATA LOCAL INFILE 'C:/path/to/match_lineups.csv'
INTO TABLE match_lineups
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(match_id, player_id, team_id, @jersey, @position, is_starting_xi)
SET jersey_number = NULLIF(@jersey, ''),
    position_name = NULLIF(@position, '');

-- ---------- 5. shot_events (ASCII-safe names) ----------
LOAD DATA LOCAL INFILE 'C:/path/to/shot_events_ascii.csv'
INTO TABLE shot_events
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, second, team_id, team_name, @player_id, @player_name,
 @position, loc_x, loc_y, @end_x, @end_y, @xg, @outcome, @body_part, @shot_type, @technique, under_pressure)
SET player_id      = NULLIF(@player_id, ''),
    player_name    = NULLIF(@player_name, ''),
    position_name  = NULLIF(@position, ''),
    end_loc_x      = NULLIF(@end_x, ''),
    end_loc_y      = NULLIF(@end_y, ''),
    statsbomb_xg   = NULLIF(@xg, ''),
    outcome_name   = NULLIF(@outcome, ''),
    body_part_name = NULLIF(@body_part, ''),
    shot_type_name = NULLIF(@shot_type, ''),
    technique_name = NULLIF(@technique, '');

-- ---------- 6. pass_events (ASCII-safe names) ----------
LOAD DATA LOCAL INFILE 'C:/path/to/pass_events_ascii.csv'
INTO TABLE pass_events
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, second, team_id, team_name, @player_id, @player_name,
 @position, loc_x, loc_y, @end_x, @end_y, @recipient_id, @recipient_name, @length, @height,
 @outcome, @body_part, under_pressure, is_shot_assist, is_goal_assist)
SET player_id        = NULLIF(@player_id, ''),
    player_name      = NULLIF(@player_name, ''),
    position_name    = NULLIF(@position, ''),
    end_loc_x        = NULLIF(@end_x, ''),
    end_loc_y        = NULLIF(@end_y, ''),
    recipient_id     = NULLIF(@recipient_id, ''),
    recipient_name   = NULLIF(@recipient_name, ''),
    pass_length      = NULLIF(@length, ''),
    pass_height_name = NULLIF(@height, ''),
    outcome_name     = NULLIF(@outcome, ''),
    body_part_name   = NULLIF(@body_part, '');

-- ---------- 7. event_locations ----------
LOAD DATA LOCAL INFILE 'C:/path/to/event_locations.csv'
INTO TABLE event_locations
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, @team_id, @player_id, @position, event_type, loc_x, loc_y)
SET team_id       = NULLIF(@team_id, ''),
    player_id     = NULLIF(@player_id, ''),
    position_name = NULLIF(@position, '');

-- ---------- 8. duel_events ----------
LOAD DATA LOCAL INFILE 'C:/path/to/duel_events.csv'
INTO TABLE duel_events
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, @team_id, @player_id, @position, @duel_type, @outcome, @is_won, @loc_x, @loc_y)
SET team_id       = NULLIF(@team_id, ''),
    player_id     = NULLIF(@player_id, ''),
    position_name = NULLIF(@position, ''),
    duel_type     = NULLIF(@duel_type, ''),
    outcome_name  = NULLIF(@outcome, ''),
    is_won        = NULLIF(@is_won, ''),
    loc_x         = NULLIF(@loc_x, ''),
    loc_y         = NULLIF(@loc_y, '');

-- ---------- 9. dribble_events ----------
LOAD DATA LOCAL INFILE 'C:/path/to/dribble_events.csv'
INTO TABLE dribble_events
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, @team_id, @player_id, @position, @outcome, @is_complete, @loc_x, @loc_y)
SET team_id       = NULLIF(@team_id, ''),
    player_id     = NULLIF(@player_id, ''),
    position_name = NULLIF(@position, ''),
    outcome_name  = NULLIF(@outcome, ''),
    is_complete   = NULLIF(@is_complete, ''),
    loc_x         = NULLIF(@loc_x, ''),
    loc_y         = NULLIF(@loc_y, '');

-- ---------- 10. substitution_events (ASCII-safe names) ----------
LOAD DATA LOCAL INFILE 'C:/path/to/substitution_events_ascii.csv'
INTO TABLE substitution_events
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(event_id, match_id, period, minute, @team_id, @off_id, @off_name, @on_id, @on_name, @position, @outcome)
SET team_id         = NULLIF(@team_id, ''),
    player_off_id   = NULLIF(@off_id, ''),
    player_off_name = NULLIF(@off_name, ''),
    player_on_id    = NULLIF(@on_id, ''),
    player_on_name  = NULLIF(@on_name, ''),
    position_name   = NULLIF(@position, ''),
    outcome_name    = NULLIF(@outcome, '');

-- ---------- 11. possession_sequences ----------
LOAD DATA LOCAL INFILE 'C:/path/to/possession_sequences.csv'
INTO TABLE possession_sequences
FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\r\n'
IGNORE 1 ROWS
(match_id, possession_seq, team_id, start_minute, start_second);

SET FOREIGN_KEY_CHECKS = 1;

-- ---------- Verify ----------
SELECT
  (SELECT COUNT(*) FROM teams)                AS teams_32,
  (SELECT COUNT(*) FROM players)              AS players_829,
  (SELECT COUNT(*) FROM matches)              AS matches_64,
  (SELECT COUNT(*) FROM match_lineups)        AS lineups_3244,
  (SELECT COUNT(*) FROM shot_events)          AS shots_1494,
  (SELECT COUNT(*) FROM pass_events)          AS passes_68515,
  (SELECT COUNT(*) FROM event_locations)      AS locations_232512,
  (SELECT COUNT(*) FROM duel_events)          AS duels_4389,
  (SELECT COUNT(*) FROM dribble_events)       AS dribbles_1793,
  (SELECT COUNT(*) FROM substitution_events)  AS subs_587,
  (SELECT COUNT(*) FROM possession_sequences) AS possession_11125;
