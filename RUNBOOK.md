# Takt support runbook

A short triage guide for the verdicts Takt raises. This mirrors the kind of documentation an MES
support engineer keeps for end users: what the alert means, what to check, and when to escalate.

## Verdict: HOLD, "work order STALLED"
**Meaning:** the work order is flagged STALLED, production events stopped before the shift ended.
1. Check the last `production_events` row for that work center: what was the final event and reason?
   A `FAULT` down event with nothing after it usually means equipment, not operator, caused the stop.
2. Confirm whether the operator logged off (planned) or the line tripped (unplanned).
3. If unplanned and equipment-related, escalate to maintenance with the reason code and timestamp.
4. Re-run Takt after the work order resumes; the stall clears once new RUN events land.

## Verdict: HOLD, "first-pass yield below target"
**Meaning:** scrap on the work order pushed quality under the target (default 95%).
1. Pull the **defect genealogy** for the work order (the report's genealogy table) and look for a
   cluster, the same defect code repeating points at a single root cause.
2. Check whether the bad units cluster in one time window; if so, correlate with a changeover, a
   material lot, or an operator change around that time.
3. Notify quality engineering with the defect code and the affected unit serials so containment can
   start before the lot moves downstream.

## Verdict: WATCH, "availability below target"
**Meaning:** downtime ate into planned production (availability under the default 90%).
1. Read the **downtime Pareto**: the top reason code is where the time went.
2. `TOOLING` or `FAULT` recurring across shifts is a maintenance pattern, not a one-off; flag it.
3. `CHANGEOVER` time that is high is a scheduling or setup-reduction opportunity, not a fault.
4. Availability alone is a WATCH, not a HOLD, unless paired with a stall or a quality miss.

## Verdict: WATCH, "OEE below world-class"
**Meaning:** the combined OEE is under the 85% benchmark even if no single term failed.
1. Look at which of the three terms (Availability, Performance, Quality) is dragging.
2. A low **Performance** term with healthy availability and quality usually means the line is running
   slower than its ideal cycle time, check for micro-stops or a reduced-rate setting.

## Verdict: OK
All terms are within target. No action; the report still logs the shift for trend tracking.

## Escalation summary
- Equipment fault or recurring tooling downtime -> Maintenance.
- Quality excursion with a defect cluster -> Quality engineering, with serials.
- Stalled work order -> Production supervisor, then maintenance if equipment-caused.
