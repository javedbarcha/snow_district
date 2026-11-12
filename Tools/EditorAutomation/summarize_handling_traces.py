"""Read existing Play traces; triage thresholds are review flags, not physics acceptance."""
import csv
import json
from pathlib import Path
import sys

folder = Path(sys.argv[1] if len(sys.argv) > 1 else "Logs/CollisionAudit")
rows = list(csv.DictReader((folder / "Results.csv").open()))
reports = []
for row in rows:
    trace = list(csv.DictReader((folder / f"Trace-{int(row['trial']):02}.csv").open()))
    peak_dv = peak_step = peak_accel = 0.0
    event = None
    for prev, now in zip(trace, trace[1:]):
        dt = float(now["time"]) - float(prev["time"])
        if dt <= 0:
            continue
        dv = float(now["up"]) - float(prev["up"])
        dy = float(now["y"]) - float(prev["y"])
        if dv > peak_dv:
            peak_dv = dv
            event = {k: now[k] for k in ("time", "speed", "y", "tilt", "up", "grounded")}
        peak_step = max(peak_step, dy)
        peak_accel = max(peak_accel, dv / dt)
    intended_context = "ramp" in row["case"] or "fast-turn" in row["case"]
    flags = []
    if peak_dv > 1.0:
        flags.append("upward velocity gained >1 m/s in one recorded step")
    if float(row["longestAirborne"]) > .2:
        flags.append("continuous airborne >0.2 s")
    if float(row["maxTilt"]) > 60:
        flags.append("tilt >60 degrees")
    reports.append({"trial": int(row["trial"]), "vehicle": row["vehicle"], "case": row["case"],
                    "targetSpeed": float(row["targetSpeed"]), "maxTilt": float(row["maxTilt"]),
                    "maxRise": float(row["maxRise"]), "maxUp": float(row["maxUp"]),
                    "longestAirborne": float(row["longestAirborne"]),
                    "snowWheelSamples": int(row["snowWheelSamples"]),
                    "targetContacts": int(row["targetContacts"]),
                    "peakUpwardVelocityStep": peak_dv, "peakUpwardPositionStep": peak_step,
                    "peakUpwardAcceleration": peak_accel, "peakVelocityEvent": event,
                    "rampOrFastTurnContext": intended_context, "reviewFlags": flags})
output = {"cases": len(reports), "notes": "Flags require contact/trace review. Ramp falls and fast-turn tips are not automatically defects. Target speed and initial pile size are not guaranteed encounter conditions.",
          "flagged": [r for r in reports if r["reviewFlags"]], "trials": reports}
(folder / "JerkReview.json").write_text(json.dumps(output, indent=2))
print(json.dumps({"cases": len(reports), "flagged": output["flagged"]}, indent=2))
