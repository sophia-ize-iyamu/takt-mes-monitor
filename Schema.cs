using Microsoft.Data.Sqlite;

namespace Takt;

/// Builds the MES schema and seeds one reproducible synthetic shift.
/// Faults are injected on purpose so the analytics and the support pass have something to catch,
/// the same honest approach used in the Market Data Health Monitor. Real shop-floor data is not
/// public at the work-order level, so the shift is hand-built to be illustrative, not real plant data.
public static class Schema
{
    public static void Build(SqliteConnection db)
    {
        Exec(db,
            "DROP TABLE IF EXISTS defects;" +
            "DROP TABLE IF EXISTS production_events;" +
            "DROP TABLE IF EXISTS work_orders;" +
            "DROP TABLE IF EXISTS work_centers;" +
            @"CREATE TABLE work_centers(
                id INTEGER PRIMARY KEY, code TEXT NOT NULL, name TEXT NOT NULL,
                ideal_cycle_sec REAL NOT NULL,   -- ideal seconds per unit
                planned_min REAL NOT NULL);      -- planned production time this shift
              CREATE TABLE work_orders(
                id INTEGER PRIMARY KEY, wo_number TEXT NOT NULL, part_number TEXT NOT NULL,
                qty_planned INTEGER NOT NULL, status TEXT NOT NULL,   -- RELEASED/RUNNING/COMPLETE/STALLED
                work_center_id INTEGER NOT NULL REFERENCES work_centers(id));
              CREATE TABLE production_events(
                id INTEGER PRIMARY KEY, work_order_id INTEGER NOT NULL REFERENCES work_orders(id),
                work_center_id INTEGER NOT NULL REFERENCES work_centers(id),
                event_type TEXT NOT NULL,        -- RUN/DOWN/IDLE
                reason_code TEXT,                -- DOWN reason: TOOLING/CHANGEOVER/MATERIAL/FAULT
                start_ts TEXT NOT NULL, end_ts TEXT NOT NULL, duration_min REAL NOT NULL,
                units_good INTEGER NOT NULL DEFAULT 0, units_scrap INTEGER NOT NULL DEFAULT 0);
              CREATE TABLE defects(
                id INTEGER PRIMARY KEY, work_order_id INTEGER NOT NULL REFERENCES work_orders(id),
                unit_serial TEXT NOT NULL, defect_code TEXT NOT NULL, ts TEXT NOT NULL);");
        Seed(db);
    }

    static void Seed(SqliteConnection db)
    {
        Exec(db, @"INSERT INTO work_centers(id,code,name,ideal_cycle_sec,planned_min) VALUES
            (1,'CNC-01','CNC Machining Cell',30,480),
            (2,'ASSY-02','Assembly Line',45,480),
            (3,'TEST-03','Final Test',20,480);");

        Exec(db, @"INSERT INTO work_orders(id,wo_number,part_number,qty_planned,status,work_center_id) VALUES
            (1,'WO-1001','P-100',900,'RUNNING',1),
            (2,'WO-1002','P-200',600,'RUNNING',2),
            (3,'WO-1003','P-300',500,'STALLED',3);");

        // (work order, work center, type, reason, start, end, minutes, good, scrap)
        AddEvent(db, 1, 1, "RUN",  null,         "06:00", "08:00", 120, 230, 2);
        AddEvent(db, 1, 1, "DOWN", "TOOLING",    "08:00", "08:45",  45,   0, 0);  // injected: long tooling stop
        AddEvent(db, 1, 1, "RUN",  null,         "08:45", "11:00", 135, 260, 3);
        AddEvent(db, 1, 1, "DOWN", "CHANGEOVER", "11:00", "11:15",  15,   0, 0);
        AddEvent(db, 1, 1, "RUN",  null,         "11:15", "14:00", 165, 300, 4);

        AddEvent(db, 2, 2, "RUN",  null,         "06:00", "09:00", 180, 230,  5);
        AddEvent(db, 2, 2, "RUN",  null,         "09:00", "12:00", 180, 180, 40); // injected: quality excursion
        AddEvent(db, 2, 2, "DOWN", "MATERIAL",   "12:00", "12:20",  20,   0,  0);
        AddEvent(db, 2, 2, "RUN",  null,         "12:20", "14:00", 100, 120,  6);

        AddEvent(db, 3, 3, "RUN",  null,         "06:00", "08:30", 150, 420,  3);
        AddEvent(db, 3, 3, "DOWN", "FAULT",      "08:30", "09:00",  30,   0,  0); // injected: fault, then stall

        // defect genealogy for the assembly excursion (traceability by unit serial)
        Exec(db, @"INSERT INTO defects(work_order_id,unit_serial,defect_code,ts) VALUES
            (2,'P200-0419','SOLDER-BRIDGE','09:14'),
            (2,'P200-0421','MISALIGN','09:31'),
            (2,'P200-0456','SOLDER-BRIDGE','10:02'),
            (2,'P200-0480','COLD-JOINT','10:48'),
            (2,'P200-0501','SOLDER-BRIDGE','11:20');");
    }

    static void AddEvent(SqliteConnection db, int wo, int wc, string type, string? reason,
                         string start, string end, double dur, int good, int scrap)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"INSERT INTO production_events
            (work_order_id,work_center_id,event_type,reason_code,start_ts,end_ts,duration_min,units_good,units_scrap)
            VALUES ($wo,$wc,$t,$r,$s,$e,$d,$g,$sc);";
        cmd.Parameters.AddWithValue("$wo", wo);
        cmd.Parameters.AddWithValue("$wc", wc);
        cmd.Parameters.AddWithValue("$t", type);
        cmd.Parameters.AddWithValue("$r", (object?)reason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$s", start);
        cmd.Parameters.AddWithValue("$e", end);
        cmd.Parameters.AddWithValue("$d", dur);
        cmd.Parameters.AddWithValue("$g", good);
        cmd.Parameters.AddWithValue("$sc", scrap);
        cmd.ExecuteNonQuery();
    }

    static void Exec(SqliteConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
