import json, csv, os

EVENTS_DIR = "events"
LINEUPS_DIR = "lineups"
MATCHES_FILE = "matches/106.json"
OUT_DIR = "csv_out"
os.makedirs(OUT_DIR, exist_ok=True)

matches = json.load(open(MATCHES_FILE))
match_ids = [m["match_id"] for m in matches]

# ---------- matches.csv ----------
with open(f"{OUT_DIR}/matches.csv", "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["match_id","match_date","kickoff_time","competition_stage","stadium_name","stadium_country",
                "home_team_id","home_team_name","away_team_id","away_team_name","home_score","away_score",
                "referee_name","referee_country"])
    for m in matches:
        w.writerow([
            m["match_id"], m["match_date"], m.get("kick_off"),
            m.get("competition_stage",{}).get("name"),
            m.get("stadium",{}).get("name"), m.get("stadium",{}).get("country",{}).get("name"),
            m["home_team"]["home_team_id"], m["home_team"]["home_team_name"],
            m["away_team"]["away_team_id"], m["away_team"]["away_team_name"],
            m.get("home_score"), m.get("away_score"),
            m.get("referee",{}).get("name") if m.get("referee") else None,
            m.get("referee",{}).get("country",{}).get("name") if m.get("referee") else None,
        ])

# ---------- teams.csv (deduped from matches) ----------
teams = {}
for m in matches:
    home_id = m["home_team"]["home_team_id"]
    home_name = m["home_team"]["home_team_name"]
    home_grp = m["home_team"].get("home_team_group")
    away_id = m["away_team"]["away_team_id"]
    away_name = m["away_team"]["away_team_name"]
    away_grp = m["away_team"].get("away_team_group")

    # Only overwrite the stored group if we don't have one yet AND the new
    # value is actually present — knockout-stage matches carry group=None,
    # so blindly overwriting on every match wipes out the real group letter.
    if home_id not in teams:
        teams[home_id] = [home_name, home_grp]
    elif teams[home_id][1] is None and home_grp is not None:
        teams[home_id][1] = home_grp

    if away_id not in teams:
        teams[away_id] = [away_name, away_grp]
    elif teams[away_id][1] is None and away_grp is not None:
        teams[away_id][1] = away_grp

# One real gap in StatsBomb's source data: Morocco's group is never
# populated (None in every single one of their matches, including group
# stage). Their group-stage opponents (Canada, Croatia, Belgium) all show
# "F", so this is inferred, not guessed — confirmed against the real 2022
# draw (Group F: Belgium, Canada, Morocco, Croatia).
for tid, info in teams.items():
    if info[0] == "Morocco" and info[1] is None:
        info[1] = "F"
