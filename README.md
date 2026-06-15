# Takt: a manufacturing-execution (MES) shop-floor monitor

What does a Manufacturing Execution System actually have to answer at the end of a shift? Which
machine lost the most time, and why. Whether quality held. Whether a work order is still moving or
has quietly stalled. And if a defect shows up later, which units and which work order it traces back
to. Takt is a small, working model of that, built end to end in **C# and SQL**.

It is the manufacturing version of a monitoring-and-support tool: model the shop floor in a database,
compute the metrics an MES reports, then turn those metrics into a support verdict an engineer can act
on.

## What it does
- **Models the shop floor in SQL**: work centers, work orders, a production-event log (RUN / DOWN with
  reason codes), and a defect table for genealogy (traceability by unit serial).
- **Computes the core MES metrics**, in SQL:
  - **OEE** per work center, decomposed into Availability, Performance, and Quality
    (OEE = A x P x Q, per Nakajima).
  - **Downtime Pareto**: minutes lost by reason code, largest first.
  - **First-pass yield** (the Quality term).
  - **Defect genealogy**: every defect on a work order, traced by unit serial.
- **Runs a support pass** that turns the metrics into a per-work-center verdict, **OK / WATCH / HOLD**,
  with the reason attached. This is the analogue of triaging and resolving MES issues: availability
  below target, a quality excursion, or a stalled work order each raise a flag.
- **Writes a shift report** to `out/findings.md`.

## The data
Shop-floor data is not public at the work-order level, so the project seeds **one reproducible
synthetic shift** with faults injected on purpose (the same honest approach as my Market Data Health
Monitor). The injected faults are:
- **CNC-01**: a 45-minute tooling stop, which pulls availability under target.
- **ASSY-02**: a quality excursion (a block of high scrap), which pulls first-pass yield under target.
- **TEST-03**: a fault followed by a stall, so the work order stops producing mid-shift.

The numbers are illustrative, not real plant data. What is real is the **method**: the schema, the SQL,
the metric definitions, and the triage logic.

## Run it
Requires the .NET SDK (built and tested on .NET 10).
```bash
dotnet run
```
You will see the OEE table and the support verdicts in the console, and a full `out/findings.md` report.

## MES concepts and references
- **OEE** and its A x P x Q decomposition: Seiichi Nakajima, *Introduction to TPM* (the origin of the
  85% world-class benchmark used here).
- **Equipment states** (run, down, idle) behind availability: SEMI E10 / E79, the standard equipment
  state and OEE definitions used across manufacturing.
- **MES scope** (work orders, tracking, genealogy, performance analysis): ISA-95 / MESA, the reference
  model for what a Manufacturing Execution System owns.

## Limitations
The shift is synthetic, so the figures are illustrative. This models the *core* of an MES (tracking,
performance, genealogy, and a support-triage layer); it is not a full platform (no scheduling, no
labor or inventory modules, no live equipment integration). The point is to show I understand what an
MES does and can build and query it in C# and SQL.
