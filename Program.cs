using Microsoft.Data.Sqlite;
using System.Text;
using Takt;

// Takt: a lightweight manufacturing-execution (MES) shop-floor monitor.
// It models work orders, machines, production events, and defect genealogy in SQL, computes the
// metrics an MES reports (OEE, downtime Pareto, first-pass yield, traceability), and runs a support
// pass that turns those metrics into a per-work-center verdict: OK, WATCH, or HOLD.

const double AvailTarget = 0.90;
const double QualityTarget = 0.95;
const double OeeWorldClass = 0.85;   // Nakajima's world-class OEE benchmark

var dbPath = Path.Combine(Environment.CurrentDirectory, "takt.db");
if (File.Exists(dbPath)) File.Delete(dbPath);   // rebuild each run so output is reproducible

using var db = new SqliteConnection($"Data Source={dbPath}");
db.Open();
Schema.Build(db);

var oee = Analytics.Oee(db);
var pareto = Analytics.DowntimePareto(db);

Console.WriteLine("== Takt: shift OEE by work center ==");
Console.WriteLine($"{"WC",-9}{"Avail",8}{"Perf",8}{"Qual",8}{"OEE",8}");
foreach (var w in oee)
    Console.WriteLine($"{w.Code,-9}{w.Availability,8:P0}{w.Performance,8:P0}{w.Quality,8:P0}{w.Oee,8:P0}");

// Support pass: turn the metrics into a verdict a support engineer can act on.
var alerts = new List<(string Code, string Verdict, string Reason)>();
foreach (var w in oee)
{
    var reasons = new List<string>();
    bool stalled = Analytics.IsStalled(db, w.Code);
    if (stalled) reasons.Add("work order STALLED (no recent production)");
    if (w.Availability < AvailTarget) reasons.Add($"availability {w.Availability:P0} below {AvailTarget:P0} target");
    if (w.Quality < QualityTarget) reasons.Add($"first-pass yield {w.Quality:P0} below {QualityTarget:P0} target");
    if (w.Oee < OeeWorldClass) reasons.Add($"OEE {w.Oee:P0} below world-class {OeeWorldClass:P0}");

    string verdict = reasons.Count == 0 ? "OK"
        : (stalled || w.Quality < QualityTarget) ? "HOLD" : "WATCH";
    alerts.Add((w.Code, verdict, reasons.Count == 0 ? "within targets" : string.Join("; ", reasons)));
}

Console.WriteLine("\n== Support verdicts ==");
foreach (var a in alerts) Console.WriteLine($"{a.Code,-9}{a.Verdict,-7}{a.Reason}");

WriteReport(oee, pareto, alerts, Analytics.Genealogy(db, "WO-1002"));
Console.WriteLine("\nWrote out/findings.md");

void WriteReport(List<OeeRow> oee, List<DownRow> pareto,
                 List<(string Code, string Verdict, string Reason)> alerts, List<DefectRow> gen)
{
    var dir = Path.Combine(Environment.CurrentDirectory, "out");
    Directory.CreateDirectory(dir);
    var sb = new StringBuilder();
    sb.AppendLine("# Takt shift report\n");
    sb.AppendLine("OEE = Availability x Performance x Quality (Nakajima). World-class OEE is about 85%.\n");

    sb.AppendLine("## OEE by work center\n");
    sb.AppendLine("| Work center | Availability | Performance | Quality | OEE |");
    sb.AppendLine("|---|---|---|---|---|");
    foreach (var w in oee)
        sb.AppendLine($"| {w.Code} ({w.Name}) | {w.Availability:P0} | {w.Performance:P0} | {w.Quality:P0} | {w.Oee:P0} |");

    sb.AppendLine("\n## Downtime Pareto (minutes by reason)\n");
    sb.AppendLine("| Reason | Minutes |");
    sb.AppendLine("|---|---|");
    foreach (var d in pareto) sb.AppendLine($"| {d.Reason} | {d.Minutes:0} |");

    sb.AppendLine("\n## Support verdicts\n");
    sb.AppendLine("| Work center | Verdict | Reason |");
    sb.AppendLine("|---|---|---|");
    foreach (var a in alerts) sb.AppendLine($"| {a.Code} | {a.Verdict} | {a.Reason} |");

    sb.AppendLine("\n## Defect genealogy, WO-1002 (assembly excursion)\n");
    sb.AppendLine("| Unit serial | Defect | Time |");
    sb.AppendLine("|---|---|---|");
    foreach (var g in gen) sb.AppendLine($"| {g.Serial} | {g.Code} | {g.Ts} |");

    File.WriteAllText(Path.Combine(dir, "findings.md"), sb.ToString());
}
