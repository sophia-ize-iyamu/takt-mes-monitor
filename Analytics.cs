using Microsoft.Data.Sqlite;

namespace Takt;

public record OeeRow(string Code, string Name, double RunMin, double PlannedMin,
                     long Good, long Scrap, long Total,
                     double Availability, double Performance, double Quality, double Oee);

public record DownRow(string Reason, double Minutes);
public record DefectRow(string Serial, string Code, string Ts);

/// The metrics an MES reports, computed in SQL against the event log.
public static class Analytics
{
    // OEE by work center (Nakajima: OEE = Availability x Performance x Quality), computed in SQL.
    public static List<OeeRow> Oee(SqliteConnection db)
    {
        const string sql = @"
            WITH agg AS (
                SELECT wc.id, wc.code, wc.name, wc.planned_min, wc.ideal_cycle_sec,
                       COALESCE(SUM(CASE WHEN pe.event_type='RUN' THEN pe.duration_min ELSE 0 END),0) AS run_min,
                       COALESCE(SUM(pe.units_good),0)  AS good,
                       COALESCE(SUM(pe.units_scrap),0) AS scrap
                FROM work_centers wc
                LEFT JOIN production_events pe ON pe.work_center_id = wc.id
                GROUP BY wc.id
            )
            SELECT code, name, run_min, planned_min, good, scrap,
                   (good + scrap) AS total,
                   run_min / planned_min AS availability,
                   CASE WHEN run_min > 0
                        THEN (ideal_cycle_sec * (good + scrap) / 60.0) / run_min ELSE 0 END AS performance,
                   CASE WHEN (good + scrap) > 0
                        THEN CAST(good AS REAL) / (good + scrap) ELSE 0 END AS quality
            FROM agg
            ORDER BY code;";

        var rows = new List<OeeRow>();
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            double a = r.GetDouble(7), p = r.GetDouble(8), q = r.GetDouble(9);
            rows.Add(new OeeRow(
                r.GetString(0), r.GetString(1), r.GetDouble(2), r.GetDouble(3),
                r.GetInt64(4), r.GetInt64(5), r.GetInt64(6), a, p, q, a * p * q));
        }
        return rows;
    }

    // Downtime minutes by reason, largest first (a Pareto for where to focus).
    public static List<DownRow> DowntimePareto(SqliteConnection db)
    {
        const string sql = @"
            SELECT COALESCE(reason_code,'UNSPECIFIED') AS reason, SUM(duration_min) AS mins
            FROM production_events WHERE event_type='DOWN'
            GROUP BY reason ORDER BY mins DESC;";
        var rows = new List<DownRow>();
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read()) rows.Add(new DownRow(r.GetString(0), r.GetDouble(1)));
        return rows;
    }

    // Trace every defect on a work order (unit serial, defect code, time).
    public static List<DefectRow> Genealogy(SqliteConnection db, string woNumber)
    {
        const string sql = @"
            SELECT d.unit_serial, d.defect_code, d.ts
            FROM defects d JOIN work_orders w ON w.id = d.work_order_id
            WHERE w.wo_number = $wo ORDER BY d.ts;";
        var rows = new List<DefectRow>();
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$wo", woNumber);
        using var r = cmd.ExecuteReader();
        while (r.Read()) rows.Add(new DefectRow(r.GetString(0), r.GetString(1), r.GetString(2)));
        return rows;
    }

    // Is the work order on this work center flagged as stalled?
    public static bool IsStalled(SqliteConnection db, string code)
    {
        const string sql = @"
            SELECT w.status FROM work_orders w
            JOIN work_centers wc ON wc.id = w.work_center_id
            WHERE wc.code = $c LIMIT 1;";
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$c", code);
        return (cmd.ExecuteScalar() as string) == "STALLED";
    }
}
