-- ============================================================
-- Matchpoint — FIFA World Cup 2022 Analytics Dashboard
-- Complete MySQL Schema
-- Source: StatsBomb Open Data (github.com/statsbomb/open-data)
-- Real event-level data: shot/pass coordinates, xG, duels, dribbles,
-- substitutions, and possession sequences.
--
-- Run this whole script in MySQL Workbench BEFORE importing any CSVs.
-- Import order: teams -> players -> matches -> match_lineups ->
--   shot_events -> pass_events -> event_locations -> duel_events ->
--   dribble_events -> substitution_events -> possession_sequences
-- ============================================================

CREATE DATABASE IF NOT EXISTS world_cup_2022
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE world_cup_2022;

-- ---------- Teams ----------
CREATE TABLE teams (
    team_id      INT PRIMARY KEY,
    team_name    VARCHAR(100) NOT NULL,
    group_letter CHAR(1)
);

-- ---------- Players ----------
CREATE TABLE players (
    player_id       INT PRIMARY KEY,
    player_name     VARCHAR(100) NOT NULL,
    player_nickname VARCHAR(100),
    jersey_number   INT,
    team_id         INT NOT NULL,
    team_name       VARCHAR(100),
    CONSTRAINT fk_players_team FOREIGN KEY (team_id) REFERENCES teams(team_id)
);

-- ---------- Matches ----------
CREATE TABLE matches (
    match_id             BIGINT PRIMARY KEY,
    match_date           DATE NOT NULL,
    kickoff_time         TIME,
    competition_stage    VARCHAR(50),
    stadium_name         VARCHAR(150),
    stadium_country      VARCHAR(50),
    home_team_id         INT NOT NULL,
    home_team_name       VARCHAR(100),
    away_team_id         INT NOT NULL,
    away_team_name       VARCHAR(100),
    home_score           INT,
    away_score           INT,
    referee_name         VARCHAR(100),
    referee_country      VARCHAR(50),
    CONSTRAINT fk_matches_home FOREIGN KEY (home_team_id) REFERENCES teams(team_id),
    CONSTRAINT fk_matches_away FOREIGN KEY (away_team_id) REFERENCES teams(team_id)
);

-- ---------- Lineups (who played, starter or sub, position) ----------
CREATE TABLE match_lineups (
    match_id        BIGINT NOT NULL,
    player_id       INT NOT NULL,
    team_id         INT NOT NULL,
    jersey_number   INT,
    position_name   VARCHAR(50),
    is_starting_xi  TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (match_id, player_id),
    CONSTRAINT fk_lineups_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_lineups_player FOREIGN KEY (player_id) REFERENCES players(player_id),
    CONSTRAINT fk_lineups_team   FOREIGN KEY (team_id) REFERENCES teams(team_id)
);

-- ---------- Shots (real x/y coordinates + xG, for shot maps) ----------
-- Pitch coordinates: 120 (length) x 80 (width), StatsBomb standard.
CREATE TABLE shot_events (
    event_id        VARCHAR(36) PRIMARY KEY,
    match_id        BIGINT NOT NULL,
    period          TINYINT,
    minute          INT,
    second          INT,
    team_id         INT NOT NULL,
    team_name       VARCHAR(100),
    player_id       INT,
    player_name     VARCHAR(100),
    position_name   VARCHAR(50),
    loc_x           DECIMAL(5,2),
    loc_y           DECIMAL(5,2),
    end_loc_x       DECIMAL(5,2),
    end_loc_y       DECIMAL(5,2),
    statsbomb_xg    DECIMAL(8,6),
    outcome_name    VARCHAR(30),
    body_part_name  VARCHAR(30),
    shot_type_name  VARCHAR(30),
    technique_name  VARCHAR(30),
    under_pressure  TINYINT(1) DEFAULT 0,
    CONSTRAINT fk_shots_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_shots_team   FOREIGN KEY (team_id) REFERENCES teams(team_id),
    CONSTRAINT fk_shots_player FOREIGN KEY (player_id) REFERENCES players(player_id)
);

-- ---------- Passes (real x/y coordinates, for pass maps + accuracy) ----------
CREATE TABLE pass_events (
    event_id         VARCHAR(36) PRIMARY KEY,
    match_id         BIGINT NOT NULL,
    period           TINYINT,
    minute           INT,
    second           INT,
    team_id          INT NOT NULL,
    team_name        VARCHAR(100),
    player_id        INT,
    player_name      VARCHAR(100),
    position_name    VARCHAR(50),
    loc_x            DECIMAL(5,2),
    loc_y            DECIMAL(5,2),
    end_loc_x        DECIMAL(5,2),
    end_loc_y        DECIMAL(5,2),
    recipient_id     INT,
    recipient_name   VARCHAR(100),
    pass_length      DECIMAL(6,2),
    pass_height_name VARCHAR(30),
    outcome_name     VARCHAR(30) DEFAULT 'Complete',
    body_part_name   VARCHAR(30),
    under_pressure   TINYINT(1) DEFAULT 0,
    is_shot_assist   TINYINT(1) DEFAULT 0,
    is_goal_assist   TINYINT(1) DEFAULT 0,
    CONSTRAINT fk_passes_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_passes_team   FOREIGN KEY (team_id) REFERENCES teams(team_id),
    CONSTRAINT fk_passes_player FOREIGN KEY (player_id) REFERENCES players(player_id)
);