with open(f"{OUT_DIR}/teams.csv", "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["team_id","team_name","group_letter"])
    for tid, (name, grp) in sorted(teams.items()):
        w.writerow([tid, name, grp])

# ---------- players.csv (from lineups) ----------
players = {}
lineup_rows = []
for mid in match_ids:
    path = f"{LINEUPS_DIR}/{mid}.json"
    if not os.path.exists(path):
        continue
    data = json.load(open(path))
    for team in data:
        team_id = team["team_id"]
        for p in team["lineup"]:
            players[p["player_id"]] = (p["player_name"], p.get("player_nickname"), p.get("jersey_number"), team_id, team["team_name"])
            positions = p.get("positions", [])
            starter = any(pos.get("start_reason") == "Starting XI" for pos in positions) if positions else False
            position_name = positions[0]["position"] if positions else None
            lineup_rows.append([mid, p["player_id"], team_id, jersey := p.get("jersey_number"), position_name, int(starter)])

with open(f"{OUT_DIR}/players.csv", "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["player_id","player_name","player_nickname","jersey_number","team_id","team_name"])
    for pid, (name, nick, jersey, team_id, team_name) in sorted(players.items()):
        w.writerow([pid, name, nick, jersey, team_id, team_name])

with open(f"{OUT_DIR}/match_lineups.csv", "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["match_id","player_id","team_id","jersey_number","position_name","is_starting_xi"])
    for row in lineup_rows:
        w.writerow(row)

# ---------- shot_events.csv ----------
shot_f = open(f"{OUT_DIR}/shot_events.csv", "w", newline="", encoding="utf-8")
shot_w = csv.writer(shot_f)
shot_w.writerow(["event_id","match_id","period","minute","second","team_id","team_name","player_id","player_name",
                  "position_name","loc_x","loc_y","end_loc_x","end_loc_y","statsbomb_xg","outcome_name",
                  "body_part_name","shot_type_name","technique_name","under_pressure"])

# ---------- pass_events.csv ----------
pass_f = open(f"{OUT_DIR}/pass_events.csv", "w", newline="", encoding="utf-8")
pass_w = csv.writer(pass_f)
pass_w.writerow(["event_id","match_id","period","minute","second","team_id","team_name","player_id","player_name",
                  "position_name","loc_x","loc_y","end_loc_x","end_loc_y","recipient_id","recipient_name",
                  "pass_length","pass_height_name","outcome_name","body_part_name","under_pressure",
                  "is_shot_assist","is_goal_assist"])

# ---------- event_locations.csv (every located event, for heat maps) ----------
loc_f = open(f"{OUT_DIR}/event_locations.csv", "w", newline="", encoding="utf-8")
loc_w = csv.writer(loc_f)
loc_w.writerow(["event_id","match_id","period","minute","team_id","player_id","position_name","event_type","loc_x","loc_y"])

for mid in match_ids:
    path = f"{EVENTS_DIR}/{mid}.json"
    if not os.path.exists(path):
        continue
    events = json.load(open(path))
    for e in events:
        loc = e.get("location")
        player = e.get("player")
        team = e.get("team")
        etype = e["type"]["name"]

        if loc:
            loc_w.writerow([e["id"], mid, e.get("period"), e.get("minute"),
                             team["id"] if team else None,
                             player["id"] if player else None,
                             e.get("position",{}).get("name") if e.get("position") else None,
                             etype, loc[0], loc[1]])

        if etype == "Shot":
            s = e["shot"]
            end = s.get("end_location", [None, None, None])
            shot_w.writerow([
                e["id"], mid, e.get("period"), e.get("minute"), e.get("second"),
                team["id"] if team else None, team["name"] if team else None,
                player["id"] if player else None, player["name"] if player else None,
                e.get("position",{}).get("name") if e.get("position") else None,
                loc[0] if loc else None, loc[1] if loc else None,
                end[0] if len(end) > 0 else None, end[1] if len(end) > 1 else None,
                s.get("statsbomb_xg"),
                s.get("outcome",{}).get("name"),
                s.get("body_part",{}).get("name"),
                s.get("type",{}).get("name"),
                s.get("technique",{}).get("name"),
                int(bool(e.get("under_pressure"))),
            ])

        if etype == "Pass":
            p = e["pass"]
            end = p.get("end_location", [None, None])
            recipient = p.get("recipient")
            pass_w.writerow([
                e["id"], mid, e.get("period"), e.get("minute"), e.get("second"),
                team["id"] if team else None, team["name"] if team else None,
                player["id"] if player else None, player["name"] if player else None,
                e.get("position",{}).get("name") if e.get("position") else None,
                loc[0] if loc else None, loc[1] if loc else None,
                end[0] if len(end) > 0 else None, end[1] if len(end) > 1 else None,
                recipient["id"] if recipient else None,
                recipient["name"] if recipient else None,
                p.get("length"),
                p.get("height",{}).get("name"),
                p.get("outcome",{}).get("name") if p.get("outcome") else "Complete",
                p.get("body_part",{}).get("name") if p.get("body_part") else None,
                int(bool(e.get("under_pressure"))),
                int(bool(p.get("shot_assist")) or bool(p.get("goal_assist"))),
                int(bool(p.get("goal_assist"))),
            ])

shot_f.close()
pass_f.close()
loc_f.close()

print("Done. Row counts:")
for fn in ["matches.csv","teams.csv","players.csv","match_lineups.csv","shot_events.csv","pass_events.csv","event_locations.csv"]:
    with open(f"{OUT_DIR}/{fn}") as f:
        print(fn, sum(1 for _ in f) - 1)

# ---------- duel_events.csv and dribble_events.csv (added later) ----------
duel_f = open(f"{OUT_DIR}/duel_events.csv", "w", newline="", encoding="utf-8")
duel_w = csv.writer(duel_f)
duel_w.writerow(["event_id","match_id","period","minute","team_id","player_id","position_name","duel_type","outcome_name","is_won","loc_x","loc_y"])

dribble_f = open(f"{OUT_DIR}/dribble_events.csv", "w", newline="", encoding="utf-8")
dribble_w = csv.writer(dribble_f)
dribble_w.writerow(["event_id","match_id","period","minute","team_id","player_id","position_name","outcome_name","is_complete","loc_x","loc_y"])

# Duel outcomes vary in wording — collapse to a simple won/lost flag.
WON_OUTCOMES = {"Won", "Success In Play", "Success Out"}

for mid in match_ids:
    path = f"{EVENTS_DIR}/{mid}.json"
    if not os.path.exists(path):
        continue
    events = json.load(open(path))
    for e in events:
        loc = e.get("location")
        player = e.get("player")
        team = e.get("team")
        etype = e["type"]["name"]

        if etype == "Duel":
            d = e.get("duel", {})
            duel_type = d.get("type", {}).get("name") if d.get("type") else None
            outcome = d.get("outcome", {}).get("name") if d.get("outcome") else None

            # "Aerial Lost" duels never carry a separate outcome field —
            # the type itself already means the duel was lost. There is no
            # corresponding "Aerial Won" tag in StatsBomb's model; a won
            # aerial duel typically isn't logged as a Duel event at all for
            # the winning player (it shows up as a Clearance/Pass instead).
            # So our aerial coverage here is losses-only, by nature of the
            # source data — flagged clearly in the UI.
            if duel_type == "Aerial Lost":
                is_won = 0
            elif outcome:
                is_won = int(outcome in WON_OUTCOMES)
            else:
                is_won = ""

            duel_w.writerow([
                e["id"], mid, e.get("period"), e.get("minute"),
                team["id"] if team else None,
                player["id"] if player else None,
                e.get("position", {}).get("name") if e.get("position") else None,
                duel_type,
                outcome,
                is_won,
                loc[0] if loc else None, loc[1] if loc else None,
            ])

        if etype == "Dribble":
            d = e.get("dribble", {})
            outcome = d.get("outcome", {}).get("name") if d.get("outcome") else None
            dribble_w.writerow([
                e["id"], mid, e.get("period"), e.get("minute"),
                team["id"] if team else None,
                player["id"] if player else None,
                e.get("position", {}).get("name") if e.get("position") else None,
                outcome,
                int(outcome == "Complete") if outcome else "",
                loc[0] if loc else None, loc[1] if loc else None,
            ])

duel_f.close()
dribble_f.close()
print("duel_events.csv:", sum(1 for _ in open(f"{OUT_DIR}/duel_events.csv")) - 1)
print("dribble_events.csv:", sum(1 for _ in open(f"{OUT_DIR}/dribble_events.csv")) - 1)

# ---------- substitution_events.csv (added later) ----------
sub_f = open(f"{OUT_DIR}/substitution_events.csv", "w", newline="", encoding="utf-8")
sub_w = csv.writer(sub_f)
sub_w.writerow(["event_id","match_id","period","minute","team_id","player_off_id","player_off_name","player_on_id","player_on_name","position_name","outcome_name"])

for mid in match_ids:
    path = f"{EVENTS_DIR}/{mid}.json"
    if not os.path.exists(path):
        continue
    events = json.load(open(path))
    for e in events:
        if e["type"]["name"] != "Substitution":
            continue
        team = e.get("team")
        player_off = e.get("player")
        sub = e.get("substitution", {})
        replacement = sub.get("replacement")
        outcome = sub.get("outcome", {}).get("name") if sub.get("outcome") else None

        sub_w.writerow([
            e["id"], mid, e.get("period"), e.get("minute"),
            team["id"] if team else None,
            player_off["id"] if player_off else None,
            player_off["name"] if player_off else None,
            replacement["id"] if replacement else None,
            replacement["name"] if replacement else None,
            e.get("position", {}).get("name") if e.get("position") else None,
            outcome
        ])

sub_f.close()
print("substitution_events.csv:", sum(1 for _ in open(f"{OUT_DIR}/substitution_events.csv")) - 1)

# ---------- possession_sequences.csv (added later) ----------
# One row per unique possession sequence per match (not per event) — much
# smaller than a full event dump, since consecutive events in the same
# sequence share the same possession number and possession_team.
poss_f = open(f"{OUT_DIR}/possession_sequences.csv", "w", newline="", encoding="utf-8")
poss_w = csv.writer(poss_f)
poss_w.writerow(["match_id","possession_seq","team_id","start_minute","start_second"])

for mid in match_ids:
    path = f"{EVENTS_DIR}/{mid}.json"
    if not os.path.exists(path):
        continue
    events = json.load(open(path))
    last_seq = None
    for e in events:
        seq = e.get("possession")
        poss_team = e.get("possession_team")
        if seq is None or poss_team is None:
            continue
        if seq == last_seq:
            continue  # already recorded this sequence's start
        last_seq = seq
        poss_w.writerow([mid, seq, poss_team["id"], e.get("minute"), e.get("second")])

poss_f.close()
print("possession_sequences.csv:", sum(1 for _ in open(f"{OUT_DIR}/possession_sequences.csv")) - 1)