-- ---------- Every located event (touches, carries, pressures, duels...) ----------
-- This is the raw material for heat maps and average-position maps: one
-- row per on-ball/positional event, of any type.
CREATE TABLE event_locations (
    event_id      VARCHAR(36) PRIMARY KEY,
    match_id      BIGINT NOT NULL,
    period        TINYINT,
    minute        INT,
    team_id       INT,
    player_id     INT,
    position_name VARCHAR(50),
    event_type    VARCHAR(30),
    loc_x         DECIMAL(5,2),
    loc_y         DECIMAL(5,2),
    CONSTRAINT fk_locs_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_locs_player FOREIGN KEY (player_id) REFERENCES players(player_id)
);

-- ---------- Duels (tackles + aerials) ----------
-- "Aerial Lost" duels never carry a separate outcome — the type itself
-- means the duel was lost. There is no corresponding "Aerial Won" tag in
-- StatsBomb's model (a won aerial typically shows up as a different event
-- for the winning player instead), so aerial win-rate isn't computable
-- from this table — only losses. Ground duels ("Tackle") have a real
-- won/lost outcome.
CREATE TABLE duel_events (
    event_id      VARCHAR(36) PRIMARY KEY,
    match_id      BIGINT NOT NULL,
    period        TINYINT,
    minute        INT,
    team_id       INT,
    player_id     INT,
    position_name VARCHAR(50),
    duel_type     VARCHAR(30),      -- "Tackle" or "Aerial Lost"
    outcome_name  VARCHAR(30),      -- raw StatsBomb outcome, blank for Aerial Lost
    is_won        TINYINT(1),       -- 1 = won, 0 = lost
    loc_x         DECIMAL(5,2),
    loc_y         DECIMAL(5,2),
    CONSTRAINT fk_duels_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_duels_player FOREIGN KEY (player_id) REFERENCES players(player_id)
);

-- ---------- Dribbles ----------
CREATE TABLE dribble_events (
    event_id      VARCHAR(36) PRIMARY KEY,
    match_id      BIGINT NOT NULL,
    period        TINYINT,
    minute        INT,
    team_id       INT,
    player_id     INT,
    position_name VARCHAR(50),
    outcome_name  VARCHAR(30),      -- "Complete" or "Incomplete"
    is_complete   TINYINT(1),
    loc_x         DECIMAL(5,2),
    loc_y         DECIMAL(5,2),
    CONSTRAINT fk_dribbles_match  FOREIGN KEY (match_id) REFERENCES matches(match_id),
    CONSTRAINT fk_dribbles_player FOREIGN KEY (player_id) REFERENCES players(player_id)
);

-- ---------- Substitutions ----------
CREATE TABLE substitution_events (
    event_id        VARCHAR(36) PRIMARY KEY,
    match_id        BIGINT NOT NULL,
    period          TINYINT,
    minute          INT,
    team_id         INT,
    player_off_id   INT,
    player_off_name VARCHAR(100),
    player_on_id    INT,
    player_on_name  VARCHAR(100),
    position_name   VARCHAR(50),
    outcome_name    VARCHAR(30),    -- "Tactical" or "Injury"
    CONSTRAINT fk_subs_match FOREIGN KEY (match_id) REFERENCES matches(match_id)
);

-- ---------- Possession sequences ----------
-- One row per unique possession sequence per match (not per event) — the
-- team that had the ball, and when that sequence started. Sequence
-- duration = time until the NEXT sequence starts; this is what powers the
-- real time-based possession % (not a pass-count proxy).
CREATE TABLE possession_sequences (
    match_id        BIGINT NOT NULL,
    possession_seq  INT NOT NULL,
    team_id         INT NOT NULL,
    start_minute    INT NOT NULL,
    start_second    INT NOT NULL,
    PRIMARY KEY (match_id, possession_seq),
    CONSTRAINT fk_poss_match FOREIGN KEY (match_id) REFERENCES matches(match_id)
);

-- ---------- Indexes ----------
CREATE INDEX idx_players_team      ON players(team_id);
CREATE INDEX idx_shots_player      ON shot_events(player_id);
CREATE INDEX idx_shots_match       ON shot_events(match_id);
CREATE INDEX idx_passes_player     ON pass_events(player_id);
CREATE INDEX idx_passes_match      ON pass_events(match_id);
CREATE INDEX idx_locs_player_match ON event_locations(player_id, match_id);
CREATE INDEX idx_duels_player      ON duel_events(player_id);
CREATE INDEX idx_dribbles_player   ON dribble_events(player_id);
CREATE INDEX idx_subs_match        ON substitution_events(match_id);
CREATE INDEX idx_poss_match_time   ON possession_sequences(match_id, start_minute, start_second);
